using LudoGame.Api.Dtos;
using LudoGame.Api.Services;
using LudoGame.Core.Engine;
using Microsoft.AspNetCore.SignalR;

namespace LudoGame.Api.Hubs;

/// <summary>
/// Thin real-time transport layer. Every method here does the same three
/// things: validate input, delegate to IGameEngine (the only place game
/// rules live), then broadcast the resulting state to everyone in the room.
/// No game logic should ever be written inside this class.
/// </summary>
public class GameHub : Hub
{
    private readonly IRoomManager _rooms;
    private readonly IGameEngine _engine;
    // Tracks which room each connection belongs to, so OnDisconnected knows where to clean up.
    private static readonly Dictionary<string, string> ConnectionToRoom = new();

    public GameHub(IRoomManager rooms, IGameEngine engine)
    {
        _rooms = rooms;
        _engine = engine;
    }

    public async Task CreateRoom(string playerName)
    {
        playerName = SanitizeName(playerName);
        var room = _rooms.CreateRoom(Context.ConnectionId, playerName);
        await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomCode);
        lock (ConnectionToRoom) ConnectionToRoom[Context.ConnectionId] = room.RoomCode;

        await Clients.Caller.SendAsync("RoomCreated", room.RoomCode, Context.ConnectionId);
        await BroadcastState(room.RoomCode);
    }

    public async Task JoinRoom(string roomCode, string playerName)
    {
        roomCode = roomCode.Trim().ToUpperInvariant();
        playerName = SanitizeName(playerName);

        if (!_rooms.TryGetRoom(roomCode, out var room) || room is null)
        {
            await Clients.Caller.SendAsync("ActionError", "Room not found.");
            return;
        }

        try
        {
            var player = _engine.JoinRoom(room, Context.ConnectionId, playerName);
            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
            lock (ConnectionToRoom) ConnectionToRoom[Context.ConnectionId] = roomCode;

            await Clients.Caller.SendAsync("RoomJoined", roomCode, Context.ConnectionId, player.Color.ToString());
            await BroadcastState(roomCode);
        }
        catch (InvalidOperationException ex)
        {
            await Clients.Caller.SendAsync("ActionError", ex.Message);
        }
    }

    public async Task StartGame(string roomCode)
    {
        if (!_rooms.TryGetRoom(roomCode, out var room) || room is null)
        {
            await Clients.Caller.SendAsync("ActionError", "Room not found.");
            return;
        }

        var requester = room.GetPlayer(Context.ConnectionId);
        if (requester is null || !requester.IsHost)
        {
            await Clients.Caller.SendAsync("ActionError", "Only the host can start the game.");
            return;
        }

        try
        {
            _engine.StartGame(room);
            await BroadcastState(roomCode);
        }
        catch (InvalidOperationException ex)
        {
            await Clients.Caller.SendAsync("ActionError", ex.Message);
        }
    }

    public async Task RollDice(string roomCode)
    {
        if (!_rooms.TryGetRoom(roomCode, out var room) || room is null) return;

        var result = _engine.RollDice(room, Context.ConnectionId);
        if (!result.Success)
        {
            await Clients.Caller.SendAsync("ActionError", result.Error);
            return;
        }

        await Clients.Group(roomCode).SendAsync("DiceRolled", Context.ConnectionId, result.Value, result.MovableTokenIds, result.TurnAutoPassed);
        await BroadcastState(roomCode);
    }

    public async Task MoveToken(string roomCode, int tokenId)
    {
        if (!_rooms.TryGetRoom(roomCode, out var room) || room is null) return;

        var result = _engine.MoveToken(room, Context.ConnectionId, tokenId);
        if (!result.Success)
        {
            await Clients.Caller.SendAsync("ActionError", result.Error);
            return;
        }

        await Clients.Group(roomCode).SendAsync("TokenMoved", Context.ConnectionId, result.TokenId,
            result.NewSteps, result.Captured, result.TokenFinished, result.ExtraTurn);

        if (result.GameWon)
        {
            await Clients.Group(roomCode).SendAsync("GameWon", result.WinnerConnectionId);
        }

        await BroadcastState(roomCode);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string? roomCode;
        lock (ConnectionToRoom)
        {
            ConnectionToRoom.TryGetValue(Context.ConnectionId, out roomCode);
            ConnectionToRoom.Remove(Context.ConnectionId);
        }

        if (roomCode is not null && _rooms.TryGetRoom(roomCode, out var room) && room is not null)
        {
            _engine.RemovePlayer(room, Context.ConnectionId);
            await BroadcastState(roomCode);
            _rooms.RemoveRoomIfEmpty(roomCode);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task BroadcastState(string roomCode)
    {
        if (!_rooms.TryGetRoom(roomCode, out var room) || room is null) return;
        var dto = DtoMapper.ToDto(room);
        await Clients.Group(roomCode).SendAsync("StateUpdated", dto);
    }

    private static string SanitizeName(string name)
    {
        name = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name)) name = "Player";
        return name.Length > 20 ? name[..20] : name;
    }
}

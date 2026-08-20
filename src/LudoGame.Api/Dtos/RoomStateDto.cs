using LudoGame.Core.Models;

namespace LudoGame.Api.Dtos;

public record TokenDto(int Id, string Color, int Steps, bool IsInBase, bool IsFinished);

public record PlayerDto(
    string ConnectionId,
    string Name,
    string Color,
    bool IsConnected,
    bool IsHost,
    bool HasWon,
    List<TokenDto> Tokens);

public record RoomStateDto(
    string RoomCode,
    string Status,
    string Phase,
    int CurrentPlayerIndex,
    string? CurrentPlayerConnectionId,
    int? LastDiceRoll,
    List<PlayerDto> Players,
    List<string> WinOrder,
    string? LastEventMessage);

public static class DtoMapper
{
    public static RoomStateDto ToDto(GameRoom room)
    {
        return new RoomStateDto(
            RoomCode: room.RoomCode,
            Status: room.Status.ToString(),
            Phase: room.Phase.ToString(),
            CurrentPlayerIndex: room.CurrentPlayerIndex,
            CurrentPlayerConnectionId: room.CurrentPlayer?.ConnectionId,
            LastDiceRoll: room.LastDiceRoll,
            Players: room.Players.Select(p => new PlayerDto(
                p.ConnectionId,
                p.Name,
                p.Color.ToString(),
                p.IsConnected,
                p.IsHost,
                p.HasWon,
                p.Tokens.Select(t => new TokenDto(t.Id, t.Color.ToString(), t.Steps, t.IsInBase, t.IsFinished)).ToList()
            )).ToList(),
            WinOrder: room.WinOrder,
            LastEventMessage: room.LastEventMessage
        );
    }
}

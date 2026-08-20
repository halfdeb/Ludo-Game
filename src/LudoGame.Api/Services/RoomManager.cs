using System.Collections.Concurrent;
using LudoGame.Core.Engine;
using LudoGame.Core.Models;

namespace LudoGame.Api.Services;

public class InMemoryRoomManager : IRoomManager
{
    // Codes avoid ambiguous characters (0/O, 1/I) for easier reading aloud / typing.
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();
    private readonly IGameEngine _engine;
    private readonly Random _random = new();

    public InMemoryRoomManager(IGameEngine engine)
    {
        _engine = engine;
    }

    public IEnumerable<GameRoom> AllRooms => _rooms.Values;

    public GameRoom CreateRoom(string hostConnectionId, string hostName)
    {
        string code;
        do
        {
            code = GenerateCode();
        } while (_rooms.ContainsKey(code));

        var room = _engine.CreateRoom(code, hostConnectionId, hostName);
        _rooms[code] = room;
        return room;
    }

    public bool TryGetRoom(string roomCode, out GameRoom? room)
    {
        return _rooms.TryGetValue(roomCode.ToUpperInvariant(), out room);
    }

    public void RemoveRoomIfEmpty(string roomCode)
    {
        if (_rooms.TryGetValue(roomCode, out var room) && room.Players.Count == 0)
        {
            _rooms.TryRemove(roomCode, out _);
        }
    }

    private string GenerateCode()
    {
        var chars = new char[4];
        for (var i = 0; i < 4; i++)
        {
            chars[i] = CodeAlphabet[_random.Next(CodeAlphabet.Length)];
        }
        return new string(chars);
    }
}

using LudoGame.Core.Models;

namespace LudoGame.Api.Services;

/// <summary>
/// Owns the collection of live GameRooms. Kept separate from GameEngine so
/// the "where do rooms live and how are codes generated" concern is fully
/// decoupled from "what are the rules of Ludo" - either can be swapped
/// independently (e.g. RoomManager could later be backed by Redis for a
/// multi-instance deployment without touching a single rule in GameEngine).
/// </summary>
public interface IRoomManager
{
    GameRoom CreateRoom(string hostConnectionId, string hostName);
    bool TryGetRoom(string roomCode, out GameRoom? room);
    void RemoveRoomIfEmpty(string roomCode);
    IEnumerable<GameRoom> AllRooms { get; }
}

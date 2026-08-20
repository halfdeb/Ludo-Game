using LudoGame.Core.Models;

namespace LudoGame.Core.Engine;

public record CapturedToken(PlayerColor Color, int TokenId);

public class RollResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public int Value { get; init; }
    public List<int> MovableTokenIds { get; init; } = new();
    public bool TurnAutoPassed { get; init; }

    public static RollResult Fail(string error) => new() { Success = false, Error = error };
}

public class MoveResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public int TokenId { get; init; }
    public int NewSteps { get; init; }
    public List<CapturedToken> Captured { get; init; } = new();
    public bool TokenFinished { get; init; }
    public bool ExtraTurn { get; init; }
    public bool GameWon { get; init; }
    public string? WinnerConnectionId { get; init; }

    public static MoveResult Fail(string error) => new() { Success = false, Error = error };
}

/// <summary>
/// The full authoritative rules engine. Everything here is stateless with
/// respect to *which* room it's operating on - a GameRoom is passed in and
/// mutated in place, so the engine itself can be registered as a singleton
/// service and reused across every room.
/// </summary>
public interface IGameEngine
{
    GameRoom CreateRoom(string roomCode, string hostConnectionId, string hostName);
    Player JoinRoom(GameRoom room, string connectionId, string name);
    void RemovePlayer(GameRoom room, string connectionId);
    void StartGame(GameRoom room);
    RollResult RollDice(GameRoom room, string connectionId);
    MoveResult MoveToken(GameRoom room, string connectionId, int tokenId);
}

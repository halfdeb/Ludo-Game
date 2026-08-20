namespace LudoGame.Core.Models;

public class GameRoom
{
    public required string RoomCode { get; init; }
    public List<Player> Players { get; set; } = new();
    public GameStatus Status { get; set; } = GameStatus.WaitingForPlayers;
    public int CurrentPlayerIndex { get; set; } = 0;
    public int? LastDiceRoll { get; set; }
    public TurnPhase Phase { get; set; } = TurnPhase.AwaitingRoll;
    public int ConsecutiveSixes { get; set; } = 0;
    public List<PlayerColor> AvailableColors { get; set; } =
        new() { PlayerColor.Red, PlayerColor.Green, PlayerColor.Yellow, PlayerColor.Blue };
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public List<string> WinOrder { get; set; } = new();
    public string? LastEventMessage { get; set; }

    // Lock object for thread-safe mutation of this specific room.
    public readonly object Lock = new();

    public Player? CurrentPlayer =>
        Players.Count == 0 ? null : Players[CurrentPlayerIndex];

    public Player? GetPlayer(string connectionId) =>
        Players.FirstOrDefault(p => p.ConnectionId == connectionId);
}

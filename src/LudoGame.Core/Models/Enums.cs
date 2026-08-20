namespace LudoGame.Core.Models;

/// <summary>
/// The four canonical Ludo colors. The frontend is free to re-skin these
/// (e.g. Green -> "Shrek", Red -> "Dragon") but the engine only ever
/// reasons about these four values.
/// </summary>
public enum PlayerColor
{
    Red = 0,
    Green = 1,
    Yellow = 2,
    Blue = 3
}

public enum GameStatus
{
    WaitingForPlayers,
    InProgress,
    Finished
}

/// <summary>
/// Whose responsibility it is to act next within a single player's turn.
/// </summary>
public enum TurnPhase
{
    AwaitingRoll,
    AwaitingMove
}

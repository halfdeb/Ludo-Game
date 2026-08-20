using LudoGame.Core.Models;

namespace LudoGame.Core.Engine;

/// <summary>
/// Pure rules data about the shape of the board. No mutable state lives here -
/// this class only answers "where is step N for color C" style questions,
/// which keeps the actual GameEngine easy to unit test.
/// </summary>
public static class BoardConstants
{
    public const int MaxPlayers = 4;
    public const int TokensPerPlayer = 4;
    public const int SharedPathLength = 51; // squares 1..51 of the common ring
    public const int HomeColumnLength = 6;  // squares 52..57
    public const int TotalSteps = SharedPathLength + HomeColumnLength; // 57 = finished
    public const int DiceMax = 6;
    public const int SixesBeforeForfeit = 3;

    /// <summary>
    /// Where each color's "step 1" lands on the shared 52-cell global ring
    /// (global ring has indices 0..51, i.e. 52 squares total).
    /// </summary>
    public static readonly IReadOnlyDictionary<PlayerColor, int> StartOffset =
        new Dictionary<PlayerColor, int>
        {
            { PlayerColor.Red, 0 },
            { PlayerColor.Green, 13 },
            { PlayerColor.Yellow, 26 },
            { PlayerColor.Blue, 39 }
        };

    /// <summary>
    /// Global ring squares (0..51) where no capture can happen: each color's
    /// start square plus the 4 star squares.
    /// </summary>
    public static readonly IReadOnlySet<int> SafeSquares =
        new HashSet<int> { 0, 8, 13, 21, 26, 34, 39, 47 };

    /// <summary>
    /// Converts a token's own relative step (1..51) into the shared global
    /// ring index (0..51). Returns -1 if the step is outside the shared ring
    /// (i.e. the token is in base, in its home column, or finished).
    /// </summary>
    public static int ToGlobalPosition(PlayerColor color, int steps)
    {
        if (steps is < 1 or > SharedPathLength) return -1;
        return (StartOffset[color] + steps - 1) % 52;
    }
}

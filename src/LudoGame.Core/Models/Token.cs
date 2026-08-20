namespace LudoGame.Core.Models;

/// <summary>
/// A single Ludo piece. Position is tracked as an abstract "Steps" counter
/// that is relative to its owner's own starting square, which keeps all the
/// math color-agnostic:
///
///   Steps == 0          -> sitting in base (not yet on the board)
///   Steps in 1..51       -> on the shared 52-square outer ring
///   Steps in 52..56       -> in this color's private 6-square home column
///   Steps == 57          -> finished (reached home)
/// </summary>
public class Token
{
    public int Id { get; init; }
    public PlayerColor Color { get; init; }
    public int Steps { get; set; } = 0;

    public bool IsInBase => Steps == 0;
    public bool IsFinished => Steps == BoardConstantsSteps.TotalSteps;
    public bool IsInHomeColumn => Steps is >= BoardConstantsSteps.SharedPathLength + 1
        and < BoardConstantsSteps.TotalSteps;
    public bool IsOnSharedPath => Steps is >= 1 and <= BoardConstantsSteps.SharedPathLength;
}

/// <summary>
/// Small numeric constants token logic needs that don't require the full
/// BoardConstants (kept here to avoid a circular file reference at parse time).
/// </summary>
internal static class BoardConstantsSteps
{
    public const int SharedPathLength = 51;
    public const int TotalSteps = 57;
}

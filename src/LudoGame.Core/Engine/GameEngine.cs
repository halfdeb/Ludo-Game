using LudoGame.Core.Models;

namespace LudoGame.Core.Engine;

/// <summary>
/// Authoritative Ludo rules implementation. The frontend never decides
/// whether a move is legal - it only ever proposes an action, and this
/// class is the single source of truth for what actually happens.
/// </summary>
public class GameEngine : IGameEngine
{
    private readonly Random _random = new();

    public GameRoom CreateRoom(string roomCode, string hostConnectionId, string hostName)
    {
        var room = new GameRoom { RoomCode = roomCode };
        var color = room.AvailableColors[0];
        room.AvailableColors.RemoveAt(0);
        room.Players.Add(Player.Create(hostConnectionId, hostName, color, isHost: true));
        return room;
    }

    public Player JoinRoom(GameRoom room, string connectionId, string name)
    {
        lock (room.Lock)
        {
            if (room.Status != GameStatus.WaitingForPlayers)
                throw new InvalidOperationException("Game has already started.");
            if (room.Players.Count >= BoardConstants.MaxPlayers)
                throw new InvalidOperationException("Room is full.");
            if (room.Players.Any(p => p.ConnectionId == connectionId))
                throw new InvalidOperationException("Already in this room.");

            var color = room.AvailableColors[0];
            room.AvailableColors.RemoveAt(0);
            var player = Player.Create(connectionId, name, color, isHost: false);
            room.Players.Add(player);
            room.LastEventMessage = $"{name} joined as {color}.";
            return player;
        }
    }

    public void RemovePlayer(GameRoom room, string connectionId)
    {
        lock (room.Lock)
        {
            var player = room.GetPlayer(connectionId);
            if (player is null) return;

            if (room.Status == GameStatus.WaitingForPlayers)
            {
                room.Players.Remove(player);
                room.AvailableColors.Insert(0, player.Color);
                room.AvailableColors.Sort();
                // Reassign host if needed.
                if (player.IsHost && room.Players.Count > 0)
                    room.Players[0].IsHost = true;
            }
            else
            {
                // Mid-game: keep their seat and tokens, just mark disconnected
                // so a reconnect (same connection id flow, handled by hub)
                // can revive them. If it's currently their turn, auto-skip.
                player.IsConnected = false;
                if (room.CurrentPlayer?.ConnectionId == connectionId)
                {
                    AdvanceTurn(room);
                }
            }
        }
    }

    public void StartGame(GameRoom room)
    {
        lock (room.Lock)
        {
            if (room.Status != GameStatus.WaitingForPlayers)
                throw new InvalidOperationException("Game already started.");
            if (room.Players.Count < 2)
                throw new InvalidOperationException("Need at least 2 players to start.");

            room.Status = GameStatus.InProgress;
            room.CurrentPlayerIndex = 0;
            room.Phase = TurnPhase.AwaitingRoll;
            room.LastEventMessage = "Game started!";
        }
    }

    public RollResult RollDice(GameRoom room, string connectionId)
    {
        lock (room.Lock)
        {
            if (room.Status != GameStatus.InProgress)
                return RollResult.Fail("Game is not in progress.");

            var current = room.CurrentPlayer;
            if (current is null || current.ConnectionId != connectionId)
                return RollResult.Fail("It is not your turn.");

            if (room.Phase != TurnPhase.AwaitingRoll)
                return RollResult.Fail("You already rolled this turn.");

            var value = _random.Next(1, BoardConstants.DiceMax + 1);
            room.LastDiceRoll = value;

            if (value == BoardConstants.DiceMax)
            {
                room.ConsecutiveSixes++;
                if (room.ConsecutiveSixes >= BoardConstants.SixesBeforeForfeit)
                {
                    // Three sixes in a row forfeits the whole turn.
                    room.ConsecutiveSixes = 0;
                    room.LastEventMessage = $"{current.Name} rolled three 6s in a row - turn forfeited.";
                    AdvanceTurn(room);
                    return new RollResult
                    {
                        Success = true,
                        Value = value,
                        MovableTokenIds = new List<int>(),
                        TurnAutoPassed = true
                    };
                }
            }
            else
            {
                room.ConsecutiveSixes = 0;
            }

            var movable = GetMovableTokenIds(current, value);
            room.Phase = TurnPhase.AwaitingMove;

            if (movable.Count == 0)
            {
                room.LastEventMessage = $"{current.Name} rolled a {value} - no legal move.";
                AdvanceTurn(room);
                return new RollResult
                {
                    Success = true,
                    Value = value,
                    MovableTokenIds = movable,
                    TurnAutoPassed = true
                };
            }

            room.LastEventMessage = $"{current.Name} rolled a {value}.";
            return new RollResult { Success = true, Value = value, MovableTokenIds = movable };
        }
    }

    public MoveResult MoveToken(GameRoom room, string connectionId, int tokenId)
    {
        lock (room.Lock)
        {
            if (room.Status != GameStatus.InProgress)
                return MoveResult.Fail("Game is not in progress.");

            var current = room.CurrentPlayer;
            if (current is null || current.ConnectionId != connectionId)
                return MoveResult.Fail("It is not your turn.");

            if (room.Phase != TurnPhase.AwaitingMove || room.LastDiceRoll is null)
                return MoveResult.Fail("Roll the dice first.");

            var dice = room.LastDiceRoll.Value;
            var token = current.Tokens.FirstOrDefault(t => t.Id == tokenId);
            if (token is null) return MoveResult.Fail("Unknown token.");

            var movable = GetMovableTokenIds(current, dice);
            if (!movable.Contains(tokenId))
                return MoveResult.Fail("That token cannot make this move.");

            var newSteps = token.IsInBase ? 1 : token.Steps + dice;
            token.Steps = newSteps;

            var captured = new List<CapturedToken>();
            if (newSteps is >= 1 and <= BoardConstants.SharedPathLength)
            {
                var globalPos = BoardConstants.ToGlobalPosition(current.Color, newSteps);
                if (!BoardConstants.SafeSquares.Contains(globalPos))
                {
                    foreach (var opponent in room.Players.Where(p => p.Color != current.Color))
                    {
                        foreach (var oppToken in opponent.Tokens.Where(t => t.IsOnSharedPath))
                        {
                            var oppGlobalPos = BoardConstants.ToGlobalPosition(opponent.Color, oppToken.Steps);
                            if (oppGlobalPos == globalPos)
                            {
                                oppToken.Steps = 0; // sent back to base
                                captured.Add(new CapturedToken(opponent.Color, oppToken.Id));
                            }
                        }
                    }
                }
            }

            var finished = token.IsFinished;
            var wonGame = finished && current.HasWon;
            var extraTurn = dice == BoardConstants.DiceMax || captured.Count > 0 || finished;

            if (wonGame)
            {
                room.Status = GameStatus.Finished;
                room.WinOrder.Add(current.ConnectionId);
                room.LastEventMessage = $"{current.Name} wins!";
            }
            else
            {
                var msg = $"{current.Name} moved token {tokenId}.";
                if (captured.Count > 0) msg += $" Captured {captured.Count} token(s)!";
                if (finished) msg += " Token reached home!";
                room.LastEventMessage = msg;

                room.Phase = TurnPhase.AwaitingRoll;
                room.LastDiceRoll = null;
                if (!extraTurn)
                {
                    AdvanceTurn(room);
                }
            }

            return new MoveResult
            {
                Success = true,
                TokenId = tokenId,
                NewSteps = newSteps,
                Captured = captured,
                TokenFinished = finished,
                ExtraTurn = extraTurn && !wonGame,
                GameWon = wonGame,
                WinnerConnectionId = wonGame ? current.ConnectionId : null
            };
        }
    }

    // ---- helpers ----

    private static List<int> GetMovableTokenIds(Player player, int dice)
    {
        var result = new List<int>();
        foreach (var token in player.Tokens)
        {
            if (token.IsFinished) continue;

            if (token.IsInBase)
            {
                if (dice == BoardConstants.DiceMax) result.Add(token.Id);
                continue;
            }

            var newSteps = token.Steps + dice;
            if (newSteps <= BoardConstants.TotalSteps) result.Add(token.Id);
        }
        return result;
    }

    private static void AdvanceTurn(GameRoom room)
    {
        room.Phase = TurnPhase.AwaitingRoll;
        room.LastDiceRoll = null;
        room.ConsecutiveSixes = 0;

        if (room.Players.Count == 0) return;

        var attempts = 0;
        do
        {
            room.CurrentPlayerIndex = (room.CurrentPlayerIndex + 1) % room.Players.Count;
            attempts++;
        } while (
            (!room.Players[room.CurrentPlayerIndex].IsConnected ||
             room.Players[room.CurrentPlayerIndex].HasWon) &&
            attempts <= room.Players.Count);
    }
}

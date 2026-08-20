namespace LudoGame.Core.Models;

public class Player
{
    public required string ConnectionId { get; set; }
    public required string Name { get; set; }
    public PlayerColor Color { get; set; }
    public List<Token> Tokens { get; set; } = new();
    public bool IsConnected { get; set; } = true;
    public bool IsHost { get; set; }

    public bool HasWon => Tokens.Count > 0 && Tokens.All(t => t.IsFinished);

    public static Player Create(string connectionId, string name, PlayerColor color, bool isHost)
    {
        var player = new Player
        {
            ConnectionId = connectionId,
            Name = name,
            Color = color,
            IsHost = isHost
        };
        for (var i = 0; i < 4; i++)
        {
            player.Tokens.Add(new Token { Id = i, Color = color, Steps = 0 });
        }
        return player;
    }
}

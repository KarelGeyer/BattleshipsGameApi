namespace BattleshipsGame.Api.Models;

public class Game
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Player? Player1 { get; set; }
    public Player? Player2 { get; set; }
    public Guid? CurrentPlayerId { get; set; }
    public GameState State { get; set; } = GameState.WaitingForPlayer;
    public Guid? WinnerId { get; set; }
    public int BoardSize { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsFull => Player1 != null && Player2 != null;
}

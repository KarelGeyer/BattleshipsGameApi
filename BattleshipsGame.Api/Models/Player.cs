namespace BattleshipsGame.Api.Models;

public class Player
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Board Board { get; set; }

    public Player(string name, int boardSize)
    {
        Name = name;
        Board = new Board(boardSize);
    }
}

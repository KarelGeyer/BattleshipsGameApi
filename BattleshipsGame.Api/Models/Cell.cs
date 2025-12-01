namespace BattleshipsGame.Api.Models;

public class Cell
{
    public int X { get; set; }
    public int Y { get; set; }
    public CellState State { get; set; } = CellState.Empty;
    public Guid? ShipId { get; set; }

    public Cell(int x, int y)
    {
        X = x;
        Y = y;
    }
}

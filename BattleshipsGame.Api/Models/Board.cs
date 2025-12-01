namespace BattleshipsGame.Api.Models;

public class Board
{
    public int Size { get; set; }
    public Cell[,] Cells { get; set; }
    public List<Ship> Ships { get; set; } = new();

    public Board(int size)
    {
        if (size < 10 || size > 20)
            throw new ArgumentOutOfRangeException(nameof(size), "Board size must be between 10 and 20");
        
        Size = size;
        Cells = new Cell[size, size];
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Cells[x, y] = new Cell(x, y);
            }
        }
    }

    public Cell GetCell(int x, int y)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size)
            throw new ArgumentOutOfRangeException($"Coordinates ({x}, {y}) are out of bounds");
        return Cells[x, y];
    }

    public bool AllShipsSunk => Ships.All(s => s.IsSunk);
}

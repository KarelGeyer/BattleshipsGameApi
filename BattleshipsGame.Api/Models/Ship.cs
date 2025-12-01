namespace BattleshipsGame.Api.Models;

public class Ship
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ShipType Type { get; set; }
    public List<(int X, int Y)> Cells { get; set; } = new();
    public int HitCount { get; set; } = 0;
    
    public bool IsSunk => HitCount >= Cells.Count;
    
    public int Size => Type switch
    {
        ShipType.Single => 1,
        ShipType.Double => 2,
        ShipType.Triple => 3,
        ShipType.Cross => 5,
        ShipType.Plus => 5,
        _ => 0
    };
}

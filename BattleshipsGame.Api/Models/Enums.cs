namespace BattleshipsGame.Api.Models;

public enum CellState
{
    Empty,
    Ship,
    Hit,
    Miss
}

public enum ShotResult
{
    Water,  // "Voda" - missed
    Hit,    // "Zásah" - hit a ship
    Sunk    // "Potopena celá" - ship is completely sunk
}

public enum GameState
{
    WaitingForPlayer,
    InProgress,
    Finished
}

public enum ShipType
{
    Single,      // 1 cell ship
    Double,      // 2 cell ship
    Triple,      // 3 cell ship
    Cross,       // Cross-shaped ship (5 cells)
    Plus         // Plus-shaped ship (5 cells)
}

public enum Orientation
{
    Horizontal,
    Vertical
}

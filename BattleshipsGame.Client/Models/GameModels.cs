namespace BattleshipsGame.Client.Models;

public enum ShotResult
{
    Water,
    Hit,
    Sunk
}

public enum GameState
{
    WaitingForPlayer,
    InProgress,
    Finished
}

public record CreateGameRequest(int BoardSize = 10);

public record JoinGameRequest(string PlayerName);

public record ShotRequest(int X, int Y);

public record CreateGameResponse(Guid GameId, Guid PlayerId, int BoardSize);

public record JoinGameResponse(Guid GameId, Guid PlayerId, bool GameStarted);

public record ShotResponse(ShotResult Result, bool GameOver, Guid? WinnerId, string? ShipTypeSunk);

public record GameStatusResponse(
    Guid GameId,
    GameState State,
    Guid? CurrentPlayerId,
    Guid? WinnerId,
    int BoardSize,
    bool IsYourTurn,
    BoardView? YourBoard,
    BoardView? EnemyBoard
);

public record BoardView(
    int Size,
    List<CellView> Cells
);

public record CellView(
    int X,
    int Y,
    bool IsHit,
    bool IsMiss,
    bool IsShip
);

public record AvailableGame(Guid Id, int BoardSize, DateTime CreatedAt);

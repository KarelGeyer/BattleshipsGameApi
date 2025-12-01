using BattleshipsGame.Api.Models;

namespace BattleshipsGame.Api.Services;

public interface IGameService
{
    CreateGameResponse CreateGame(CreateGameRequest request);
    JoinGameResponse JoinGame(Guid gameId, JoinGameRequest request);
    ShotResponse MakeShot(Guid gameId, Guid playerId, ShotRequest request);
    GameStatusResponse GetGameStatus(Guid gameId, Guid playerId);
    IEnumerable<Game> GetAvailableGames();
    
    // Local game (single computer) mode
    CreateLocalGameResponse CreateLocalGame(CreateLocalGameRequest request);
    LocalGameStatusResponse GetLocalGameStatus(Guid gameId);
    ShotResponse MakeLocalShot(Guid gameId, ShotRequest request);
}

public class GameService : IGameService
{
    private readonly Dictionary<Guid, Game> _games = new();
    private readonly Random _random = new();
    private readonly object _lock = new();

    public CreateGameResponse CreateGame(CreateGameRequest request)
    {
        var boardSize = Math.Clamp(request.BoardSize, 10, 20);
        
        var game = new Game
        {
            BoardSize = boardSize,
            State = GameState.WaitingForPlayer
        };

        var player = new Player("Player 1", boardSize);
        game.Player1 = player;
        
        PlaceShipsRandomly(player.Board);
        
        lock (_lock)
        {
            _games[game.Id] = game;
        }

        return new CreateGameResponse(game.Id, player.Id, boardSize);
    }

    public JoinGameResponse JoinGame(Guid gameId, JoinGameRequest request)
    {
        lock (_lock)
        {
            if (!_games.TryGetValue(gameId, out var game))
                throw new InvalidOperationException("Game not found");

            if (game.IsFull)
                throw new InvalidOperationException("Game is already full");

            var player = new Player(request.PlayerName ?? "Player 2", game.BoardSize);
            game.Player2 = player;
            
            PlaceShipsRandomly(player.Board);
            
            game.State = GameState.InProgress;
            game.CurrentPlayerId = game.Player1!.Id;

            return new JoinGameResponse(game.Id, player.Id, true);
        }
    }

    public ShotResponse MakeShot(Guid gameId, Guid playerId, ShotRequest request)
    {
        lock (_lock)
        {
            if (!_games.TryGetValue(gameId, out var game))
                throw new InvalidOperationException("Game not found");

            if (game.State != GameState.InProgress)
                throw new InvalidOperationException("Game is not in progress");

            if (game.CurrentPlayerId != playerId)
                throw new InvalidOperationException("It's not your turn");

            var shooter = game.Player1?.Id == playerId ? game.Player1 : game.Player2;
            var target = game.Player1?.Id == playerId ? game.Player2 : game.Player1;

            if (shooter == null || target == null)
                throw new InvalidOperationException("Invalid player");

            var cell = target.Board.GetCell(request.X, request.Y);

            if (cell.State == CellState.Hit || cell.State == CellState.Miss)
                throw new InvalidOperationException("Cell already targeted");

            ShotResult result;
            string? shipTypeSunk = null;

            if (cell.State == CellState.Ship && cell.ShipId.HasValue)
            {
                cell.State = CellState.Hit;
                var ship = target.Board.Ships.First(s => s.Id == cell.ShipId);
                ship.HitCount++;

                if (ship.IsSunk)
                {
                    result = ShotResult.Sunk;
                    shipTypeSunk = ship.Type.ToString();
                }
                else
                {
                    result = ShotResult.Hit;
                }
            }
            else
            {
                cell.State = CellState.Miss;
                result = ShotResult.Water;
            }

            bool gameOver = target.Board.AllShipsSunk;
            Guid? winnerId = null;

            if (gameOver)
            {
                game.State = GameState.Finished;
                game.WinnerId = playerId;
                winnerId = playerId;
            }
            else if (result == ShotResult.Water)
            {
                // Only switch turns on a miss; player gets to play again on a hit
                game.CurrentPlayerId = target.Id;
            }

            return new ShotResponse(result, gameOver, winnerId, shipTypeSunk);
        }
    }

    public GameStatusResponse GetGameStatus(Guid gameId, Guid playerId)
    {
        lock (_lock)
        {
            if (!_games.TryGetValue(gameId, out var game))
                throw new InvalidOperationException("Game not found");

            var isPlayer1 = game.Player1?.Id == playerId;
            var isPlayer2 = game.Player2?.Id == playerId;

            if (!isPlayer1 && !isPlayer2)
                throw new InvalidOperationException("Player not in this game");

            var ownBoard = isPlayer1 ? game.Player1!.Board : game.Player2!.Board;
            var enemyBoard = isPlayer1 ? game.Player2?.Board : game.Player1?.Board;

            return new GameStatusResponse(
                game.Id,
                game.State,
                game.CurrentPlayerId,
                game.WinnerId,
                game.BoardSize,
                game.CurrentPlayerId == playerId,
                CreateBoardView(ownBoard, true),
                enemyBoard != null ? CreateBoardView(enemyBoard, false) : null
            );
        }
    }

    public IEnumerable<Game> GetAvailableGames()
    {
        lock (_lock)
        {
            return _games.Values.Where(g => g.State == GameState.WaitingForPlayer).ToList();
        }
    }

    public CreateLocalGameResponse CreateLocalGame(CreateLocalGameRequest request)
    {
        var boardSize = Math.Clamp(request.BoardSize, 10, 20);
        
        var game = new Game
        {
            BoardSize = boardSize,
            State = GameState.InProgress,
            IsLocalGame = true
        };

        var player1 = new Player("Player 1", boardSize);
        var player2 = new Player("Player 2", boardSize);
        
        game.Player1 = player1;
        game.Player2 = player2;
        
        PlaceShipsRandomly(player1.Board);
        PlaceShipsRandomly(player2.Board);
        
        game.CurrentPlayerId = player1.Id;
        
        lock (_lock)
        {
            _games[game.Id] = game;
        }

        return new CreateLocalGameResponse(
            game.Id, 
            player1.Id, 
            player2.Id, 
            boardSize, 
            player1.Id
        );
    }

    public LocalGameStatusResponse GetLocalGameStatus(Guid gameId)
    {
        lock (_lock)
        {
            if (!_games.TryGetValue(gameId, out var game))
                throw new InvalidOperationException("Game not found");

            if (!game.IsLocalGame)
                throw new InvalidOperationException("This is not a local game");

            var currentPlayer = game.CurrentPlayerId == game.Player1!.Id ? game.Player1 : game.Player2!;
            var opponent = game.CurrentPlayerId == game.Player1!.Id ? game.Player2! : game.Player1;

            return new LocalGameStatusResponse(
                game.Id,
                game.State,
                game.CurrentPlayerId!.Value,
                game.WinnerId,
                game.BoardSize,
                currentPlayer.Name,
                CreateBoardView(currentPlayer.Board, true),
                CreateBoardView(opponent.Board, false)
            );
        }
    }

    public ShotResponse MakeLocalShot(Guid gameId, ShotRequest request)
    {
        lock (_lock)
        {
            if (!_games.TryGetValue(gameId, out var game))
                throw new InvalidOperationException("Game not found");

            if (!game.IsLocalGame)
                throw new InvalidOperationException("This is not a local game");

            if (game.State != GameState.InProgress)
                throw new InvalidOperationException("Game is not in progress");

            var shooter = game.CurrentPlayerId == game.Player1!.Id ? game.Player1 : game.Player2!;
            var target = game.CurrentPlayerId == game.Player1!.Id ? game.Player2! : game.Player1;

            var cell = target.Board.GetCell(request.X, request.Y);

            if (cell.State == CellState.Hit || cell.State == CellState.Miss)
                throw new InvalidOperationException("Cell already targeted");

            ShotResult result;
            string? shipTypeSunk = null;

            if (cell.State == CellState.Ship && cell.ShipId.HasValue)
            {
                cell.State = CellState.Hit;
                var ship = target.Board.Ships.First(s => s.Id == cell.ShipId);
                ship.HitCount++;

                if (ship.IsSunk)
                {
                    result = ShotResult.Sunk;
                    shipTypeSunk = ship.Type.ToString();
                }
                else
                {
                    result = ShotResult.Hit;
                }
            }
            else
            {
                cell.State = CellState.Miss;
                result = ShotResult.Water;
            }

            bool gameOver = target.Board.AllShipsSunk;
            Guid? winnerId = null;

            if (gameOver)
            {
                game.State = GameState.Finished;
                game.WinnerId = shooter.Id;
                winnerId = shooter.Id;
            }
            else if (result == ShotResult.Water)
            {
                // Only switch turns on a miss; player gets to play again on a hit
                game.CurrentPlayerId = target.Id;
            }

            return new ShotResponse(result, gameOver, winnerId, shipTypeSunk);
        }
    }

    private BoardView CreateBoardView(Board board, bool showShips)
    {
        var cells = new List<CellView>();
        
        for (int y = 0; y < board.Size; y++)
        {
            for (int x = 0; x < board.Size; x++)
            {
                var cell = board.Cells[x, y];
                cells.Add(new CellView(
                    x,
                    y,
                    cell.State == CellState.Hit,
                    cell.State == CellState.Miss,
                    showShips && (cell.State == CellState.Ship || cell.State == CellState.Hit)
                ));
            }
        }

        return new BoardView(board.Size, cells);
    }

    private void PlaceShipsRandomly(Board board)
    {
        // Place larger ships first to increase success rate
        var shipsToPlace = new List<ShipType>
        {
            ShipType.Plus,
            ShipType.Cross,
            ShipType.Triple,
            ShipType.Double, ShipType.Double,
            ShipType.Single, ShipType.Single
        };

        foreach (var shipType in shipsToPlace)
        {
            PlaceShipRandomly(board, shipType);
        }
    }

    private void PlaceShipRandomly(Board board, ShipType type)
    {
        int maxAttempts = 1000;
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            attempts++;
            var cells = GetShipCells(board.Size, type);
            
            if (cells == null || !CanPlaceShip(board, cells))
                continue;

            var ship = new Ship { Type = type, Cells = cells };
            
            foreach (var (x, y) in cells)
            {
                board.Cells[x, y].State = CellState.Ship;
                board.Cells[x, y].ShipId = ship.Id;
            }
            
            board.Ships.Add(ship);
            return;
        }

        throw new InvalidOperationException($"Could not place ship of type {type} after {maxAttempts} attempts");
    }

    private List<(int X, int Y)>? GetShipCells(int boardSize, ShipType type)
    {
        var cells = new List<(int X, int Y)>();
        
        switch (type)
        {
            case ShipType.Single:
                {
                    int x = _random.Next(boardSize);
                    int y = _random.Next(boardSize);
                    cells.Add((x, y));
                }
                break;

            case ShipType.Double:
                {
                    int x = _random.Next(boardSize);
                    int y = _random.Next(boardSize);
                    bool horizontal = _random.Next(2) == 0;
                    
                    cells.Add((x, y));
                    if (horizontal)
                    {
                        if (x + 1 >= boardSize) return null;
                        cells.Add((x + 1, y));
                    }
                    else
                    {
                        if (y + 1 >= boardSize) return null;
                        cells.Add((x, y + 1));
                    }
                }
                break;

            case ShipType.Triple:
                {
                    int x = _random.Next(boardSize);
                    int y = _random.Next(boardSize);
                    bool horizontal = _random.Next(2) == 0;
                    
                    if (horizontal)
                    {
                        if (x + 2 >= boardSize) return null;
                        cells.Add((x, y));
                        cells.Add((x + 1, y));
                        cells.Add((x + 2, y));
                    }
                    else
                    {
                        if (y + 2 >= boardSize) return null;
                        cells.Add((x, y));
                        cells.Add((x, y + 1));
                        cells.Add((x, y + 2));
                    }
                }
                break;

            case ShipType.Cross:
                // Shape:   [X]
                //        [XXX]
                //          [X]
                {
                    int minX = 1;
                    int maxX = boardSize - 2;
                    int minY = 1;
                    int maxY = boardSize - 2;
                    if (maxX < minX || maxY < minY) return null;
                    
                    int x = _random.Next(minX, maxX + 1);
                    int y = _random.Next(minY, maxY + 1);
                    
                    cells.Add((x, y - 1));      // Top
                    cells.Add((x - 1, y));      // Left
                    cells.Add((x, y));          // Center
                    cells.Add((x + 1, y));      // Right
                    cells.Add((x, y + 1));      // Bottom
                }
                break;

            case ShipType.Plus:
                // Shape: [XXXX]
                //          [X]
                {
                    int maxX = boardSize - 4;
                    int maxY = boardSize - 2;
                    if (maxX < 0 || maxY < 0) return null;
                    
                    int x = _random.Next(maxX + 1);
                    int y = _random.Next(maxY + 1);
                    
                    cells.Add((x, y));
                    cells.Add((x + 1, y));
                    cells.Add((x + 2, y));
                    cells.Add((x + 3, y));
                    cells.Add((x + 1, y + 1));  // The single cell below second position
                }
                break;
        }

        return cells;
    }

    private bool CanPlaceShip(Board board, List<(int X, int Y)> cells)
    {
        foreach (var (x, y) in cells)
        {
            if (x < 0 || x >= board.Size || y < 0 || y >= board.Size)
                return false;

            if (board.Cells[x, y].State != CellState.Empty)
                return false;

            // Check adjacent cells (ships shouldn't touch)
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    
                    if (nx >= 0 && nx < board.Size && ny >= 0 && ny < board.Size)
                    {
                        if (board.Cells[nx, ny].State == CellState.Ship)
                        {
                            // Allow if the adjacent cell is part of the same ship
                            if (!cells.Contains((nx, ny)))
                                return false;
                        }
                    }
                }
            }
        }

        return true;
    }
}

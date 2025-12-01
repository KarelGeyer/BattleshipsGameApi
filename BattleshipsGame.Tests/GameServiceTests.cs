using BattleshipsGame.Api.Models;
using BattleshipsGame.Api.Services;

namespace BattleshipsGame.Tests;

public class GameServiceTests
{
    private readonly GameService _gameService;

    public GameServiceTests()
    {
        _gameService = new GameService();
    }

    [Theory]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(20)]
    public void CreateGame_WithValidBoardSize_ReturnsGameWithCorrectSize(int boardSize)
    {
        var request = new CreateGameRequest(boardSize);
        
        var response = _gameService.CreateGame(request);
        
        Assert.Equal(boardSize, response.BoardSize);
        Assert.NotEqual(Guid.Empty, response.GameId);
        Assert.NotEqual(Guid.Empty, response.PlayerId);
    }

    [Fact]
    public void CreateGame_WithSizeBelowMinimum_ClampsTen()
    {
        var request = new CreateGameRequest(5);
        
        var response = _gameService.CreateGame(request);
        
        Assert.Equal(10, response.BoardSize);
    }

    [Fact]
    public void CreateGame_WithSizeAboveMaximum_ClampsTwenty()
    {
        var request = new CreateGameRequest(25);
        
        var response = _gameService.CreateGame(request);
        
        Assert.Equal(20, response.BoardSize);
    }

    [Fact]
    public void JoinGame_ValidGame_ReturnsJoinedAndStarted()
    {
        var createResponse = _gameService.CreateGame(new CreateGameRequest(10));
        var joinRequest = new JoinGameRequest("Player 2");
        
        var joinResponse = _gameService.JoinGame(createResponse.GameId, joinRequest);
        
        Assert.Equal(createResponse.GameId, joinResponse.GameId);
        Assert.True(joinResponse.GameStarted);
        Assert.NotEqual(Guid.Empty, joinResponse.PlayerId);
    }

    [Fact]
    public void JoinGame_NonExistentGame_ThrowsException()
    {
        var joinRequest = new JoinGameRequest("Player 2");
        
        Assert.Throws<InvalidOperationException>(() => 
            _gameService.JoinGame(Guid.NewGuid(), joinRequest));
    }

    [Fact]
    public void JoinGame_GameAlreadyFull_ThrowsException()
    {
        var createResponse = _gameService.CreateGame(new CreateGameRequest(10));
        _gameService.JoinGame(createResponse.GameId, new JoinGameRequest("Player 2"));
        
        Assert.Throws<InvalidOperationException>(() => 
            _gameService.JoinGame(createResponse.GameId, new JoinGameRequest("Player 3")));
    }

    [Fact]
    public void MakeShot_ValidShot_ReturnsShotResult()
    {
        var createResponse = _gameService.CreateGame(new CreateGameRequest(10));
        var joinResponse = _gameService.JoinGame(createResponse.GameId, new JoinGameRequest("Player 2"));
        
        // Player 1 makes first shot (since they go first)
        var shotRequest = new ShotRequest(0, 0);
        var shotResponse = _gameService.MakeShot(createResponse.GameId, createResponse.PlayerId, shotRequest);
        
        Assert.True(shotResponse.Result == ShotResult.Water || 
                   shotResponse.Result == ShotResult.Hit || 
                   shotResponse.Result == ShotResult.Sunk);
    }

    [Fact]
    public void MakeShot_WhenNotYourTurn_ThrowsException()
    {
        var createResponse = _gameService.CreateGame(new CreateGameRequest(10));
        var joinResponse = _gameService.JoinGame(createResponse.GameId, new JoinGameRequest("Player 2"));
        
        // Player 2 tries to shoot first (but Player 1 goes first)
        var shotRequest = new ShotRequest(0, 0);
        
        Assert.Throws<InvalidOperationException>(() => 
            _gameService.MakeShot(createResponse.GameId, joinResponse.PlayerId, shotRequest));
    }

    [Fact]
    public void MakeShot_SamePositionTwice_ThrowsException()
    {
        var createResponse = _gameService.CreateGame(new CreateGameRequest(10));
        var joinResponse = _gameService.JoinGame(createResponse.GameId, new JoinGameRequest("Player 2"));
        
        // Player 1 shoots until they miss to ensure turn switches
        int x = 0, y = 0;
        ShotResponse shotResponse;
        
        do
        {
            shotResponse = _gameService.MakeShot(createResponse.GameId, createResponse.PlayerId, new ShotRequest(x, y));
            if (shotResponse.Result != ShotResult.Water)
            {
                x++;
                if (x >= 10)
                {
                    x = 0;
                    y++;
                }
            }
        } while (shotResponse.Result != ShotResult.Water && !shotResponse.GameOver);
        
        if (shotResponse.GameOver)
            return; // Skip if game ended
        
        int missX = x, missY = y;
        
        // Player 2 shoots until they miss
        x = 0; y = 0;
        do
        {
            shotResponse = _gameService.MakeShot(createResponse.GameId, joinResponse.PlayerId, new ShotRequest(x, y));
            if (shotResponse.Result != ShotResult.Water)
            {
                x++;
                if (x >= 10)
                {
                    x = 0;
                    y++;
                }
            }
        } while (shotResponse.Result != ShotResult.Water && !shotResponse.GameOver);
        
        if (shotResponse.GameOver)
            return; // Skip if game ended
        
        // Now it's Player 1's turn again, try to shoot at the same cell they already targeted
        Assert.Throws<InvalidOperationException>(() => 
            _gameService.MakeShot(createResponse.GameId, createResponse.PlayerId, new ShotRequest(missX, missY)));
    }

    [Fact]
    public void GetGameStatus_ValidPlayer_ReturnsStatus()
    {
        var createResponse = _gameService.CreateGame(new CreateGameRequest(10));
        var joinResponse = _gameService.JoinGame(createResponse.GameId, new JoinGameRequest("Player 2"));
        
        var status = _gameService.GetGameStatus(createResponse.GameId, createResponse.PlayerId);
        
        Assert.Equal(createResponse.GameId, status.GameId);
        Assert.Equal(GameState.InProgress, status.State);
        Assert.True(status.IsYourTurn);
        Assert.NotNull(status.YourBoard);
        Assert.NotNull(status.EnemyBoard);
    }

    [Fact]
    public void GetAvailableGames_WithWaitingGame_ReturnsGame()
    {
        var createResponse = _gameService.CreateGame(new CreateGameRequest(10));
        
        var availableGames = _gameService.GetAvailableGames();
        
        Assert.Single(availableGames);
        Assert.Equal(createResponse.GameId, availableGames.First().Id);
    }

    [Fact]
    public void GetAvailableGames_AfterGameStarted_ReturnsEmpty()
    {
        var createResponse = _gameService.CreateGame(new CreateGameRequest(10));
        _gameService.JoinGame(createResponse.GameId, new JoinGameRequest("Player 2"));
        
        var availableGames = _gameService.GetAvailableGames();
        
        Assert.Empty(availableGames);
    }

    // Local game tests

    [Theory]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(20)]
    public void CreateLocalGame_WithValidBoardSize_ReturnsGameWithBothPlayers(int boardSize)
    {
        var request = new CreateLocalGameRequest(boardSize);
        
        var response = _gameService.CreateLocalGame(request);
        
        Assert.Equal(boardSize, response.BoardSize);
        Assert.NotEqual(Guid.Empty, response.GameId);
        Assert.NotEqual(Guid.Empty, response.Player1Id);
        Assert.NotEqual(Guid.Empty, response.Player2Id);
        Assert.NotEqual(response.Player1Id, response.Player2Id);
        Assert.Equal(response.Player1Id, response.CurrentPlayerId);
    }

    [Fact]
    public void CreateLocalGame_WithSizeBelowMinimum_ClampsTen()
    {
        var request = new CreateLocalGameRequest(5);
        
        var response = _gameService.CreateLocalGame(request);
        
        Assert.Equal(10, response.BoardSize);
    }

    [Fact]
    public void GetLocalGameStatus_ReturnsCurrentPlayerInfo()
    {
        var createResponse = _gameService.CreateLocalGame(new CreateLocalGameRequest(10));
        
        var status = _gameService.GetLocalGameStatus(createResponse.GameId);
        
        Assert.Equal(createResponse.GameId, status.GameId);
        Assert.Equal(GameState.InProgress, status.State);
        Assert.Equal("Player 1", status.CurrentPlayerName);
        Assert.NotNull(status.CurrentPlayerBoard);
        Assert.NotNull(status.OpponentBoard);
    }

    [Fact]
    public void MakeLocalShot_ValidShot_ReturnsShotResult()
    {
        var createResponse = _gameService.CreateLocalGame(new CreateLocalGameRequest(10));
        
        var shotRequest = new ShotRequest(0, 0);
        var shotResponse = _gameService.MakeLocalShot(createResponse.GameId, shotRequest);
        
        Assert.True(shotResponse.Result == ShotResult.Water || 
                   shotResponse.Result == ShotResult.Hit || 
                   shotResponse.Result == ShotResult.Sunk);
    }

    [Fact]
    public void MakeLocalShot_SwitchesTurnOnMiss()
    {
        var createResponse = _gameService.CreateLocalGame(new CreateLocalGameRequest(10));
        
        // Keep shooting until we get a miss (Water) to test turn switching
        int x = 0, y = 0;
        ShotResponse shotResponse;
        do
        {
            shotResponse = _gameService.MakeLocalShot(createResponse.GameId, new ShotRequest(x, y));
            // If hit, player 1 continues, try next cell
            if (shotResponse.Result != ShotResult.Water)
            {
                x++;
                if (x >= 10)
                {
                    x = 0;
                    y++;
                }
            }
        } while (shotResponse.Result != ShotResult.Water && !shotResponse.GameOver);
        
        if (!shotResponse.GameOver)
        {
            var status = _gameService.GetLocalGameStatus(createResponse.GameId);
            
            // Should now be Player 2's turn after a miss
            Assert.Equal("Player 2", status.CurrentPlayerName);
            Assert.Equal(createResponse.Player2Id, status.CurrentPlayerId);
        }
    }

    [Fact]
    public void MakeLocalShot_KeepsTurnOnHit()
    {
        // Create multiple games until we find one where (0,0) has a ship
        GameService gameService;
        CreateLocalGameResponse createResponse;
        ShotResponse shotResponse;
        
        // Try to find a game configuration where we get a hit
        int attempts = 0;
        do
        {
            gameService = new GameService();
            createResponse = gameService.CreateLocalGame(new CreateLocalGameRequest(10));
            shotResponse = gameService.MakeLocalShot(createResponse.GameId, new ShotRequest(0, 0));
            attempts++;
        } while (shotResponse.Result == ShotResult.Water && attempts < 100);
        
        // Skip test if we couldn't get a hit (unlikely, but possible)
        if (shotResponse.Result == ShotResult.Water)
            return;
        
        var status = gameService.GetLocalGameStatus(createResponse.GameId);
        
        // Player 1 should still have the turn after hitting a ship
        Assert.Equal("Player 1", status.CurrentPlayerName);
        Assert.Equal(createResponse.Player1Id, status.CurrentPlayerId);
    }

    [Fact]
    public void MakeShot_KeepsTurnOnHit()
    {
        // Create multiple games until we find one where we get a hit
        GameService gameService;
        CreateGameResponse createResponse;
        JoinGameResponse joinResponse;
        ShotResponse shotResponse;
        
        // Try to find a game configuration where we get a hit
        int attempts = 0;
        do
        {
            gameService = new GameService();
            createResponse = gameService.CreateGame(new CreateGameRequest(10));
            joinResponse = gameService.JoinGame(createResponse.GameId, new JoinGameRequest("Player 2"));
            shotResponse = gameService.MakeShot(createResponse.GameId, createResponse.PlayerId, new ShotRequest(0, 0));
            attempts++;
        } while (shotResponse.Result == ShotResult.Water && attempts < 100);
        
        // Skip test if we couldn't get a hit (unlikely, but possible)
        if (shotResponse.Result == ShotResult.Water)
            return;
        
        var status = gameService.GetGameStatus(createResponse.GameId, createResponse.PlayerId);
        
        // Player 1 should still have the turn after hitting a ship
        Assert.True(status.IsYourTurn);
        Assert.Equal(createResponse.PlayerId, status.CurrentPlayerId);
    }

    [Fact]
    public void MakeShot_SwitchesTurnOnMiss()
    {
        var createResponse = _gameService.CreateGame(new CreateGameRequest(10));
        var joinResponse = _gameService.JoinGame(createResponse.GameId, new JoinGameRequest("Player 2"));
        
        // Keep shooting until we get a miss (Water) to test turn switching
        int x = 0, y = 0;
        ShotResponse shotResponse;
        do
        {
            shotResponse = _gameService.MakeShot(createResponse.GameId, createResponse.PlayerId, new ShotRequest(x, y));
            // If hit, player 1 continues, try next cell
            if (shotResponse.Result != ShotResult.Water)
            {
                x++;
                if (x >= 10)
                {
                    x = 0;
                    y++;
                }
            }
        } while (shotResponse.Result != ShotResult.Water && !shotResponse.GameOver);
        
        if (!shotResponse.GameOver)
        {
            var status = _gameService.GetGameStatus(createResponse.GameId, createResponse.PlayerId);
            
            // Should now be Player 2's turn after a miss
            Assert.False(status.IsYourTurn);
            Assert.Equal(joinResponse.PlayerId, status.CurrentPlayerId);
        }
    }

    [Fact]
    public void MakeLocalShot_SamePositionTwice_ThrowsException()
    {
        var createResponse = _gameService.CreateLocalGame(new CreateLocalGameRequest(10));
        
        // First ensure we miss to get consistent turn switching, then repeat position
        // Find a position that results in a miss, then try to target it again
        int x = 0, y = 0;
        ShotResponse shotResponse;
        
        // Player 1 shoots until they miss
        do
        {
            shotResponse = _gameService.MakeLocalShot(createResponse.GameId, new ShotRequest(x, y));
            if (shotResponse.Result != ShotResult.Water)
            {
                x++;
                if (x >= 10)
                {
                    x = 0;
                    y++;
                }
            }
        } while (shotResponse.Result != ShotResult.Water && !shotResponse.GameOver);
        
        if (shotResponse.GameOver)
            return; // Skip if game ended
        
        int missX = x, missY = y;
        
        // Player 2 shoots until they miss
        x = 0; y = 0;
        do
        {
            shotResponse = _gameService.MakeLocalShot(createResponse.GameId, new ShotRequest(x, y));
            if (shotResponse.Result != ShotResult.Water)
            {
                x++;
                if (x >= 10)
                {
                    x = 0;
                    y++;
                }
            }
        } while (shotResponse.Result != ShotResult.Water && !shotResponse.GameOver);
        
        if (shotResponse.GameOver)
            return; // Skip if game ended
        
        // Now it's Player 1's turn again, try to shoot at the same cell they already targeted
        Assert.Throws<InvalidOperationException>(() => 
            _gameService.MakeLocalShot(createResponse.GameId, new ShotRequest(missX, missY)));
    }

    [Fact]
    public void GetLocalGameStatus_NonExistentGame_ThrowsException()
    {
        Assert.Throws<InvalidOperationException>(() => 
            _gameService.GetLocalGameStatus(Guid.NewGuid()));
    }

    [Fact]
    public void GetLocalGameStatus_NonLocalGame_ThrowsException()
    {
        // Create a regular (network) game, not a local game
        var createResponse = _gameService.CreateGame(new CreateGameRequest(10));
        
        Assert.Throws<InvalidOperationException>(() => 
            _gameService.GetLocalGameStatus(createResponse.GameId));
    }
}

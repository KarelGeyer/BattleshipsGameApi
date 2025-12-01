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
        
        // Player 1 shoots
        var shotRequest = new ShotRequest(0, 0);
        _gameService.MakeShot(createResponse.GameId, createResponse.PlayerId, shotRequest);
        
        // Player 2 shoots at a different position
        _gameService.MakeShot(createResponse.GameId, joinResponse.PlayerId, new ShotRequest(1, 1));
        
        // Player 1 tries to shoot at same position again
        Assert.Throws<InvalidOperationException>(() => 
            _gameService.MakeShot(createResponse.GameId, createResponse.PlayerId, shotRequest));
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
}

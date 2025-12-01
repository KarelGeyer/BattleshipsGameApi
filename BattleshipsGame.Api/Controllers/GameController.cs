using BattleshipsGame.Api.Models;
using BattleshipsGame.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BattleshipsGame.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly IGameService _gameService;

    public GameController(IGameService gameService)
    {
        _gameService = gameService;
    }

    /// <summary>
    /// Create a new game with specified board size (10-20)
    /// </summary>
    [HttpPost("create")]
    public ActionResult<CreateGameResponse> CreateGame([FromBody] CreateGameRequest request)
    {
        try
        {
            var response = _gameService.CreateGame(request);
            return Ok(response);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Join an existing game
    /// </summary>
    [HttpPost("{gameId}/join")]
    public ActionResult<JoinGameResponse> JoinGame(Guid gameId, [FromBody] JoinGameRequest request)
    {
        try
        {
            var response = _gameService.JoinGame(gameId, request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Make a shot at the specified coordinates
    /// </summary>
    [HttpPost("{gameId}/shot")]
    public ActionResult<ShotResponse> MakeShot(Guid gameId, [FromHeader(Name = "X-Player-Id")] Guid playerId, [FromBody] ShotRequest request)
    {
        try
        {
            var response = _gameService.MakeShot(gameId, playerId, request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Get current game status for a player
    /// </summary>
    [HttpGet("{gameId}/status")]
    public ActionResult<GameStatusResponse> GetGameStatus(Guid gameId, [FromHeader(Name = "X-Player-Id")] Guid playerId)
    {
        try
        {
            var response = _gameService.GetGameStatus(gameId, playerId);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Get list of available games to join
    /// </summary>
    [HttpGet("available")]
    public ActionResult<IEnumerable<object>> GetAvailableGames()
    {
        var games = _gameService.GetAvailableGames()
            .Select(g => new { g.Id, g.BoardSize, g.CreatedAt });
        return Ok(games);
    }
}

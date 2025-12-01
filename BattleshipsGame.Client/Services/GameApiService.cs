using System.Net.Http.Json;
using BattleshipsGame.Client.Models;

namespace BattleshipsGame.Client.Services;

public interface IGameApiService
{
    Task<CreateGameResponse?> CreateGameAsync(int boardSize);
    Task<JoinGameResponse?> JoinGameAsync(Guid gameId, string playerName);
    Task<ShotResponse?> MakeShotAsync(Guid gameId, Guid playerId, int x, int y);
    Task<GameStatusResponse?> GetGameStatusAsync(Guid gameId, Guid playerId);
    Task<IEnumerable<AvailableGame>> GetAvailableGamesAsync();
    
    // Local game (single computer) mode
    Task<CreateLocalGameResponse?> CreateLocalGameAsync(int boardSize);
    Task<LocalGameStatusResponse?> GetLocalGameStatusAsync(Guid gameId);
    Task<ShotResponse?> MakeLocalShotAsync(Guid gameId, int x, int y);
}

public class GameApiService : IGameApiService
{
    private readonly HttpClient _httpClient;

    public GameApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CreateGameResponse?> CreateGameAsync(int boardSize)
    {
        var request = new CreateGameRequest(boardSize);
        var response = await _httpClient.PostAsJsonAsync("api/game/create", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CreateGameResponse>();
    }

    public async Task<JoinGameResponse?> JoinGameAsync(Guid gameId, string playerName)
    {
        var request = new JoinGameRequest(playerName);
        var response = await _httpClient.PostAsJsonAsync($"api/game/{gameId}/join", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JoinGameResponse>();
    }

    public async Task<ShotResponse?> MakeShotAsync(Guid gameId, Guid playerId, int x, int y)
    {
        var request = new ShotRequest(x, y);
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"api/game/{gameId}/shot")
        {
            Content = JsonContent.Create(request)
        };
        requestMessage.Headers.Add("X-Player-Id", playerId.ToString());
        
        var response = await _httpClient.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ShotResponse>();
    }

    public async Task<GameStatusResponse?> GetGameStatusAsync(Guid gameId, Guid playerId)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"api/game/{gameId}/status");
        requestMessage.Headers.Add("X-Player-Id", playerId.ToString());
        
        var response = await _httpClient.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GameStatusResponse>();
    }

    public async Task<IEnumerable<AvailableGame>> GetAvailableGamesAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<IEnumerable<AvailableGame>>("api/game/available");
        return response ?? Enumerable.Empty<AvailableGame>();
    }

    public async Task<CreateLocalGameResponse?> CreateLocalGameAsync(int boardSize)
    {
        var request = new CreateLocalGameRequest(boardSize);
        var response = await _httpClient.PostAsJsonAsync("api/game/local/create", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CreateLocalGameResponse>();
    }

    public async Task<LocalGameStatusResponse?> GetLocalGameStatusAsync(Guid gameId)
    {
        var response = await _httpClient.GetFromJsonAsync<LocalGameStatusResponse>($"api/game/local/{gameId}/status");
        return response;
    }

    public async Task<ShotResponse?> MakeLocalShotAsync(Guid gameId, int x, int y)
    {
        var request = new ShotRequest(x, y);
        var response = await _httpClient.PostAsJsonAsync($"api/game/local/{gameId}/shot", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ShotResponse>();
    }
}

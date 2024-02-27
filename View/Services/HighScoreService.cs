using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Model.Entities;

namespace View.Services;

public class HighScoreService(IHttpClientFactory clientFactory) {

    public async Task<List<HighScore>> GetHighScoresAsync(CancellationToken ct) {
        var client = clientFactory.CreateClient("HighScoreClient");
        
        var response = await client.GetAsync(client.BaseAddress, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return [];
        
        var highScores =  await response.Content.ReadFromJsonAsync<List<HighScore>>(ct);
        
        return highScores ?? [];
    }
}
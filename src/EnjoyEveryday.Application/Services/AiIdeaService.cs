using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace EnjoyEveryday.Application.Services;

public class AiIdeaService : IAiIdeaService
{
    private readonly HttpClient _httpClient;

    public AiIdeaService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<AiSuggestion>> GenerateIdeasAsync(string ideaInput)
    {
        var request = new { idea = ideaInput };
        var response = await _httpClient.PostAsJsonAsync("http://127.0.0.1:8000/api/ideas", request);

        if (response.IsSuccessStatusCode)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = await response.Content.ReadFromJsonAsync<AiIdeaResponse>(options);
            return result?.Suggestions ?? new List<AiSuggestion>();
        }

        return new List<AiSuggestion>();
    }

    private class AiIdeaResponse
    {
        public List<AiSuggestion>? Suggestions { get; set; }
    }
}

using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class AiIdeaService : IAiIdeaService
{
    private readonly HttpClient _httpClient;
    private readonly TenantSettingsService _settingsService;
    private readonly ITenantContext _tenantContext;

    public AiIdeaService(HttpClient httpClient, TenantSettingsService settingsService, ITenantContext tenantContext)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<ExperienceFullData>> GenerateIdeasAsync(string ideaInput)
    {
        var apiKey = await _settingsService.GetAiApiKeyAsync(_tenantContext.TenantId);
        if (string.IsNullOrEmpty(apiKey)) return new List<ExperienceFullData>();

        var request = new { idea = ideaInput };
        
        var message = new HttpRequestMessage(HttpMethod.Post, "http://127.0.0.1:8000/api/ideas");
        message.Headers.Add("X-Tenant-Api-Key", apiKey);
        message.Content = JsonContent.Create(request);

        var response = await _httpClient.SendAsync(message);

        if (response.IsSuccessStatusCode)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = await response.Content.ReadFromJsonAsync<AiIdeaResponse>(options);
            return result?.Suggestions ?? new List<ExperienceFullData>();
        }

        return new List<ExperienceFullData>();
    }
    
    public async Task<ExperienceImprovementResult?> ImproveExperienceAsync(object experienceDna, string requestContext)
    {
        var apiKey = await _settingsService.GetAiApiKeyAsync(_tenantContext.TenantId);
        if (string.IsNullOrEmpty(apiKey)) return null;

        var request = new { experienceDna, requestContext };
        
        var message = new HttpRequestMessage(HttpMethod.Post, "http://127.0.0.1:8000/api/improve");
        message.Headers.Add("X-Tenant-Api-Key", apiKey);
        message.Content = JsonContent.Create(request);

        var response = await _httpClient.SendAsync(message);

        if (response.IsSuccessStatusCode)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = await response.Content.ReadFromJsonAsync<ExperienceImprovementResult>(options);
            return result;
        }

        return null;
    }

    public async Task<IEnumerable<string>> SuggestMaterialsAsync(object experienceDna)
    {
        var apiKey = await _settingsService.GetAiApiKeyAsync(_tenantContext.TenantId);
        if (string.IsNullOrEmpty(apiKey)) return new List<string>();

        var request = new { experienceDna };
        
        var message = new HttpRequestMessage(HttpMethod.Post, "http://127.0.0.1:8000/api/materials");
        message.Headers.Add("X-Tenant-Api-Key", apiKey);
        message.Content = JsonContent.Create(request);

        var response = await _httpClient.SendAsync(message);

        if (response.IsSuccessStatusCode)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = await response.Content.ReadFromJsonAsync<AiMaterialsResponse>(options);
            return result?.Materials ?? new List<string>();
        }

        return new List<string>();
    }

    private class AiMaterialsResponse
    {
        public List<string>? Materials { get; set; }
    }

    private class AiIdeaResponse
    {
        public List<ExperienceFullData>? Suggestions { get; set; }
    }
}

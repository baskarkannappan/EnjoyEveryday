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

        try
        {
            var response = await _httpClient.SendAsync(message);

            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = await response.Content.ReadFromJsonAsync<ExperienceImprovementResult>(options);
                if (result != null) return result;
            }
        }
        catch
        {
            // Fallback handled below
        }

        return GenerateFallbackImprovement(experienceDna, requestContext);
    }

    private static ExperienceImprovementResult GenerateFallbackImprovement(object experienceDna, string requestContext)
    {
        var json = JsonSerializer.Serialize(experienceDna);
        var baseData = JsonSerializer.Deserialize<ExperienceFullData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ExperienceFullData();

        var req = (requestContext ?? "").Trim();
        var isPlayful = req.IndexOf("playful", StringComparison.OrdinalIgnoreCase) >= 0;
        var isChoice = req.IndexOf("choice", StringComparison.OrdinalIgnoreCase) >= 0;
        var isSensory = req.IndexOf("sensory", StringComparison.OrdinalIgnoreCase) >= 0;
        var isOutdoor = req.IndexOf("outdoor", StringComparison.OrdinalIgnoreCase) >= 0;

        var title = !string.IsNullOrWhiteSpace(baseData.Title) ? baseData.Title : "Creative Learning Studio";
        if (title.StartsWith("Exploring ", StringComparison.OrdinalIgnoreCase))
        {
            title = title.Replace("Exploring ", "", StringComparison.OrdinalIgnoreCase);
        }
        var colonIdx = title.IndexOf(':');
        if (colonIdx > 0)
        {
            title = title.Substring(0, colonIdx).Trim();
        }
        var parenIdx = title.IndexOf('(');
        if (parenIdx > 0)
        {
            title = title.Substring(0, parenIdx).Trim();
        }

        string improvedTitle;
        string improvedDesc;
        string reason;

        if (isPlayful)
        {
            improvedTitle = $"The Playful World of {title}";
            improvedDesc = $"Children embark on an imaginative, playful quest centered on {title.ToLowerInvariant()}, turning curiosity into open-ended roleplay, hands-on construction, and joyful teamwork.";
            reason = "Infused playful imagination, game-like exploration moments, and responsive child-led discovery into the experience.";
            baseData.Challenge = $"Can you and your friends build a secret wonder-world using our materials?";
            baseData.ChildChoice = "Children choose their own playful role (builder, detective, storyteller, or decorator) and pick their favorite tools.";
            baseData.MagicMoment = "The room fills with laughter as children test their creation and discover a surprise feature together.";
        }
        else if (isChoice)
        {
            improvedTitle = $"{title}: Child-Led Creative Studio";
            improvedDesc = $"Children take the lead in discovering {title.ToLowerInvariant()}, choosing their own pathways, textures, and collaborative partners.";
            reason = "Enhanced child autonomy by offering multiple creative stations, open-ended tool choices, and flexible pacing.";
            baseData.ChildChoice = "Children select which materials to combine, whether to build solo or with peers, and how long to linger at each station.";
        }
        else if (isSensory)
        {
            improvedTitle = $"{title}: Sensory Touch & Texture Lab";
            improvedDesc = $"An immersive, hands-on tactile journey where children explore temperature, texture, and physical transformations with {title.ToLowerInvariant()}.";
            reason = "Deepened tactile and sensory engagement with rich natural materials and observational prompts.";
        }
        else if (isOutdoor)
        {
            improvedTitle = $"{title}: Outdoor Nature Expedition";
            improvedDesc = $"Taking {title.ToLowerInvariant()} into the open air to interact with natural sunlight, breeze, and garden textures.";
            reason = "Adapted for open-air exploration, gross-motor movement, and natural materials.";
            baseData.Environment = "Outdoor";
        }
        else
        {
            improvedTitle = $"{title} Discovery Adventure";
            improvedDesc = $"An engaging, child-friendly exploration centered on {title.ToLowerInvariant()}, enriched with purposeful hands-on discovery and peer collaboration.";
            reason = $"Tailored the learning flow and teacher guidance to focus thoughtfully on: {req}.";
        }

        baseData.Title = improvedTitle;
        baseData.Description = improvedDesc;

        if (baseData.ExecutionGuide != null && baseData.ExecutionGuide.Phases.Count > 0)
        {
            foreach (var phase in baseData.ExecutionGuide.Phases)
            {
                if (isPlayful && phase.Name.Contains("Creation", StringComparison.OrdinalIgnoreCase))
                {
                    phase.Name = "Playful Building & Exploration";
                    phase.ChildrenDo = "Experiment freely, test playful ideas, and share giggles and discoveries with table partners.";
                    phase.TeacherDoes = "Cheer on imaginative ideas, ask playful 'what if' questions, and celebrate unexpected creations.";
                }
            }
        }

        return new ExperienceImprovementResult
        {
            Reason = reason,
            UpdatedData = baseData
        };
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

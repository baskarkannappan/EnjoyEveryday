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
        if (string.IsNullOrWhiteSpace(ideaInput)) return new List<ExperienceFullData>();

        try
        {
            var apiKey = await _settingsService.GetAiApiKeyAsync(_tenantContext.TenantId);
            if (!string.IsNullOrEmpty(apiKey))
            {
                var request = new { idea = ideaInput };
                using var message = new HttpRequestMessage(HttpMethod.Post, "/api/ideas");
                message.Headers.Add("X-Tenant-Api-Key", apiKey);
                message.Content = JsonContent.Create(request);

                var response = await _httpClient.SendAsync(message);
                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = await response.Content.ReadFromJsonAsync<AiIdeaResponse>(options);
                    if (result?.Suggestions != null && result.Suggestions.Count > 0)
                    {
                        return result.Suggestions;
                    }
                }
            }
        }
        catch
        {
            // Fallback handled below
        }

        return GenerateFallbackIdeas(ideaInput);
    }
    public async Task<ExperienceImprovementResult?> ImproveExperienceAsync(object experienceDna, string requestContext)
    {
        var request = new { experienceDna, requestContext };
        try
        {
            var apiKey = await _settingsService.GetAiApiKeyAsync(_tenantContext.TenantId);
            if (!string.IsNullOrEmpty(apiKey))
            {
                using var message = new HttpRequestMessage(HttpMethod.Post, "/api/improve");
                message.Headers.Add("X-Tenant-Api-Key", apiKey);
                message.Content = JsonContent.Create(request);

                var response = await _httpClient.SendAsync(message);
                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = await response.Content.ReadFromJsonAsync<ExperienceImprovementResult>(options);
                    if (result != null) return result;
                }
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

        // Clean out any existing prefixes or suffixes to avoid compounding like "The Playful World of The Playful World of..."
        var prefixesToStrip = new[] { "The Playful World of ", "The Playful World Of ", "The Magic of ", "Exploring " };
        foreach (var pfx in prefixesToStrip)
        {
            if (title.StartsWith(pfx, StringComparison.OrdinalIgnoreCase))
            {
                title = title.Substring(pfx.Length).Trim();
            }
        }
        var suffixesToStrip = new[] { " Discovery Adventure", ": Child-Led Creative Studio", ": Sensory Touch & Texture Lab", ": Outdoor Nature Expedition" };
        foreach (var sfx in suffixesToStrip)
        {
            if (title.EndsWith(sfx, StringComparison.OrdinalIgnoreCase))
            {
                title = title.Substring(0, title.Length - sfx.Length).Trim();
            }
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
        try
        {
            var apiKey = await _settingsService.GetAiApiKeyAsync(_tenantContext.TenantId);
            if (!string.IsNullOrEmpty(apiKey))
            {
                var request = new { experienceDna };
                using var message = new HttpRequestMessage(HttpMethod.Post, "/api/materials");
                message.Headers.Add("X-Tenant-Api-Key", apiKey);
                message.Content = JsonContent.Create(request);

                var response = await _httpClient.SendAsync(message);
                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = await response.Content.ReadFromJsonAsync<AiMaterialsResponse>(options);
                    if (result?.Materials != null && result.Materials.Count > 0)
                    {
                        return result.Materials;
                    }
                }
            }
        }
        catch
        {
            // Fallback handled below
        }

        return GenerateFallbackMaterials(experienceDna);
    }

    private static IEnumerable<ExperienceFullData> GenerateFallbackIdeas(string ideaInput)
    {
        var cleanTopic = CleanTopic(ideaInput);

        return new List<ExperienceFullData>
        {
            new()
            {
                Title = $"{cleanTopic}: Creative Tactile Studio",
                Description = $"Children engage in open-ended creative construction and tactile exploration centered on {cleanTopic.ToLowerInvariant()}, combining colorful patterns, cutouts, and child-safe materials.",
                Challenge = $"Can we design and build a colorful tactile display inspired by {cleanTopic.ToLowerInvariant()}?",
                Together = "Children share building materials and describe what they are making to their peers.",
                Create = $"A tangible collaborative {cleanTopic.ToLowerInvariant()} model or collage.",
                ChildChoice = "Children choose which textures, shapes, and tools they wish to combine.",
                MagicMoment = "Noticing how different materials join together to form a new creation.",
                Accomplishment = "Presenting their completed project during reflection circle.",
                Reflection = $"What was your favorite texture or tool you used while exploring {cleanTopic.ToLowerInvariant()} today?",
                TeacherGuidance = "Introduce materials at the low table. Encourage children to test how pieces fit together.",
                AgeMin = 3,
                AgeMax = 5,
                GroupSizeMin = 2,
                GroupSizeMax = 6,
                Environment = "Indoor",
                DevelopmentalGoals = new List<string> { "Fine Motor Skills", "Creative Expression", "Social Cooperation" },
                Materials = new List<string> { $"{cleanTopic} reference cards", "Precut shapes & silhouettes", "Colored patterned paper", "Washable craft adhesive", "Wooden craft sticks", "Textured paper scraps" },
                ExecutionGuide = new Domain.Entities.ExecutionGuide
                {
                    Preparation = new List<string> { "Cut paper into varied shapes", "Set out trays on low work tables" },
                    EnvironmentSetup = new List<string> { "Spacious floor or craft table with accessible trays" },
                    MaterialSetup = new List<string> { "Place trays evenly within reach of small groups" },
                    SuggestedDurationMinutes = 25,
                    TeacherIntroduction = $"Today we are exploring {cleanTopic.ToLowerInvariant()}! Look at all these interesting textures we can build with.",
                    Phases = new List<Domain.Entities.ExecutionPhase>
                    {
                        new() { Name = "Welcome & Material Touch", DurationMinutes = 5, ChildrenDo = "Feel materials and select initial pieces", TeacherDoes = "Demonstrates gentle handling and asks open questions" },
                        new() { Name = "Hands-On Creation", DurationMinutes = 15, ChildrenDo = "Assemble and combine elements", TeacherDoes = "Circulates, providing positive reinforcement" },
                        new() { Name = "Sharing & Clean-Up", DurationMinutes = 5, ChildrenDo = "Place finished crafts on drying rack and tidy up", TeacherDoes = "Leads a gentle closing transition" }
                    },
                    Watch = "Observe hand-eye coordination and peer turn-taking.",
                    Ask = "How does that piece feel when you touch it?",
                    Try = "Try connecting two different shapes together.",
                    Safety = new List<string> { "Ensure only child-safe non-toxic adhesive is used", "Remind children that materials remain on the table" },
                    Adaptations = new List<string> { "Provide larger chunky pieces for developing fine-motor grip" },
                    Extensions = new List<string> { "Take photos of finished structures to create a classroom storybook" },
                    Closing = "Gather on the carpet for a quick proud celebration of everyone's work.",
                    LittleMomentOpportunity = "Praise a child who noticed an interesting detail in their friend's project."
                }
            },
            new()
            {
                Title = $"Investigating {cleanTopic}: Sensory Wonder Lab",
                Description = $"An investigative station where children inspect, sort, and discover {cleanTopic.ToLowerInvariant()} through sensory observation and inquiry.",
                Challenge = $"What can we discover when we look closely and listen to our clues?",
                Together = "Children work in pairs to compare findings and share magnifying glasses.",
                Create = $"A group discovery chart recording observations about {cleanTopic.ToLowerInvariant()}.",
                ChildChoice = "Children choose which station tool (magnifier, scoop, sorting tray) to investigate with first.",
                MagicMoment = "The spark of excitement when a child uncovers a hidden pattern.",
                Accomplishment = "Contributing one personal observation to the group chart.",
                Reflection = $"What surprised you the most about {cleanTopic.ToLowerInvariant()}?",
                TeacherGuidance = "Model curiosity by asking open-ended questions like 'What do you notice here?' rather than giving answers.",
                AgeMin = 3,
                AgeMax = 5,
                GroupSizeMin = 2,
                GroupSizeMax = 5,
                Environment = "Indoor",
                DevelopmentalGoals = new List<string> { "Scientific Inquiry", "Sensory Processing", "Language Development" },
                Materials = new List<string> { "Magnifying glasses", "Sorting bowls", "Observation clipboards", "Washable markers", "Sensory discovery trays" },
                ExecutionGuide = new Domain.Entities.ExecutionGuide
                {
                    Preparation = new List<string> { "Prepare sensory bins with themed items", "Place magnifying glasses near trays" },
                    EnvironmentSetup = new List<string> { "Sensory table or quiet corner with natural lighting" },
                    MaterialSetup = new List<string> { "Arrange tools neatly in low baskets" },
                    SuggestedDurationMinutes = 20,
                    TeacherIntroduction = $"We have a mystery discovery lab today all about {cleanTopic.ToLowerInvariant()}!",
                    Phases = new List<Domain.Entities.ExecutionPhase>
                    {
                        new() { Name = "Curious Arrival", DurationMinutes = 5, ChildrenDo = "Examine tools and trays", TeacherDoes = "Introduces observational vocabulary" },
                        new() { Name = "Sensory Investigation", DurationMinutes = 10, ChildrenDo = "Sort, touch, and inspect items", TeacherDoes = "Listens and records child quotes" },
                        new() { Name = "Group Reflection", DurationMinutes = 5, ChildrenDo = "Share what they noticed with a partner", TeacherDoes = "Adds child ideas to observation chart" }
                    },
                    Watch = "Notice how children describe textures, colors, and weights.",
                    Ask = "What does this remind you of?",
                    Try = "Can you find two things that feel completely different?",
                    Safety = new List<string> { "Ensure all observation items are choking-hazard safe for the age group" },
                    Adaptations = new List<string> { "Offer tactile tools with easy-grip handles" },
                    Extensions = new List<string> { "Draw what was seen on child-sized sketch sheets" },
                    Closing = "Thank each explorer for their careful eyes and kind teamwork.",
                    LittleMomentOpportunity = "Notice a child helping their peer focus their magnifying glass."
                }
            }
        };
    }

    private static IEnumerable<string> GenerateFallbackMaterials(object experienceDna)
    {
        var json = JsonSerializer.Serialize(experienceDna);
        var baseData = JsonSerializer.Deserialize<ExperienceFullData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ExperienceFullData();

        var materials = new List<string>
        {
            "Child-safe washable markers",
            "Heavyweight construction paper",
            "Natural sorting trays",
            "Pre-cut textured paper strips",
            "Wooden counting and building sticks"
        };

        if (!string.IsNullOrWhiteSpace(baseData.Title))
        {
            materials.Insert(0, $"{CleanTopic(baseData.Title)} themed flashcards / picture prompts");
        }

        return materials;
    }

    private static string CleanTopic(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "Learning Adventure";
        var cleaned = input.Trim();

        var prefixes = new[] { "Exploring ", "The Magic of ", "The Playful World of ", "Investigating ", "A study of ", "The children are interested in ", "Children love ", "Learning about " };
        foreach (var pfx in prefixes)
        {
            if (cleaned.StartsWith(pfx, StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(pfx.Length).Trim();
            }
        }

        if (cleaned.Length > 40)
        {
            cleaned = cleaned.Substring(0, 40).Trim();
        }

        return string.IsNullOrWhiteSpace(cleaned) ? "Creative Exploration" : char.ToUpper(cleaned[0]) + cleaned.Substring(1);
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

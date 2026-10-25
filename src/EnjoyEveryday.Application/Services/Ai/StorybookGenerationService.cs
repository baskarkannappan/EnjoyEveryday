using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Entities.Learning;

namespace EnjoyEveryday.Application.Services.Ai;

public interface IStorybookGenerationService
{
    Storybook GenerateStorybook(Experience exp, ExperienceDna dna);
    StorybookValidationResult ValidateStorybook(Storybook storybook);
}

public class StorybookGenerationService : IStorybookGenerationService
{
    public Storybook GenerateStorybook(Experience exp, ExperienceDna dna)
    {
        var type = DetectExperienceType(exp, dna);
        var cleanTitle = CleanTitle(exp.Title);
        var palette = DeterminePaletteTheme(type, cleanTitle);

        var duration = dna.ExecutionGuide?.SuggestedDurationMinutes > 0
            ? dna.ExecutionGuide.SuggestedDurationMinutes
            : 25;

        var storybook = new Storybook
        {
            Id = Guid.NewGuid(),
            ExperienceId = exp.Id,
            TenantId = exp.TenantId,
            OrganizationId = exp.OrganizationId,
            Title = cleanTitle,
            Subtitle = CleanDescription(exp.Description, cleanTitle),
            Summary = $"An engaging {type.ToLowerInvariant()} exploration designed for ages {dna.AgeMin}-{dna.AgeMax} with intentional hands-on discovery and peer collaboration.",
            ExperienceType = type,
            AgeMin = dna.AgeMin > 0 ? dna.AgeMin : 3,
            AgeMax = dna.AgeMax > 0 ? dna.AgeMax : 5,
            GroupSizeMin = dna.GroupSizeMin > 0 ? dna.GroupSizeMin : 4,
            GroupSizeMax = dna.GroupSizeMax > 0 ? dna.GroupSizeMax : 12,
            Setting = !string.IsNullOrWhiteSpace(dna.Environment) ? dna.Environment : "Indoor / Outdoor",
            TotalDurationMinutes = duration,
            CoverIllustrationKey = DetermineCoverIllustrationKey(type, cleanTitle),
            PaletteTheme = palette,
            LearningIntentions = dna.DevelopmentalGoals != null && dna.DevelopmentalGoals.Count > 0
                ? dna.DevelopmentalGoals
                : GenerateDefaultLearningIntentions(type),
            DevelopmentDomains = DetermineDevelopmentDomains(type, dna),
            RequiredMaterials = dna.Materials != null && dna.Materials.Count > 0
                ? dna.Materials
                : GenerateDefaultMaterials(type, cleanTitle),
            OptionalMaterials = new List<string> { "Magnifying mirrors", "Sensory texture cloth", "Photo observation clipboard" },
            MaterialSubstitutions = GenerateMaterialSubstitutions(type),
            PreparationSteps = dna.ExecutionGuide?.Preparation != null && dna.ExecutionGuide.Preparation.Count > 0
                ? dna.ExecutionGuide.Preparation
                : GeneratePreparationSteps(type, cleanTitle),
            EnvironmentSetupSteps = dna.ExecutionGuide?.EnvironmentSetup != null && dna.ExecutionGuide.EnvironmentSetup.Count > 0
                ? dna.ExecutionGuide.EnvironmentSetup
                : GenerateEnvironmentSetup(type),
            SafetyRisksAndMitigations = GenerateSafetyRisks(dna.AgeMin, type, dna.Materials),
            AccessibilityAdaptations = dna.ExecutionGuide?.Adaptations != null && dna.ExecutionGuide.Adaptations.Count > 0
                ? dna.ExecutionGuide.Adaptations
                : GenerateAccessibilityAdaptations(type),
            SensoryConsiderations = GenerateSensoryConsiderations(type),
            ReflectionPrompt = !string.IsNullOrWhiteSpace(dna.Reflection)
                ? dna.Reflection
                : $"What was your favorite secret or surprise you discovered while exploring {cleanTitle.ToLowerInvariant()} today?",
            CleanupRitual = "Sing the gentle transition chime. Children place materials back in their respective color trays with partner high-fives.",
            LittleMomentObservationOpportunity = !string.IsNullOrWhiteSpace(dna.ExecutionGuide?.LittleMomentOpportunity)
                ? dna.ExecutionGuide.LittleMomentOpportunity
                : "Notice a child gently assisting a peer or using descriptive adjectives to explain their unique creation.",
            Status = exp.Status == "Published" ? "Published" : "Draft"
        };

        storybook.Chapters = GenerateChapters(exp, dna, type, cleanTitle);
        storybook.Validation = ValidateStorybook(storybook);
        storybook.IsSafetyApproved = storybook.Validation.IsValid && storybook.Validation.SafetyFlags.Count == 0;

        return storybook;
    }

    public StorybookValidationResult ValidateStorybook(Storybook storybook)
    {
        var res = new StorybookValidationResult();

        if (string.IsNullOrWhiteSpace(storybook.Title))
            res.Errors.Add("Storybook title is required.");

        if (storybook.Chapters.Count < 2)
            res.Errors.Add("Storybook requires at least 2 distinct developmental stages or chapters.");

        var totalChapterDuration = storybook.Chapters.Sum(c => c.DurationMinutes);
        if (Math.Abs(totalChapterDuration - storybook.TotalDurationMinutes) > 5)
        {
            res.Warnings.Add($"Total chapter duration ({totalChapterDuration} min) deviates from planned duration ({storybook.TotalDurationMinutes} min).");
        }

        // Safety rules for toddler / preschool
        if (storybook.AgeMin < 3)
        {
            foreach (var mat in storybook.RequiredMaterials)
            {
                var lower = mat.ToLowerInvariant();
                if (lower.Contains("bead") || lower.Contains("marble") || lower.Contains("button") || lower.Contains("small stone"))
                {
                    res.SafetyFlags.Add($"Choking hazard alert: '{mat}' detected for age group under 3 years. Supervised sensory bottles or chunky items required.");
                }
            }
        }

        // Water safety
        var fullText = $"{storybook.Title} {storybook.Subtitle} {string.Join(" ", storybook.RequiredMaterials)}".ToLowerInvariant();
        if (fullText.Contains("water table") || fullText.Contains("splash") || fullText.Contains("puddle") || fullText.Contains("deep water"))
        {
            res.SafetyFlags.Add("Water play safety: Non-slip floor mats and continuous teacher direct sightline required.");
        }

        return res;
    }

    private static string DetectExperienceType(Experience exp, ExperienceDna dna)
    {
        var text = $"{exp.Title} {exp.Description} {dna.Challenge} {string.Join(" ", dna.Materials ?? new())}".ToLowerInvariant();

        if (text.Contains("rescue") || text.Contains("quest") || text.Contains("adventure") || text.Contains("journey") || text.Contains("reef") || text.Contains("pirate") || text.Contains("safari"))
            return "Narrative Adventure";

        if (text.Contains("sensory") || text.Contains("touch") || text.Contains("texture") || text.Contains("water") || text.Contains("sand") || text.Contains("slime") || text.Contains("ice"))
            return "Sensory Exploration";

        if (text.Contains("paint") || text.Contains("sculpt") || text.Contains("clay") || text.Contains("collage") || text.Contains("craft") || text.Contains("draw") || text.Contains("build"))
            return "Creative Art & Construction";

        if (text.Contains("garden") || text.Contains("nature") || text.Contains("leaf") || text.Contains("insect") || text.Contains("bug") || text.Contains("sunlight") || text.Contains("outdoor"))
            return "Outdoor Discovery";

        if (text.Contains("sort") || text.Contains("count") || text.Contains("pattern") || text.Contains("stem") || text.Contains("magnet") || text.Contains("shadow") || text.Contains("balance"))
            return "Early STEM & Inquiry";

        if (text.Contains("dance") || text.Contains("music") || text.Contains("song") || text.Contains("rhythm") || text.Contains("jump") || text.Contains("movement") || text.Contains("crawl"))
            return "Music & Movement";

        return "Cooperative Wonder Lab";
    }

    private static string DeterminePaletteTheme(string type, string title)
    {
        var t = title.ToLowerInvariant();
        if (t.Contains("reef") || t.Contains("ocean") || t.Contains("water") || t.Contains("sea"))
            return "ocean-coral";
        if (t.Contains("sun") || t.Contains("fire") || t.Contains("warm") || t.Contains("sand"))
            return "solar-warm";
        if (type == "Outdoor Discovery" || type == "Sensory Exploration")
            return "nature-sage";
        return "creative-indigo";
    }

    private static string DetermineCoverIllustrationKey(string type, string title)
    {
        var t = title.ToLowerInvariant();
        if (t.Contains("reef") || t.Contains("ocean") || t.Contains("fish") || t.Contains("sea"))
            return "reef";
        if (t.Contains("forest") || t.Contains("tree") || t.Contains("garden") || t.Contains("nature"))
            return "nature";
        if (t.Contains("clay") || t.Contains("paint") || t.Contains("build") || t.Contains("block"))
            return "workshop";
        return "wonderlab";
    }

    private static List<StorybookChapter> GenerateChapters(Experience exp, ExperienceDna dna, string type, string cleanTitle)
    {
        var chapters = new List<StorybookChapter>();
        var phases = dna.ExecutionGuide?.Phases;

        if (phases != null && phases.Count >= 3)
        {
            for (int i = 0; i < phases.Count; i++)
            {
                var p = phases[i];
                var isFirst = i == 0;
                var isLast = i == phases.Count - 1;

                chapters.Add(new StorybookChapter
                {
                    ChapterNumber = i + 1,
                    Title = !string.IsNullOrWhiteSpace(p.Name) ? p.Name : $"Stage {i + 1}",
                    PhaseTag = isFirst ? "Step 1: Invitation & Setup" : (isLast ? $"Step {i + 1}: Celebration & Reflection" : $"Step {i + 1}: Active Discovery"),
                    DurationMinutes = p.DurationMinutes > 0 ? p.DurationMinutes : 5,
                    ChildNarrative = GenerateChildNarrative(i, phases.Count, type, cleanTitle, p.ChildrenDo, dna),
                    TeacherDialogueScript = !string.IsNullOrWhiteSpace(p.TeacherDoes) && p.TeacherDoes.Contains("\"")
                        ? p.TeacherDoes
                        : GenerateTeacherDialogue(i, phases.Count, cleanTitle, p.TeacherDoes, dna),
                    TeacherFacilitationNotes = !string.IsNullOrWhiteSpace(p.TeacherDoes) ? p.TeacherDoes : "Facilitate open-ended discovery and observe child peer communication.",
                    ChildrenAction = !string.IsNullOrWhiteSpace(p.ChildrenDo) ? p.ChildrenDo : "Children engage playfully with curiosity and test ideas.",
                    ChildChoicePrompt = isFirst
                        ? "Children pick their preferred tool or starting station."
                        : (isLast ? "Children choose which triumph to share with the circle." : "Children choose how to assemble, sort, or interact with materials."),
                    WonderQuestion = !string.IsNullOrWhiteSpace(dna.ExecutionGuide?.Ask) ? dna.ExecutionGuide.Ask : "What do you wonder when you look closely at this?",
                    WatchFor = !string.IsNullOrWhiteSpace(dna.ExecutionGuide?.Watch) ? dna.ExecutionGuide.Watch : "Observe hand-eye coordination and positive peer sharing.",
                    TransitionToNext = isLast ? "Gentle deep breath and transition to handwashing." : "Invite partners to carry their discoveries to the next station.",
                    IllustrationKey = isFirst ? "gathering" : (isLast ? "reflection" : (i == 1 ? "challenge" : "explore")),
                    StepIcon = isFirst ? "🌿" : (isLast ? "🌟" : "🎨")
                });
            }
        }
        else
        {
            // Standard 4-chapter pedagogical arc tailored to type
            chapters.Add(new StorybookChapter
            {
                ChapterNumber = 1,
                Title = "The Curious Invitation & Gathering",
                PhaseTag = "Step 1: Gathering",
                DurationMinutes = 5,
                ChildNarrative = $"Hark, curious explorers! Today an exciting mystery is waiting for us all about {cleanTitle.ToLowerInvariant()}. Come feel the soft textures and take a gentle explorer breath together.",
                TeacherDialogueScript = $"\"Welcome, brave explorers! Look at what has arrived on our discovery carpet today. What do your fingers notice when you gently touch this?\"",
                TeacherFacilitationNotes = "Introduce materials gently. Encourage touching, smelling, and whispering observations without rushing to construct.",
                ChildrenAction = "Gather around the low work mat, handle materials with gentle touch, and share eager guesses.",
                ChildChoicePrompt = "Children choose whether to explore with their bare fingers, a magnifying glass, or a scoop.",
                WonderQuestion = "What secret do you think is hiding inside our discovery basket?",
                WatchFor = "Observe non-verbal expressions of wonder and curiosity.",
                StepIcon = "🌿",
                IllustrationKey = "gathering"
            });

            chapters.Add(new StorybookChapter
            {
                ChapterNumber = 2,
                Title = "The Wonder Challenge",
                PhaseTag = "Step 2: The Challenge",
                DurationMinutes = 5,
                ChildNarrative = $"Can we work as a team to solve our mission? {dna.Challenge}. Let's test our ideas and see what surprises we can build!",
                TeacherDialogueScript = $"\"I wonder what will happen if we combine these two textures? What does your imagination tell you?\"",
                TeacherFacilitationNotes = "Frame the activity not as a test of right or wrong, but as an open-ended science or artistic quest.",
                ChildrenAction = "Experiment freely, test combinations, and share tools with smiling partners.",
                ChildChoicePrompt = "Children decide if they wish to work solo or collaborate with a table neighbor.",
                WonderQuestion = dna.Challenge,
                WatchFor = "Notice how children negotiate shared space and materials.",
                StepIcon = "🔍",
                IllustrationKey = "challenge"
            });

            chapters.Add(new StorybookChapter
            {
                ChapterNumber = 3,
                Title = "Deep Hands-On Flow & Creation",
                PhaseTag = "Step 3: Deep Flow",
                DurationMinutes = 10,
                ChildNarrative = $"The room hums with joyful energy! Hands mold, sort, and arrange wonderful designs. Look—a magical breakthrough happens right before our eyes!",
                TeacherDialogueScript = $"\"Look at that incredible focus! You found a brand new way to balance those pieces—tell me about what you are creating!\"",
                TeacherFacilitationNotes = "Step back slightly to honor child flow. Narrate process rather than passing quick judgment.",
                ChildrenAction = "Deeply engaged in flow state: sorting colors, building structures, testing balance, and sharing supplies kindly.",
                ChildChoicePrompt = "Children select how tall, wide, or detailed their individual creation becomes.",
                WonderQuestion = "What happens if we gently try one more piece on top?",
                WatchFor = "Pincer grip control, resilience when pieces tip over, and spontaneous roleplay dialogue.",
                StepIcon = "🎨",
                IllustrationKey = "explore"
            });

            chapters.Add(new StorybookChapter
            {
                ChapterNumber = 4,
                Title = "Reef Celebration & Story Sharing",
                PhaseTag = "Step 4: Celebration",
                DurationMinutes = 5,
                ChildNarrative = $"We did it! Every little creation shines with personality and care. Hold up your hands for a quiet group cheer for our wonderful teamwork today.",
                TeacherDialogueScript = $"\"Look at what our classroom created together today! Who would like to show their favorite secret spot in their project?\"",
                TeacherFacilitationNotes = "Celebrate the effort and journey. Record child quotes on the classroom documentation board.",
                ChildrenAction = "Proudly show creations, explain choices, and participate in a peaceful tidy-up ritual.",
                ChildChoicePrompt = "Children choose one adjective to describe their finished creation.",
                WonderQuestion = "What was the most surprising thing your hands discovered today?",
                WatchFor = "Expressions of earned pride and warm peer appreciation.",
                StepIcon = "🌟",
                IllustrationKey = "reflection"
            });
        }

        return chapters;
    }

    private static string GenerateChildNarrative(int index, int total, string type, string title, string? childrenDo, ExperienceDna dna)
    {
        if (index == 0)
            return $"Welcome to our {title} quest! Put on your imaginary explorer hats and let's discover what wonders are waiting for our hands.";
        if (index == total - 1)
            return $"Look at what we accomplished together! Our whole room is glowing with pride and creative wonder.";
        return !string.IsNullOrWhiteSpace(childrenDo)
            ? $"Now the adventure deepens: {childrenDo}"
            : $"Little hands explore, mold, and test new ideas as our {title} world comes alive!";
    }

    private static string GenerateTeacherDialogue(int index, int total, string title, string? teacherDoes, ExperienceDna dna)
    {
        if (index == 0)
            return $"\"Come sit close, little explorers! What do your eyes and fingers notice about our materials today?\"";
        if (index == total - 1)
            return $"\"Look at each of your triumphs! Who would like to tell our circle the story behind their creation?\"";
        return $"\"You are trying so many creative ideas! Tell me what's happening in your hands right now.\"";
    }

    private static List<string> GenerateDefaultLearningIntentions(string type)
    {
        return new List<string>
        {
            "Foster tactile sensory curiosity and fine-motor dexterity",
            "Promote cooperative peer communication and turn-taking",
            "Encourage open-ended creative problem solving without rigid right-or-wrong answers"
        };
    }

    private static List<string> DetermineDevelopmentDomains(string type, ExperienceDna dna)
    {
        var list = new List<string> { "Cognitive Inquiry", "Fine Motor Coordination", "Social-Emotional Connection" };
        if (type == "Music & Movement") list.Add("Gross Motor & Rhythm");
        if (type == "Creative Art & Construction") list.Add("Expressive Arts & Design");
        if (type == "Outdoor Discovery") list.Add("Environmental Sciences & Wonder");
        return list;
    }

    private static List<string> GenerateDefaultMaterials(string type, string title)
    {
        return new List<string>
        {
            $"{title} themed discovery props",
            "Child-safe sensory sorting trays",
            "Natural textures (wood, smooth river stones, fabric)",
            "Magnifying glasses and observation mirrors",
            "Low work surface or circle carpet"
        };
    }

    private static List<string> GenerateMaterialSubstitutions(string type)
    {
        return new List<string>
        {
            "If water trays are unavailable: use soft blue crinkle paper or silk scarves.",
            "If wooden tools are unavailable: use sturdy silicone baking cups or plastic kitchen scoops.",
            "If sensory bins are limited: set up individual cardboard trays lined with baking parchment."
        };
    }

    private static List<string> GeneratePreparationSteps(string type, string title)
    {
        return new List<string>
        {
            $"Wipe and dry low activity table, setting up {title} exploration mats.",
            "Place materials in tactile discovery baskets at child eye-level.",
            "Pre-cut paper, tape, or clay so children can immediately dive into flow."
        };
    }

    private static List<string> GenerateEnvironmentSetup(string type)
    {
        return new List<string>
        {
            "Arrange a soft circular boundary so all children have equal reach to central supplies.",
            "Maintain soft, indirect natural lighting to encourage calm, grounded focus.",
            "Keep clean-up washcloths or damp sponges nearby on a dedicated side tray."
        };
    }

    private static List<string> GenerateSafetyRisks(int ageMin, string type, List<string>? materials)
    {
        var risks = new List<string>
        {
            "Inspect all natural and tactile items for sharp splinters or loose edges.",
            "Maintain active adult supervision to ensure supplies remain on the activity table."
        };

        if (ageMin < 3)
        {
            risks.Add("Choking hazard prevention: All items must be larger than standard 1.25-inch choke tube.");
        }

        return risks;
    }

    private static List<string> GenerateAccessibilityAdaptations(string type)
    {
        return new List<string>
        {
            "Provide chunky, easy-grip silicone tools for children developing fine-motor grip strength.",
            "Offer noise-dampening headphones or a quieter perimeter cushion for children sensitive to sensory sounds.",
            "Ensure table height accommodates standing, seated, or wheelchair exploration seamlessly."
        };
    }

    private static List<string> GenerateSensoryConsiderations(string type)
    {
        return new List<string>
        {
            "Warm versus cool material textures available side-by-side.",
            "Offer dry paper towels for children who prefer not to have sticky or wet hands.",
            "Maintain gentle speaking tones and calm visual cues throughout transitions."
        };
    }

    private static string CleanTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "Wonder Experience";
        var t = title.Trim();
        while (t.StartsWith("The Playful World of The Playful World of", StringComparison.OrdinalIgnoreCase))
        {
            t = "The Playful World of " + t.Substring("The Playful World of The Playful World of".Length).Trim();
        }
        return t;
    }

    private static string CleanDescription(string? desc, string cleanTitle)
    {
        if (string.IsNullOrWhiteSpace(desc))
            return $"Children embark on a delightful exploration of {cleanTitle.ToLowerInvariant()} with hands-on wonder and peer teamwork.";
        var d = desc.Trim();
        d = Regex.Replace(d, @"the playful world of the playful world of", "the playful world of", RegexOptions.IgnoreCase);
        return d;
    }
}

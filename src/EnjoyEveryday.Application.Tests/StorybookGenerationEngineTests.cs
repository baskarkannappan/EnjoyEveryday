using System;
using System.Collections.Generic;
using System.Linq;
using EnjoyEveryday.Application.Services.Ai;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Entities.Learning;
using NUnit.Framework;

namespace EnjoyEveryday.Application.Tests;

[TestFixture]
public class StorybookGenerationEngineTests
{
    private StorybookGenerationService _service = null!;

    [SetUp]
    public void Setup()
    {
        _service = new StorybookGenerationService();
    }

    [Test]
    public void GenerateStorybook_WithRainbowReefRescue_ProducesRichChaptersAndOceanTheme()
    {
        // Arrange
        var exp = new Experience
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Title = "The Rainbow Reef Rescue",
            Description = "A playful underwater adventure where children become ocean explorers finding colorful fish friends in a shimmering sensory sea.",
            Status = "Published"
        };

        var dna = new ExperienceDna
        {
            AgeMin = 3,
            AgeMax = 4,
            Challenge = "Can we help all the colorful fish find their way to their favorite coral homes?",
            Materials = new List<string> { "Blue sensory fabric", "Colorful fish toys", "Sorting buckets", "Child scoops" },
            ExecutionGuide = new ExecutionGuide
            {
                SuggestedDurationMinutes = 25,
                Phases = new List<ExecutionPhase>
                {
                    new() { Name = "Dive Briefing", DurationMinutes = 5, ChildrenDo = "Put on imaginary goggles", TeacherDoes = "Sets the scene with a story" },
                    new() { Name = "The Great Fish Rescue", DurationMinutes = 15, ChildrenDo = "Catch fish with nets", TeacherDoes = "Narrates the action" },
                    new() { Name = "Reef Celebration", DurationMinutes = 5, ChildrenDo = "Admire the sorted fish", TeacherDoes = "Leads a cheer" }
                }
            }
        };

        // Act
        var storybook = _service.GenerateStorybook(exp, dna);

        // Assert
        Assert.That(storybook, Is.Not.Null);
        Assert.That(storybook.Title, Is.EqualTo("The Rainbow Reef Rescue"));
        Assert.That(storybook.ExperienceType, Is.EqualTo("Narrative Adventure"));
        Assert.That(storybook.PaletteTheme, Is.EqualTo("ocean-coral"));
        Assert.That(storybook.CoverIllustrationKey, Is.EqualTo("reef"));
        Assert.That(storybook.Chapters.Count, Is.EqualTo(3));
        Assert.That(storybook.Validation.IsValid, Is.True);
        Assert.That(storybook.Chapters[0].ChildNarrative, Does.Contain("explorer"));
        Assert.That(storybook.Chapters[1].ChildrenAction, Does.Contain("Catch fish with nets"));
    }

    [Test]
    public void GenerateStorybook_WithCreativeArtStudio_DetectsArtThemeAndGeneratesGuidance()
    {
        // Arrange
        var exp = new Experience
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Title = "Clay Pinch Pots and Leaf Textures",
            Description = "Children press real leaves into air-dry clay to observe natural patterns.",
            Status = "Draft"
        };

        var dna = new ExperienceDna
        {
            AgeMin = 4,
            AgeMax = 5,
            Challenge = "Can you capture the leaf veins in your pottery?",
            Materials = new List<string> { "Air-dry clay", "Fresh leaves", "Wooden roller tools" }
        };

        // Act
        var storybook = _service.GenerateStorybook(exp, dna);

        // Assert
        Assert.That(storybook.ExperienceType, Is.EqualTo("Creative Art & Construction"));
        Assert.That(storybook.Chapters.Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(storybook.RequiredMaterials, Does.Contain("Air-dry clay"));
        Assert.That(storybook.DevelopmentDomains, Does.Contain("Expressive Arts & Design"));
        Assert.That(storybook.Status, Is.EqualTo("Draft"));
    }

    [Test]
    public void ValidateStorybook_WhenToddlerUnderThreeUsesSmallBeads_FlagsChokingHazard()
    {
        // Arrange
        var exp = new Experience
        {
            Id = Guid.NewGuid(),
            Title = "Toddler Counting Fun"
        };

        var dna = new ExperienceDna
        {
            AgeMin = 2,
            AgeMax = 3,
            Materials = new List<string> { "Small plastic beads", "Sorting bowls" }
        };

        // Act
        var storybook = _service.GenerateStorybook(exp, dna);

        // Assert
        Assert.That(storybook.Validation.SafetyFlags.Count, Is.GreaterThan(0));
        Assert.That(storybook.Validation.RequiresHumanSafetyReview, Is.True);
        Assert.That(storybook.Validation.SafetyFlags.Any(f => f.Contains("Choking hazard")), Is.True);
    }

    [Test]
    public void ValidateStorybook_WhenWaterSplashActivity_FlagsWaterSupervisionRule()
    {
        // Arrange
        var exp = new Experience
        {
            Id = Guid.NewGuid(),
            Title = "Water Table Waves",
            Description = "Splashing and pouring through water wheels"
        };

        var dna = new ExperienceDna
        {
            AgeMin = 3,
            AgeMax = 5,
            Materials = new List<string> { "Water table", "Measuring cups" }
        };

        // Act
        var storybook = _service.GenerateStorybook(exp, dna);

        // Assert
        Assert.That(storybook.Validation.SafetyFlags.Any(f => f.Contains("Water play safety")), Is.True);
    }
}

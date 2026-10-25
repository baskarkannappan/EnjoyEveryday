using System;
using System.Collections.Generic;

namespace EnjoyEveryday.Domain.Entities.Learning;

public enum StorybookDisplayMode
{
    ChildStory,     // Big illustrations, child-friendly narrative, simplified prompts for circle time
    TeacherPlaybook // Facilitation guide, spoken scripts, materials checklist, safety notes, observation lens
}

public class Storybook
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExperienceId { get; set; }
    public Guid TenantId { get; set; }
    public Guid? OrganizationId { get; set; }
    public int VersionNumber { get; set; } = 1;

    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ExperienceType { get; set; } = "Sensory Exploration"; // Narrative, Sensory, CreativeArt, Outdoor, STEM, MusicMovement, SocialEmotional

    // Target Audience & Structure
    public int AgeMin { get; set; } = 3;
    public int AgeMax { get; set; } = 5;
    public int GroupSizeMin { get; set; } = 4;
    public int GroupSizeMax { get; set; } = 12;
    public string Setting { get; set; } = "Indoor / Outdoor";
    public int TotalDurationMinutes { get; set; } = 25;

    // Cover & Visual Identity
    public string CoverIllustrationKey { get; set; } = "reef";
    public string CoverIllustrationPrompt { get; set; } = string.Empty;
    public string CoverSvgScene { get; set; } = string.Empty;
    public string PaletteTheme { get; set; } = "nature-sage"; // ocean-coral, nature-sage, solar-warm, clay-earth

    // Pedagogical Foundations
    public List<string> LearningIntentions { get; set; } = new();
    public List<string> DevelopmentDomains { get; set; } = new();

    // Preparation & Materials
    public List<string> RequiredMaterials { get; set; } = new();
    public List<string> OptionalMaterials { get; set; } = new();
    public List<string> MaterialSubstitutions { get; set; } = new();
    public List<string> PreparationSteps { get; set; } = new();
    public List<string> EnvironmentSetupSteps { get; set; } = new();

    // Ordered Chapters
    public List<StorybookChapter> Chapters { get; set; } = new();

    // Safety & Inclusion
    public List<string> SafetyRisksAndMitigations { get; set; } = new();
    public List<string> AccessibilityAdaptations { get; set; } = new();
    public List<string> SensoryConsiderations { get; set; } = new();

    // Reflection & Evidence
    public string ReflectionPrompt { get; set; } = string.Empty;
    public string CleanupRitual { get; set; } = string.Empty;
    public string LittleMomentObservationOpportunity { get; set; } = string.Empty;

    // Status & Metadata
    public string Status { get; set; } = "Draft"; // Draft, ReviewRequired, Published, Archived
    public bool IsSafetyApproved { get; set; } = false;
    public StorybookValidationResult Validation { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class StorybookChapter
{
    public int ChapterNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PhaseTag { get; set; } = "Step 1: Invitation";
    public int DurationMinutes { get; set; } = 5;

    // Dual-Mode Content
    public string ChildNarrative { get; set; } = string.Empty; // Big text for child mode
    public string TeacherDialogueScript { get; set; } = string.Empty; // "Say aloud..."
    public string TeacherFacilitationNotes { get; set; } = string.Empty; // Educator guidance
    public string ChildrenAction { get; set; } = string.Empty; // What children do
    public string ChildChoicePrompt { get; set; } = string.Empty; // Autonomy moment
    public string WonderQuestion { get; set; } = string.Empty; // Inquiry spark

    // Observation Lens
    public string WatchFor { get; set; } = string.Empty;
    public string TransitionToNext { get; set; } = string.Empty;

    // Visuals
    public string IllustrationKey { get; set; } = "gathering";
    public string IllustrationPrompt { get; set; } = string.Empty;
    public string ChapterSvgScene { get; set; } = string.Empty;
    public string StepIcon { get; set; } = "🌿";
}

public class StorybookValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> SafetyFlags { get; set; } = new();
    public bool RequiresHumanSafetyReview => SafetyFlags.Count > 0;
}

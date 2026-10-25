using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Daycare.Web.Components.Pages.Experiences;

namespace EnjoyEveryday.Daycare.Web.Components.Pages;

public static class StandaloneHtmlGenerator
{
    public static string CleanTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "Wonder Experience";
        var t = title.Trim();
        // Remove repeated prefixes if compounded
        while (t.StartsWith("The Playful World of The Playful World of", StringComparison.OrdinalIgnoreCase) ||
               t.StartsWith("The Playful World of The Playful World Of", StringComparison.OrdinalIgnoreCase))
        {
            t = "The Playful World of " + t.Substring("The Playful World of The Playful World of".Length).Trim();
        }
        return t;
    }

    public static string CleanDescription(string? desc, string cleanTitle)
    {
        if (string.IsNullOrWhiteSpace(desc))
            return "Children embark on an imaginative, playful quest turning tactile curiosity into creative storytelling, hands-on construction, and joyful collaboration.";
        var d = desc.Trim();
        d = Regex.Replace(d, @"the playful world of the playful world of", "the playful world of", RegexOptions.IgnoreCase);
        return d;
    }

    public static List<ExperienceStorybook.StoryChapter> BuildChapters(Experience? exp, ExperienceDna dna)
    {
        var chapters = new List<ExperienceStorybook.StoryChapter>();
        var cleanTitle = CleanTitle(exp?.Title);
        var mission = !string.IsNullOrWhiteSpace(exp?.Description) ? CleanDescription(exp.Description, cleanTitle) : "Discovering new wonders through playful hands and curious eyes.";
        var challenge = !string.IsNullOrWhiteSpace(dna.Challenge) ? dna.Challenge : "Explore materials, test ideas, and discover something surprising together.";
        // Clean double prefixes in challenge
        challenge = Regex.Replace(challenge, @"our the playful world of", "our playful world of", RegexOptions.IgnoreCase);
        var create = !string.IsNullOrWhiteSpace(dna.Create) ? dna.Create : "Children engage hands-on with organic shapes, sensory textures, and spontaneous creation.";
        var magic = !string.IsNullOrWhiteSpace(dna.MagicMoment) ? dna.MagicMoment : "A sudden flash of excitement when materials combine in an unexpected, beautiful way.";
        var accomplishment = !string.IsNullOrWhiteSpace(dna.Accomplishment) ? dna.Accomplishment : "Children beam with pride as they showcase their creation to friends.";
        var reflection = !string.IsNullOrWhiteSpace(dna.Reflection) ? dna.Reflection : "Gathering back in a gentle circle to share what we learned and felt today.";

        if (dna.ExecutionGuide?.Phases != null && dna.ExecutionGuide.Phases.Count >= 3)
        {
            var phases = dna.ExecutionGuide.Phases;
            int count = phases.Count;

            for (int i = 0; i < count; i++)
            {
                var p = phases[i];
                string sceneKey;
                string icon;
                string narrative;
                string teacherScript;
                string wonderPrompt;

                if (i == 0)
                {
                    sceneKey = "gathering";
                    icon = "🌿";
                    narrative = $"Welcome the children warmly onto the circle rug. Lay out tactile materials within easy reach. Invite curious little hands to explore textures and share observations before construction begins.";
                    teacherScript = !string.IsNullOrWhiteSpace(dna.ExecutionGuide?.TeacherIntroduction)
                        ? dna.ExecutionGuide.TeacherIntroduction
                        : $"\"Welcome, explorers! Look at what's waiting in our basket today. What do your fingers notice when you touch these materials?\"";
                    wonderPrompt = !string.IsNullOrWhiteSpace(dna.ExecutionGuide?.Ask) ? dna.ExecutionGuide.Ask : "What does this feel like in your hands? Have you ever seen this before?";
                }
                else if (i == count - 1)
                {
                    sceneKey = "reflection";
                    icon = "🌟";
                    narrative = $"Gather back together into a cozy reflection circle. Celebrate each child's creativity and effort with warmth. {accomplishment}. The memories made today will stay warmly in our hearts.";
                    teacherScript = $"\"Let's celebrate our wonderful creations! Who would like to show what surprised them most while building today?\"";
                    wonderPrompt = !string.IsNullOrWhiteSpace(reflection) ? reflection : "What was your favorite part of today's adventure? Who would you like to share this story with?";
                }
                else if (count == 3) // middle phase for a 3-step experience
                {
                    sceneKey = "explore";
                    icon = "🎨";
                    narrative = $"The room fills with the happy hum of active creation. {create}. Little hands mold, balance, test, and arrange materials into brand new worlds.";
                    teacherScript = $"\"Look what happens when these two materials meet! There are no wrong answers here—what would you like to invent next?\"";
                    wonderPrompt = challenge;
                }
                else if (i == 1 && count >= 4)
                {
                    sceneKey = "challenge";
                    icon = "🔍";
                    narrative = $"Pose the gentle wonder challenge: {challenge}. There are no right or wrong answers—only open invitations to experiment, hypothesize, and create.";
                    teacherScript = $"\"I wonder what secret worlds we can build if we work together? What happens if we try this?\"";
                    wonderPrompt = challenge;
                }
                else if (i == 2 && count >= 4)
                {
                    sceneKey = "explore";
                    icon = "🎨";
                    narrative = $"Deep creative flow takes over. Children sort, build, balance, and problem-solve together with focused, joyful energy.";
                    teacherScript = $"\"You found a whole new way to connect those pieces! Tell me about what's happening in your creation.\"";
                    wonderPrompt = "Look what happens when these textures combine! What story is your creation telling?";
                }
                else
                {
                    sceneKey = "magic";
                    icon = "✨";
                    narrative = $"A breakthrough moment of discovery! {magic}. A shared ripple of delight touches every heart in the room.";
                    teacherScript = $"\"You discovered that all on your own! How did that happen? Can we try it together?\"";
                    wonderPrompt = "Did you see that? What made it happen? Can we do it again?";
                }

                chapters.Add(new ExperienceStorybook.StoryChapter
                {
                    Number = i + 1,
                    Title = string.IsNullOrWhiteSpace(p.Name) ? $"Step {i + 1}" : p.Name,
                    PhaseTag = i == 0 ? "Step 1: Setup & Invitation" : i == count - 1 ? $"Step {i + 1}: Reflection & Sharing" : $"Step {i + 1}: Hands-On Action",
                    Duration = p.DurationMinutes > 0 ? $"{p.DurationMinutes} min" : "5 min",
                    Narrative = narrative,
                    ChildrenAction = string.IsNullOrWhiteSpace(p.ChildrenDo) ? "Children explore with open hands and focused curiosity." : p.ChildrenDo,
                    TeacherSpark = string.IsNullOrWhiteSpace(p.TeacherDoes) ? "Observe and offer open-ended support when invited." : p.TeacherDoes,
                    WonderPrompt = wonderPrompt,
                    SvgSceneKey = sceneKey,
                    TeacherScript = teacherScript,
                    StepIcon = icon
                });
            }
        }
        else
        {
            chapters.Add(new ExperienceStorybook.StoryChapter
            {
                Number = 1,
                Title = "The Gathering & Setup Invitation",
                PhaseTag = "Step 1: Setup & Invitation",
                Duration = "5 min",
                Narrative = "Prepare the low circle table or exploration rug. Place tactile elements within reach. Children gather closely to inspect the materials with open curiosity.",
                ChildrenAction = "Children gather around the sensory rug, feeling the smooth textures and whispering guesses about today's mystery.",
                TeacherSpark = !string.IsNullOrWhiteSpace(dna.ExecutionGuide?.TeacherIntroduction) ? dna.ExecutionGuide.TeacherIntroduction : "Present the mystery basket with calm excitement. Ask: 'What curious secrets are hiding inside?'",
                WonderPrompt = !string.IsNullOrWhiteSpace(dna.ExecutionGuide?.Ask) ? dna.ExecutionGuide.Ask : "What does this feel like in your hands? Have you ever seen this before?",
                SvgSceneKey = "gathering",
                TeacherScript = "\"Come gather close, little explorers! What do your fingers notice when you touch these materials?\"",
                StepIcon = "🌿"
            });

            chapters.Add(new ExperienceStorybook.StoryChapter
            {
                Number = 2,
                Title = "The Wonder Challenge & Inquiry",
                PhaseTag = "Step 2: The Inquiry",
                Duration = "5 min",
                Narrative = $"Pose the gentle challenge: {challenge}. There are no right or wrong answers—only invitations to experiment, hypothesize, and build.",
                ChildrenAction = "Children examine their tools, turn them over in their fingers, and whisper ideas to their neighbor with joyful energy.",
                TeacherSpark = "Frame the challenge as an adventure. Encourage each child that every idea is valuable and worth trying.",
                WonderPrompt = "How do you think we can make this work? What if we try it together?",
                SvgSceneKey = "challenge",
                TeacherScript = "\"I wonder what will happen if we test these ideas together? What does your curiosity tell you?\"",
                StepIcon = "🔍"
            });

            chapters.Add(new ExperienceStorybook.StoryChapter
            {
                Number = 3,
                Title = "Active Hands-On Play & Construction",
                PhaseTag = "Step 3: Deep Flow",
                Duration = "10 min",
                Narrative = $"The room fills with the happy hum of active creation. {create}. Little hands mold, balance, test, and arrange materials into brand new worlds.",
                ChildrenAction = "Deeply engaged in flow state: sorting colors, building structures, testing balance, and sharing supplies kindly.",
                TeacherSpark = !string.IsNullOrWhiteSpace(dna.ExecutionGuide?.Watch) ? dna.ExecutionGuide.Watch : "Observe problem-solving moments. Notice who collaborates and who finds a peaceful solo focus.",
                WonderPrompt = "Look what happens when these two come together! What story is your creation telling?",
                SvgSceneKey = "explore",
                TeacherScript = "\"Look at that balance! Tell me what you're imagining as your hands build this.\"",
                StepIcon = "🎨"
            });

            chapters.Add(new ExperienceStorybook.StoryChapter
            {
                Number = 4,
                Title = "The Magic Moment & Discovery",
                PhaseTag = "Step 4: The Discovery",
                Duration = "5 min",
                Narrative = $"Encourage children to share their breakthrough: {magic}. A shared ripple of delight touches every heart in the room.",
                ChildrenAction = "Eyes widen with excitement, pointing out their breakthrough to friends with wide, luminous smiles.",
                TeacherSpark = "Celebrate the discovery immediately! Say: 'You discovered that all on your own! How did that happen?'",
                WonderPrompt = "Did you see that? What made it happen? Can we do it again?",
                SvgSceneKey = "magic",
                TeacherScript = "\"You discovered that all on your own! Look at that wonderful breakthrough!\"",
                StepIcon = "✨"
            });

            chapters.Add(new ExperienceStorybook.StoryChapter
            {
                Number = 5,
                Title = "Pride, Reflection & Story Sharing",
                PhaseTag = "Step 5: Reflection",
                Duration = "5 min",
                Narrative = $"{accomplishment}. Each child holds up their triumph with gentle pride. {reflection}. The memories of today will stay warmly in our hearts.",
                ChildrenAction = "Children proudly present their work, explain their creative decisions, and help tidy up their sanctuary.",
                TeacherSpark = "Capture the moment with a warm photo or learning note. Guide a closing gratitude breath for our journey together.",
                WonderPrompt = "What was your favorite part of today's adventure? Who would you like to share this story with?",
                SvgSceneKey = "reflection",
                TeacherScript = "\"Let's celebrate each of our creations. What was your favorite surprise today?\"",
                StepIcon = "🌟"
            });
        }

        return chapters;
    }

    public static string GenerateStorybookHtml(Experience exp, ExperienceDna dna)
    {
        var cleanTitle = CleanTitle(exp.Title);
        var chapters = BuildChapters(exp, dna);
        var ageRange = dna.AgeMin > 0 && dna.AgeMax > 0 ? $"{dna.AgeMin}-{dna.AgeMax} yrs" : "3-5 yrs";
        var setting = !string.IsNullOrWhiteSpace(dna.Environment) ? dna.Environment : "Indoor / Outdoor";
        var duration = dna.ExecutionGuide?.SuggestedDurationMinutes > 0 ? dna.ExecutionGuide.SuggestedDurationMinutes : 25;
        var materials = dna.Materials != null && dna.Materials.Count > 0 ? dna.Materials : new List<string>
        {
            "Cozy exploration mat or round circle rug",
            "Natural sensory items (smooth stones, pinecones, twigs)",
            "Magnifying glasses or wonder mirrors",
            "Child-safe craft clay or modeling sand",
            "Observation notebook or memory clipboard"
        };

        var versionText = exp.UpdatedAt > DateTimeOffset.MinValue
            ? $"Updated {exp.UpdatedAt:MMM dd, yyyy}" 
            : $"Created {exp.CreatedAt:MMM dd, yyyy}";

        return GenerateHtml(
            cleanTitle,
            CleanDescription(exp.Description, cleanTitle),
            ageRange,
            setting,
            duration,
            materials,
            dna.ExecutionGuide?.Watch ?? "",
            dna.ExecutionGuide?.Ask ?? "",
            dna.ExecutionGuide?.Try ?? "",
            chapters,
            versionText
        );
    }

    public static string GenerateHtml(
        string title,
        string description,
        string ageRange,
        string setting,
        int durationMinutes,
        List<string> materials,
        string watchFor,
        string askQuestions,
        string tryThis,
        List<ExperienceStorybook.StoryChapter> chapters,
        string versionInfo = "")
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"  <title>{HttpUtility.HtmlEncode(title)} — Activity Storyboard & Playbook</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine(@"
    :root {
      --bg: #f5f5ef;
      --surface: #ffffff;
      --surface-soft: #f8f9f5;
      --border: #e4e7df;
      --text: #26332d;
      --muted: #6c7870;
      --accent: #557c6c;
      --accent-light: #e6eee9;
      --accent-dark: #3e6253;
      --warm: #f4eee1;
      --shadow: 0 8px 30px rgba(40, 55, 47, .06);
      --radius: 18px;
    }

    * { box-sizing: border-box; margin: 0; padding: 0; }
    body {
      font-family: ""Figtree"", Inter, -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif;
      background: var(--bg);
      color: var(--text);
      line-height: 1.5;
      padding: 24px 20px 60px;
      -webkit-font-smoothing: antialiased;
    }

    .container {
      max-width: 1400px;
      margin: 0 auto;
    }

    /* Top Hero Header */
    .hero {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 20px;
      margin-bottom: 24px;
      flex-wrap: wrap;
    }

    .hero-eyebrow {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 11px;
      font-weight: 800;
      letter-spacing: 1.2px;
      color: var(--accent-dark);
      text-transform: uppercase;
      margin-bottom: 8px;
      flex-wrap: wrap;
    }

    .badge-pill {
      background: var(--accent-light);
      color: var(--accent-dark);
      padding: 3px 10px;
      border-radius: 999px;
      font-size: 11px;
      font-weight: 750;
      letter-spacing: normal;
    }

    .setting-pill {
      background: var(--warm);
      color: #634f35;
    }

    .version-pill {
      background: #eef2f6;
      color: #3b4b5e;
      border: 1px solid #d4dde5;
    }

    .hero-title {
      font-size: 30px;
      font-weight: 800;
      letter-spacing: -0.5px;
      color: var(--text);
      margin-bottom: 6px;
    }

    .hero-desc {
      color: var(--muted);
      font-size: 14px;
      max-width: 820px;
      line-height: 1.6;
    }

    .hero-actions {
      display: flex;
      gap: 10px;
      align-items: center;
    }

    .btn {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      padding: 10px 18px;
      border-radius: 11px;
      font-weight: 700;
      font-size: 13px;
      cursor: pointer;
      border: 1px solid transparent;
      transition: all 0.2s ease;
      text-decoration: none;
    }

    .btn.primary {
      background: var(--accent);
      color: white;
    }

    .btn.primary:hover {
      background: var(--accent-dark);
    }

    .btn.secondary {
      background: white;
      border-color: var(--border);
      color: var(--text);
    }

    .btn.secondary:hover {
      background: var(--surface-soft);
    }

    /* Mode Switcher Bar */
    .view-toggle-bar {
      display: flex;
      align-items: center;
      gap: 8px;
      background: var(--surface);
      padding: 6px 10px;
      border-radius: 12px;
      border: 1px solid var(--border);
      margin-bottom: 22px;
      width: fit-content;
    }

    .view-toggle-btn {
      padding: 6px 14px;
      border-radius: 8px;
      border: none;
      background: transparent;
      font-size: 12.5px;
      font-weight: 700;
      color: var(--muted);
      cursor: pointer;
      display: flex;
      align-items: center;
      gap: 6px;
      transition: all 0.2s;
    }

    .view-toggle-btn.active {
      background: var(--accent-light);
      color: var(--accent-dark);
    }

    /* Split Layout */
    .split-layout {
      display: grid;
      grid-template-columns: 360px 1fr;
      gap: 24px;
      align-items: flex-start;
    }

    @media (max-width: 1024px) {
      .split-layout { grid-template-columns: 1fr; }
    }

    /* Sidebar Cards */
    .sidebar {
      display: flex;
      flex-direction: column;
      gap: 18px;
    }

    .card {
      background: var(--surface);
      border: 1px solid var(--border);
      border-radius: var(--radius);
      box-shadow: var(--shadow);
      padding: 18px 20px;
    }

    .card-title-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 12px;
    }

    .card-title {
      font-size: 13.5px;
      font-weight: 750;
      color: var(--text);
      display: flex;
      align-items: center;
      gap: 8px;
    }

    /* Step Nav Stepper */
    .stepper {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .step-btn {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 10px 12px;
      border-radius: 12px;
      border: 1px solid transparent;
      background: var(--surface-soft);
      cursor: pointer;
      text-align: left;
      width: 100%;
      transition: all 0.2s;
    }

    .step-btn:hover {
      border-color: var(--border);
      background: #f0f4f1;
    }

    .step-btn.active {
      background: var(--accent-light);
      border-color: var(--accent);
    }

    .step-icon-badge {
      width: 32px;
      height: 32px;
      border-radius: 50%;
      background: white;
      font-size: 15px;
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      border: 1px solid var(--border);
      box-shadow: 0 2px 6px rgba(0,0,0,0.03);
    }

    .step-btn.active .step-icon-badge {
      border-color: var(--accent);
      background: #ffffff;
      transform: scale(1.05);
    }

    .step-text h4 {
      font-size: 13px;
      font-weight: 750;
      color: var(--text);
      margin-bottom: 2px;
    }

    .step-text p {
      font-size: 11px;
      color: var(--muted);
    }

    /* Materials Checklist */
    .mat-item {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 7px 0;
      font-size: 13px;
      color: #3b4640;
      border-bottom: 1px solid #f2f5f1;
      cursor: pointer;
    }

    .mat-item:last-child { border-bottom: none; }
    .mat-item input { accent-color: var(--accent); width: 16px; height: 16px; }

    /* Teacher observation cues */
    .cue-item {
      margin-bottom: 12px;
    }

    .cue-item:last-child { margin-bottom: 0; }

    .cue-pill {
      font-size: 10px;
      font-weight: 800;
      letter-spacing: 0.5px;
      padding: 2px 7px;
      border-radius: 4px;
      display: inline-block;
      margin-bottom: 4px;
    }

    .cue-watch { background: #eaf3ed; color: #2d5a3f; }
    .cue-ask { background: #fef7ea; color: #855e1b; }
    .cue-try { background: #fbeee8; color: #8f402c; }

    .cue-item p {
      font-size: 12px;
      color: #435149;
      line-height: 1.45;
    }

    /* Main Storyboard Stage */
    .stage {
      background: var(--surface);
      border: 1px solid var(--border);
      border-radius: var(--radius);
      box-shadow: var(--shadow);
      padding: 22px 24px;
      transition: all 0.3s ease;
    }

    .stage-top {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 18px;
      padding-bottom: 12px;
      border-bottom: 1px solid var(--border);
    }

    .scene-meta-row {
      display: flex;
      align-items: center;
      gap: 10px;
    }

    .scene-tag {
      font-size: 11px;
      font-weight: 800;
      color: var(--accent-dark);
      background: var(--accent-light);
      padding: 4px 12px;
      border-radius: 6px;
      text-transform: uppercase;
      letter-spacing: 0.4px;
    }

    .dots-nav {
      display: flex;
      gap: 6px;
      align-items: center;
    }

    .dot {
      width: 9px;
      height: 9px;
      border-radius: 50%;
      background: #d8ded9;
      cursor: pointer;
      transition: all 0.2s;
    }

    .dot.active {
      background: var(--accent);
      transform: scale(1.35);
    }

    /* Storyboard Grid: Diagram + Story Instructions */
    .diy-step-grid {
      display: grid;
      grid-template-columns: 1.1fr 1fr;
      gap: 24px;
      margin-bottom: 22px;
      align-items: stretch;
    }

    @media (max-width: 950px) {
      .diy-step-grid { grid-template-columns: 1fr; }
    }

    .diy-diagram-box {
      background: #fafaf8;
      border: 1px solid var(--border);
      border-radius: 14px;
      overflow: hidden;
      display: flex;
      flex-direction: column;
      box-shadow: inset 0 1px 3px rgba(0,0,0,0.02);
    }

    .diagram-header {
      padding: 10px 14px;
      background: #f1f4f0;
      border-bottom: 1px solid var(--border);
      display: flex;
      align-items: center;
      justify-content: space-between;
      font-size: 11px;
      font-weight: 750;
      color: var(--accent-dark);
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .diagram-canvas {
      width: 100%;
      height: 340px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: #fdfbf7;
      position: relative;
    }

    .diagram-canvas svg {
      width: 100%;
      height: 100%;
      object-fit: cover;
      display: block;
    }

    .diy-info-box {
      display: flex;
      flex-direction: column;
      justify-content: space-between;
      gap: 14px;
    }

    .scene-heading {
      font-size: 23px;
      font-weight: 800;
      letter-spacing: -0.4px;
      color: var(--text);
      margin-bottom: 10px;
    }

    /* Narrative Flow Callout */
    .instruction-callout {
      background: var(--surface-soft);
      border-left: 4px solid var(--accent);
      padding: 12px 16px;
      border-radius: 0 10px 10px 0;
      margin-bottom: 12px;
    }

    .instruction-callout h5 {
      font-size: 10.5px;
      font-weight: 800;
      text-transform: uppercase;
      color: var(--accent-dark);
      margin-bottom: 4px;
      letter-spacing: 0.5px;
    }

    .instruction-callout p {
      font-size: 13px;
      color: #313f38;
      line-height: 1.5;
    }

    /* Teacher Spoken Script Box (What to say aloud) */
    .teacher-script-callout {
      background: #fbf7ef;
      border: 1px solid #ebdcc5;
      padding: 12px 14px;
      border-radius: 10px;
      margin-bottom: 12px;
    }

    .script-header {
      display: flex;
      align-items: center;
      gap: 6px;
      font-size: 11px;
      font-weight: 800;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      color: #8c5a1e;
      margin-bottom: 4px;
    }

    .script-text {
      font-size: 13px;
      font-style: italic;
      color: #4f3b20;
      line-height: 1.5;
    }

    /* Actions Grid */
    .actions-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 12px;
      margin-bottom: 12px;
    }

    .action-card {
      padding: 12px 14px;
      border-radius: 10px;
      background: #fafaf8;
      border: 1px solid var(--border);
    }

    .action-header {
      display: flex;
      align-items: center;
      gap: 6px;
      font-weight: 750;
      font-size: 11.5px;
      margin-bottom: 4px;
    }

    .child-action .action-header { color: #2e6047; }
    .teacher-action .action-header { color: #855e39; }

    .action-card p {
      font-size: 12px;
      color: #4b5850;
      line-height: 1.45;
    }

    .wonder-box {
      background: #fffbeb;
      border: 1px solid #fef3c7;
      padding: 11px 14px;
      border-radius: 10px;
      display: flex;
      align-items: center;
      gap: 10px;
    }

    .wonder-box strong {
      color: #92400e;
      font-size: 12px;
    }

    .wonder-box span {
      font-size: 12px;
      color: #78350f;
    }

    /* Bottom Navigation */
    .stage-bottom {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding-top: 16px;
      border-top: 1px solid var(--border);
    }

    /* Storyboard Presentation Mode Styles */
    .storyboard-mode .diy-step-grid {
      grid-template-columns: 1fr;
    }

    .storyboard-mode .diagram-canvas {
      height: 440px;
    }

    /* Print View */
    @media print {
      body { background: white; padding: 0; }
      .hero-actions, .stage-bottom, .dots-nav, .view-toggle-bar { display: none !important; }
      .split-layout { display: block; }
      .stage { page-break-after: always; box-shadow: none; border: 1px solid #ccc; margin-bottom: 30px; }
      .diagram-canvas { height: 260px; }
    }
  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine("  <div class=\"container\">");

        // Hero
        sb.AppendLine("    <header class=\"hero\">");
        sb.AppendLine("      <div>");
        sb.AppendLine("        <div class=\"hero-eyebrow\">");
        sb.AppendLine("          <span>📐</span>");
        sb.AppendLine("          <span>ACTIVITY STORYBOARD &amp; PLAYBOOK MANUAL</span>");
        sb.AppendLine($"          <span class=\"badge-pill\">Ages {HttpUtility.HtmlEncode(ageRange)}</span>");
        sb.AppendLine($"          <span class=\"badge-pill setting-pill\">{HttpUtility.HtmlEncode(setting)}</span>");
        if (!string.IsNullOrWhiteSpace(versionInfo))
        {
            sb.AppendLine($"          <span class=\"badge-pill version-pill\">{HttpUtility.HtmlEncode(versionInfo)}</span>");
        }
        sb.AppendLine("        </div>");
        sb.AppendLine($"        <h1 class=\"hero-title\">{HttpUtility.HtmlEncode(title)}</h1>");
        sb.AppendLine($"        <p class=\"hero-desc\">{HttpUtility.HtmlEncode(description)}</p>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div class=\"hero-actions\">");
        sb.AppendLine("        <button class=\"btn secondary\" onclick=\"window.print()\">🖨️ Print Storyboard</button>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </header>");

        // View Mode Switcher
        sb.AppendLine("    <div class=\"view-toggle-bar\">");
        sb.AppendLine("      <button class=\"view-toggle-btn active\" id=\"btnModeSplit\" onclick=\"setMode('split')\">📋 Activity Manual</button>");
        sb.AppendLine("      <button class=\"view-toggle-btn\" id=\"btnModeSlide\" onclick=\"setMode('slide')\">🎨 Visual Storyboard</button>");
        sb.AppendLine("    </div>");

        // Split Layout
        sb.AppendLine("    <div class=\"split-layout\" id=\"layoutContainer\">");

        // Sidebar
        sb.AppendLine("      <aside class=\"sidebar\">");

        // Step Navigation Stepper
        sb.AppendLine("        <div class=\"card\">");
        sb.AppendLine("          <div class=\"card-title-row\">");
        sb.AppendLine("            <div class=\"card-title\"><span>📋</span> Storyboard Steps</div>");
        sb.AppendLine($"            <span style=\"font-size: 12px; font-weight: 700; color: var(--accent-dark);\">~{durationMinutes} min</span>");
        sb.AppendLine("          </div>");
        sb.AppendLine("          <div class=\"stepper\" id=\"stepperContainer\">");

        for (int i = 0; i < chapters.Count; i++)
        {
            var ch = chapters[i];
            var activeClass = i == 0 ? " active" : "";
            var icon = !string.IsNullOrWhiteSpace(ch.StepIcon) ? ch.StepIcon : "🌿";
            sb.AppendLine($"            <button class=\"step-btn{activeClass}\" onclick=\"goToChapter({i})\" id=\"step-{i}\">");
            sb.AppendLine($"              <div class=\"step-icon-badge\">{icon}</div>");
            sb.AppendLine("              <div class=\"step-text\">");
            sb.AppendLine($"                <h4>{HttpUtility.HtmlEncode(ch.Title)}</h4>");
            sb.AppendLine($"                <p>{HttpUtility.HtmlEncode(ch.PhaseTag)} • {HttpUtility.HtmlEncode(ch.Duration)}</p>");
            sb.AppendLine("              </div>");
            sb.AppendLine("            </button>");
        }

        sb.AppendLine("          </div>");
        sb.AppendLine("        </div>");

        // Materials Checklist
        sb.AppendLine("        <div class=\"card\">");
        sb.AppendLine("          <div class=\"card-title-row\">");
        sb.AppendLine("            <div class=\"card-title\"><span>🌿</span> Materials & Setup Kit</div>");
        sb.AppendLine($"            <span style=\"font-size: 11px; color: var(--muted);\">{materials.Count} items</span>");
        sb.AppendLine("          </div>");
        sb.AppendLine("          <div>");
        foreach (var mat in materials)
        {
            sb.AppendLine("            <label class=\"mat-item\">");
            sb.AppendLine("              <input type=\"checkbox\" />");
            sb.AppendLine($"              <span>{HttpUtility.HtmlEncode(mat)}</span>");
            sb.AppendLine("            </label>");
        }
        sb.AppendLine("          </div>");
        sb.AppendLine("        </div>");

        // Teacher's Observation Lens
        sb.AppendLine("        <div class=\"card\">");
        sb.AppendLine("          <div class=\"card-title-row\">");
        sb.AppendLine("            <div class=\"card-title\"><span>👀</span> Teacher's Observation Lens</div>");
        sb.AppendLine("          </div>");
        if (!string.IsNullOrWhiteSpace(watchFor))
        {
            sb.AppendLine("          <div class=\"cue-item\">");
            sb.AppendLine("            <span class=\"cue-pill cue-watch\">WATCH FOR</span>");
            sb.AppendLine($"            <p>{HttpUtility.HtmlEncode(watchFor)}</p>");
            sb.AppendLine("          </div>");
        }
        if (!string.IsNullOrWhiteSpace(askQuestions))
        {
            sb.AppendLine("          <div class=\"cue-item\">");
            sb.AppendLine("            <span class=\"cue-pill cue-ask\">WONDERING QUESTIONS</span>");
            sb.AppendLine($"            <p>{HttpUtility.HtmlEncode(askQuestions)}</p>");
            sb.AppendLine("          </div>");
        }
        if (!string.IsNullOrWhiteSpace(tryThis))
        {
            sb.AppendLine("          <div class=\"cue-item\">");
            sb.AppendLine("            <span class=\"cue-pill cue-try\">TRY THIS</span>");
            sb.AppendLine($"            <p>{HttpUtility.HtmlEncode(tryThis)}</p>");
            sb.AppendLine("          </div>");
        }
        sb.AppendLine("        </div>");

        sb.AppendLine("      </aside>");

        // Stage Main (Visual Storyboard Step with Diagram + Pedagogy)
        sb.AppendLine("      <main>");
        sb.AppendLine("        <div class=\"stage\" id=\"stageElement\">");
        sb.AppendLine("          <div class=\"stage-top\">");
        sb.AppendLine("            <div class=\"scene-meta-row\">");
        sb.AppendLine("              <span class=\"scene-tag\" id=\"chapterPill\">Step 1 of " + chapters.Count + "</span>");
        sb.AppendLine("              <span style=\"font-size: 12px; color: var(--muted);\" id=\"chapterDuration\">⏱️ 5 min</span>");
        sb.AppendLine("            </div>");
        sb.AppendLine("            <div class=\"dots-nav\" id=\"dotsContainer\">");
        for (int i = 0; i < chapters.Count; i++)
        {
            var activeClass = i == 0 ? " active" : "";
            sb.AppendLine($"              <span class=\"dot{activeClass}\" onclick=\"goToChapter({i})\" id=\"dot-{i}\"></span>");
        }
        sb.AppendLine("            </div>");
        sb.AppendLine("          </div>");

        // Side-by-side DIY Step Grid
        sb.AppendLine("          <div class=\"diy-step-grid\">");
        
        // Diagram on Left
        sb.AppendLine("            <div class=\"diy-diagram-box\">");
        sb.AppendLine("              <div class=\"diagram-header\">");
        sb.AppendLine("                <span>🎨 Visual Blueprint Scene</span>");
        sb.AppendLine("                <span id=\"diagramStepNum\">STEP 1</span>");
        sb.AppendLine("              </div>");
        sb.AppendLine("              <div class=\"diagram-canvas\" id=\"svgBackdrop\">");
        if (chapters.Count > 0)
        {
            sb.AppendLine(GetSvgForKey(chapters[0].SvgSceneKey));
        }
        sb.AppendLine("              </div>");
        sb.AppendLine("            </div>");

        // Instructions on Right
        sb.AppendLine("            <div class=\"diy-info-box\">");
        sb.AppendLine("              <div>");
        sb.AppendLine("                <h2 class=\"scene-heading\" id=\"chapterTitle\">" + HttpUtility.HtmlEncode(chapters[0].Title) + "</h2>");
        
        // Narrative Callout
        sb.AppendLine("                <div class=\"instruction-callout\">");
        sb.AppendLine("                  <h5>Pedagogical Story Flow</h5>");
        sb.AppendLine("                  <p id=\"chapterNarrative\">" + HttpUtility.HtmlEncode(chapters[0].Narrative) + "</p>");
        sb.AppendLine("                </div>");

        // Teacher Script Callout
        sb.AppendLine("                <div class=\"teacher-script-callout\">");
        sb.AppendLine("                  <div class=\"script-header\">");
        sb.AppendLine("                    <span>🗣️</span>");
        sb.AppendLine("                    <strong>Teacher's Spoken Script (Say Aloud)</strong>");
        sb.AppendLine("                  </div>");
        sb.AppendLine("                  <p class=\"script-text\" id=\"teacherScript\">" + HttpUtility.HtmlEncode(chapters[0].TeacherScript) + "</p>");
        sb.AppendLine("                </div>");

        // Perspectives Grid
        sb.AppendLine("                <div class=\"actions-grid\">");
        sb.AppendLine("                  <div class=\"action-card child-action\">");
        sb.AppendLine("                    <div class=\"action-header\">👧👦 Children Action</div>");
        sb.AppendLine("                    <p id=\"childAction\">" + HttpUtility.HtmlEncode(chapters[0].ChildrenAction) + "</p>");
        sb.AppendLine("                  </div>");
        sb.AppendLine("                  <div class=\"action-card teacher-action\">");
        sb.AppendLine("                    <div class=\"action-header\">🌱 Teacher Guidance</div>");
        sb.AppendLine("                    <p id=\"teacherSpark\">" + HttpUtility.HtmlEncode(chapters[0].TeacherSpark) + "</p>");
        sb.AppendLine("                  </div>");
        sb.AppendLine("                </div>");
        sb.AppendLine("              </div>");

        sb.AppendLine("              <div class=\"wonder-box\">");
        sb.AppendLine("                <span style=\"font-size: 18px;\">💡</span>");
        sb.AppendLine("                <div>");
        sb.AppendLine("                  <strong>Wonder Prompt: </strong>");
        sb.AppendLine("                  <span id=\"wonderPrompt\">" + HttpUtility.HtmlEncode(chapters[0].WonderPrompt) + "</span>");
        sb.AppendLine("                </div>");
        sb.AppendLine("              </div>");

        sb.AppendLine("            </div>"); // end diy-info-box
        sb.AppendLine("          </div>"); // end diy-step-grid

        // Stage Bottom Nav
        sb.AppendLine("          <div class=\"stage-bottom\">");
        sb.AppendLine("            <button class=\"btn secondary\" id=\"prevBtn\" onclick=\"prevChapter()\" disabled>← Previous Step</button>");
        sb.AppendLine("            <button class=\"btn primary\" id=\"nextBtn\" onclick=\"nextChapter()\">Next Step →</button>");
        sb.AppendLine("          </div>");

        sb.AppendLine("        </div>");
        sb.AppendLine("      </main>");

        sb.AppendLine("    </div>"); // end split-layout
        sb.AppendLine("  </div>"); // end container

        // Embed Chapters JSON & Interactive JS
        sb.AppendLine("  <script>");
        sb.AppendLine("    const chapters = [");
        for (int i = 0; i < chapters.Count; i++)
        {
            var ch = chapters[i];
            sb.AppendLine("      {");
            sb.AppendLine($"        index: {i},");
            sb.AppendLine($"        title: {System.Text.Json.JsonSerializer.Serialize(ch.Title)},");
            sb.AppendLine($"        phaseTag: {System.Text.Json.JsonSerializer.Serialize(ch.PhaseTag)},");
            sb.AppendLine($"        duration: {System.Text.Json.JsonSerializer.Serialize(ch.Duration)},");
            sb.AppendLine($"        narrative: {System.Text.Json.JsonSerializer.Serialize(ch.Narrative)},");
            sb.AppendLine($"        teacherScript: {System.Text.Json.JsonSerializer.Serialize(ch.TeacherScript)},");
            sb.AppendLine($"        childAction: {System.Text.Json.JsonSerializer.Serialize(ch.ChildrenAction)},");
            sb.AppendLine($"        teacherSpark: {System.Text.Json.JsonSerializer.Serialize(ch.TeacherSpark)},");
            sb.AppendLine($"        wonderPrompt: {System.Text.Json.JsonSerializer.Serialize(ch.WonderPrompt)},");
            sb.AppendLine($"        svgKey: {System.Text.Json.JsonSerializer.Serialize(ch.SvgSceneKey)}");
            sb.AppendLine("      }" + (i < chapters.Count - 1 ? "," : ""));
        }
        sb.AppendLine("    ];");

        sb.AppendLine("    const svgMap = {");
        sb.AppendLine($"      'gathering': {System.Text.Json.JsonSerializer.Serialize(GetSvgForKey("gathering"))},");
        sb.AppendLine($"      'challenge': {System.Text.Json.JsonSerializer.Serialize(GetSvgForKey("challenge"))},");
        sb.AppendLine($"      'explore': {System.Text.Json.JsonSerializer.Serialize(GetSvgForKey("explore"))},");
        sb.AppendLine($"      'magic': {System.Text.Json.JsonSerializer.Serialize(GetSvgForKey("magic"))},");
        sb.AppendLine($"      'reflection': {System.Text.Json.JsonSerializer.Serialize(GetSvgForKey("reflection"))}");
        sb.AppendLine("    };");

        sb.AppendLine(@"
    let currentIndex = 0;
    let currentMode = 'split';

    function setMode(mode) {
      currentMode = mode;
      const stage = document.getElementById('stageElement');
      const btnSplit = document.getElementById('btnModeSplit');
      const btnSlide = document.getElementById('btnModeSlide');

      if (mode === 'slide') {
        stage.classList.add('storyboard-mode');
        btnSlide.classList.add('active');
        btnSplit.classList.remove('active');
      } else {
        stage.classList.remove('storyboard-mode');
        btnSplit.classList.add('active');
        btnSlide.classList.remove('active');
      }
    }

    function goToChapter(index) {
      if (index < 0 || index >= chapters.length) return;
      currentIndex = index;
      renderCurrentChapter();
    }

    function nextChapter() {
      if (currentIndex < chapters.length - 1) {
        goToChapter(currentIndex + 1);
      }
    }

    function prevChapter() {
      if (currentIndex > 0) {
        goToChapter(currentIndex - 1);
      }
    }

    function renderCurrentChapter() {
      const ch = chapters[currentIndex];
      document.getElementById('chapterPill').innerText = `Step ${currentIndex + 1} of ${chapters.length} • ${ch.phaseTag}`;
      document.getElementById('chapterDuration').innerText = `⏱️ ${ch.duration}`;
      document.getElementById('diagramStepNum').innerText = `STEP ${currentIndex + 1}`;
      document.getElementById('chapterTitle').innerText = ch.title;
      document.getElementById('chapterNarrative').innerText = ch.narrative;
      document.getElementById('teacherScript').innerText = ch.teacherScript;
      document.getElementById('childAction').innerText = ch.childAction;
      document.getElementById('teacherSpark').innerText = ch.teacherSpark;
      document.getElementById('wonderPrompt').innerText = ch.wonderPrompt;

      // Update Artwork Diagram
      const svgContainer = document.getElementById('svgBackdrop');
      if (svgMap[ch.svgKey]) {
        svgContainer.innerHTML = svgMap[ch.svgKey];
      }

      // Update Stepper & Dots
      chapters.forEach((_, i) => {
        const step = document.getElementById(`step-${i}`);
        const dot = document.getElementById(`dot-${i}`);
        if (step) step.className = i === currentIndex ? 'step-btn active' : 'step-btn';
        if (dot) dot.className = i === currentIndex ? 'dot active' : 'dot';
      });

      // Update Nav Buttons
      document.getElementById('prevBtn').disabled = currentIndex === 0;
      const nextBtn = document.getElementById('nextBtn');
      if (currentIndex === chapters.length - 1) {
        nextBtn.innerText = '↺ Replay from Step 1';
        nextBtn.onclick = () => goToChapter(0);
      } else {
        nextBtn.innerText = `Next: ${chapters[currentIndex + 1].title} →`;
        nextBtn.onclick = nextChapter;
      }
    }

    // Keyboard navigation
    window.addEventListener('keydown', (e) => {
      if (e.key === 'ArrowRight') nextChapter();
      if (e.key === 'ArrowLeft') prevChapter();
    });

    renderCurrentChapter();
  </script>
</body>
</html>");

        return sb.ToString();
    }

    public static string GetSvgForKey(string key)
    {
        return key switch
        {
            "gathering" => @"
<svg viewBox=""0 0 800 360"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"" style=""width:100%;height:100%;"">
  <defs>
    <linearGradient id=""gSky"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""100%"">
      <stop offset=""0%"" stop-color=""#fdfbf7""/>
      <stop offset=""60%"" stop-color=""#f4efe5""/>
      <stop offset=""100%"" stop-color=""#e9f0eb""/>
    </linearGradient>
    <radialGradient id=""gSun"" cx=""400"" cy=""90"" r=""220"" gradientUnits=""userSpaceOnUse"">
      <stop offset=""0%"" stop-color=""#faeecf"" stop-opacity=""0.7""/>
      <stop offset=""60%"" stop-color=""#faeecf"" stop-opacity=""0.2""/>
      <stop offset=""100%"" stop-color=""#faeecf"" stop-opacity=""0""/>
    </radialGradient>
    <linearGradient id=""gWood"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""0%"">
      <stop offset=""0%"" stop-color=""#dcc9a8""/>
      <stop offset=""50%"" stop-color=""#ebd8ba""/>
      <stop offset=""100%"" stop-color=""#d5bf9b""/>
    </linearGradient>
  </defs>

  <!-- Background Wall -->
  <rect width=""800"" height=""360"" fill=""url(#gSky)""/>
  <circle cx=""400"" cy=""90"" r=""220"" fill=""url(#gSun)""/>

  <!-- Grand Arched Studio Window -->
  <path d=""M 300 240 L 300 120 C 300 65, 500 65, 500 120 L 500 240 Z"" fill=""#ffffff"" stroke=""#d9e2db"" stroke-width=""6""/>
  <line x1=""400"" y1=""72"" x2=""400"" y2=""240"" stroke=""#d9e2db"" stroke-width=""3""/>
  <line x1=""300"" y1=""150"" x2=""500"" y2=""150"" stroke=""#d9e2db"" stroke-width=""3""/>
  <polygon points=""310,240 220,330 580,330 490,240"" fill=""rgba(255,255,255,0.45)""/>

  <!-- Scandinavian Floor & Horizon -->
  <line x1=""0"" y1=""255"" x2=""800"" y2=""255"" stroke=""#e0e6e1"" stroke-width=""2""/>
  <line x1=""90"" y1=""255"" x2=""30"" y2=""360"" stroke=""#e8ede8"" stroke-width=""1.5""/>
  <line x1=""240"" y1=""255"" x2=""190"" y2=""360"" stroke=""#e8ede8"" stroke-width=""1.5""/>
  <line x1=""560"" y1=""255"" x2=""610"" y2=""360"" stroke=""#e8ede8"" stroke-width=""1.5""/>
  <line x1=""710"" y1=""255"" x2=""770"" y2=""360"" stroke=""#e8ede8"" stroke-width=""1.5""/>

  <!-- Large Braided Woven Circle Rug with Fringe -->
  <ellipse cx=""400"" cy=""285"" rx=""270"" ry=""56"" fill=""#e6eee9"" stroke=""#557c6c"" stroke-width=""3.5"" stroke-dasharray=""7 5""/>
  <ellipse cx=""400"" cy=""285"" rx=""220"" ry=""44"" fill=""#f5efe3"" stroke=""#d9caa8"" stroke-width=""2""/>

  <!-- Low Montessori Circle Table -->
  <ellipse cx=""400"" cy=""278"" rx=""100"" ry=""22"" fill=""url(#gWood)"" stroke=""#b89e72"" stroke-width=""2""/>
  <ellipse cx=""400"" cy=""275"" rx=""90"" ry=""18"" fill=""#f7eee0""/>

  <!-- Wicker Curiosity Basket overflowing with tactile items -->
  <path d=""M 370 250 L 430 250 L 424 274 L 376 274 Z"" fill=""#a87d55"" stroke=""#8c603a"" stroke-width=""2""/>
  <ellipse cx=""400"" cy=""250"" rx=""30"" ry=""7"" fill=""#c99c72""/>
  <!-- Basket Items: Pinecones, river stones, magnifying glass -->
  <ellipse cx=""390"" cy=""247"" rx=""9"" ry=""6"" fill=""#6b4928""/>
  <ellipse cx=""410"" cy=""246"" rx=""8"" ry=""5"" fill=""#8c9e94""/>
  <circle cx=""400"" cy=""243"" r=""11"" stroke=""#df9b3a"" stroke-width=""3"" fill=""rgba(240,248,244,0.7)""/>
  <line x1=""407"" y1=""250"" x2=""422"" y2=""262"" stroke=""#8c603a"" stroke-width=""4"" stroke-linecap=""round""/>

  <!-- Explorer Floor Cushions around table -->
  <ellipse cx=""260"" cy=""288"" rx=""42"" ry=""18"" fill=""#f4d89a"" stroke=""#df9b3a"" stroke-width=""2.5""/>
  <ellipse cx=""540"" cy=""288"" rx=""42"" ry=""18"" fill=""#f2cdc3"" stroke=""#d9826c"" stroke-width=""2.5""/>
  <ellipse cx=""400"" cy=""312"" rx=""48"" ry=""20"" fill=""#d5e5db"" stroke=""#557c6c"" stroke-width=""2.5""/>

  <!-- Potted Fiddle Leaf Tree (Right) -->
  <path d=""M 650 250 L 685 250 L 680 286 L 655 286 Z"" fill=""#d9826c""/>
  <path d=""M 667 250 Q 655 180 670 140"" stroke=""#3e6253"" stroke-width=""4.5"" fill=""none""/>
  <ellipse cx=""662"" cy=""170"" rx=""24"" ry=""13"" fill=""#557c6c"" transform=""rotate(-25 662 170)""/>
  <ellipse cx=""682"" cy=""145"" rx=""26"" ry=""15"" fill=""#3e6253"" transform=""rotate(20 682 145)""/>
  <ellipse cx=""665"" cy=""125"" rx=""22"" ry=""12"" fill=""#78998a""/>

  <!-- Hanging Potted Eucalyptus (Left) -->
  <line x1=""140"" y1=""0"" x2=""140"" y2=""85"" stroke=""#7c877f"" stroke-width=""2""/>
  <path d=""M 125 85 L 155 85 L 150 110 L 130 110 Z"" fill=""#ab9c76""/>
  <path d=""M 140 85 Q 110 120 120 150 Q 135 125 140 85"" fill=""#557c6c""/>
  <path d=""M 140 85 Q 170 115 160 155 Q 145 130 140 85"" fill=""#3e6253""/>
  <path d=""M 140 85 Q 140 135 145 165 Q 150 135 140 85"" fill=""#78998a""/>

  <!-- Stage Label Badge -->
  <rect x=""30"" y=""24"" width=""290"" height=""32"" rx=""8"" fill=""rgba(255,255,255,0.92)"" stroke=""#557c6c"" stroke-width=""1.5""/>
  <text x=""45"" y=""45"" font-size=""11.5"" font-family=""Figtree, sans-serif"" font-weight=""800"" fill=""#26332d"" letter-spacing=""0.8"">PHASE 1 · SENSORY INVITATION SETUP</text>

  <!-- Sparkles of curiosity -->
  <polygon points=""400,210 404,220 415,222 406,228 408,238 400,232 392,238 394,228 385,222 396,220"" fill=""#e9b65e""/>
  <circle cx=""438"" cy=""218"" r=""3.5"" fill=""#e9b65e""/>
  <circle cx=""362"" cy=""222"" r=""3"" fill=""#e9b65e""/>
</svg>",

            "challenge" => @"
<svg viewBox=""0 0 800 360"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"" style=""width:100%;height:100%;"">
  <defs>
    <linearGradient id=""cSky"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""100%"">
      <stop offset=""0%"" stop-color=""#faf8f3""/>
      <stop offset=""100%"" stop-color=""#eff5f1""/>
    </linearGradient>
  </defs>

  <rect width=""800"" height=""360"" fill=""url(#cSky)""/>
  <circle cx=""400"" cy=""180"" r=""160"" fill=""#edf4f0""/>
  <circle cx=""400"" cy=""180"" r=""110"" fill=""#ffffff""/>

  <!-- Large Montessori Inquiry Workbench -->
  <rect x=""180"" y=""215"" width=""440"" height=""26"" rx=""8"" fill=""#ecd9ba"" stroke=""#ab9c76"" stroke-width=""2.5""/>
  <rect x=""220"" y=""241"" width=""24"" height=""85"" rx=""4"" fill=""#ab9c76""/>
  <rect x=""556"" y=""241"" width=""24"" height=""85"" rx=""4"" fill=""#ab9c76""/>
  <line x1=""180"" y1=""326"" x2=""620"" y2=""326"" stroke=""#e0e6e1"" stroke-width=""3""/>

  <!-- Centerpiece: Mystery Inquiry Box Glowing with Amber Light -->
  <rect x=""330"" y=""135"" width=""140"" height=""80"" rx=""10"" fill=""#557c6c""/>
  <rect x=""322"" y=""128"" width=""156"" height=""18"" rx=""6"" fill=""#3e6253""/>
  <!-- Golden Ribbon & Bow -->
  <rect x=""392"" y=""128"" width=""16"" height=""87"" fill=""#e9b65e""/>
  <ellipse cx=""388"" cy=""120"" rx=""12"" ry=""7"" fill=""#e9b65e"" transform=""rotate(-20 388 120)""/>
  <ellipse cx=""412"" cy=""120"" rx=""12"" ry=""7"" fill=""#e9b65e"" transform=""rotate(20 412 120)""/>
  <circle cx=""400"" cy=""122"" r=""5"" fill=""#df9b3a""/>

  <!-- Giant Detailed Magnifying Glass with Glass Shine -->
  <circle cx=""260"" cy=""175"" r=""38"" stroke=""#df9b3a"" stroke-width=""8"" fill=""rgba(235, 246, 240, 0.75)""/>
  <line x1=""288"" y1=""203"" x2=""325"" y2=""238"" stroke=""#a87d55"" stroke-width=""10"" stroke-linecap=""round""/>
  <path d=""M 240 160 Q 260 148 280 160"" stroke=""white"" stroke-width=""4"" stroke-linecap=""round"" fill=""none""/>

  <!-- Nature specimen jars with leaves and acorns -->
  <rect x=""495"" y=""165"" width=""36"" height=""50"" rx=""6"" fill=""rgba(240,248,244,0.85)"" stroke=""#78998a"" stroke-width=""2""/>
  <rect x=""492"" y=""160"" width=""42"" height=""8"" rx=""3"" fill=""#8c603a""/>
  <path d=""M 513 180 Q 520 195 513 205 Q 506 195 513 180 Z"" fill=""#557c6c""/>

  <rect x=""545"" y=""175"" width=""32"" height=""40"" rx=""6"" fill=""rgba(240,248,244,0.85)"" stroke=""#78998a"" stroke-width=""2""/>
  <rect x=""542"" y=""170"" width=""38"" height=""8"" rx=""3"" fill=""#8c603a""/>
  <circle cx=""561"" cy=""195"" r=""9"" fill=""#df9b3a""/>

  <!-- Wonder Question & Spark Constellations -->
  <text x=""225"" y=""120"" font-size=""44"" font-family=""Figtree, sans-serif"" font-weight=""800"" fill=""#557c6c"">?</text>
  <text x=""550"" y=""115"" font-size=""38"" font-family=""Figtree, sans-serif"" font-weight=""800"" fill=""#df9b3a"">?</text>
  <polygon points=""300,90 304,98 312,100 305,106 307,114 300,109 293,114 295,106 288,100 296,98"" fill=""#e9b65e""/>
  <polygon points=""510,80 513,87 520,89 514,94 516,101 510,96 504,101 506,94 500,89 507,87"" fill=""#557c6c""/>

  <!-- Stage Label Badge -->
  <rect x=""30"" y=""24"" width=""290"" height=""32"" rx=""8"" fill=""rgba(255,255,255,0.92)"" stroke=""#557c6c"" stroke-width=""1.5""/>
  <text x=""45"" y=""45"" font-size=""11.5"" font-family=""Figtree, sans-serif"" font-weight=""800"" fill=""#26332d"" letter-spacing=""0.8"">PHASE 2 · THE WONDER INQUIRY</text>
</svg>",

            "explore" => @"
<svg viewBox=""0 0 800 360"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"" style=""width:100%;height:100%;"">
  <defs>
    <linearGradient id=""eSky"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""100%"">
      <stop offset=""0%"" stop-color=""#f6f8f5""/>
      <stop offset=""100%"" stop-color=""#eef4ee""/>
    </linearGradient>
  </defs>

  <rect width=""800"" height=""360"" fill=""url(#eSky)""/>
  <ellipse cx=""400"" cy=""190"" rx=""320"" ry=""120"" fill=""#ffffff"" stroke=""#e1e9e3"" stroke-width=""2.5""/>

  <!-- Studio Craft Table Surface -->
  <rect x=""140"" y=""225"" width=""520"" height=""24"" rx=""8"" fill=""#eedfc4"" stroke=""#b89e72"" stroke-width=""2""/>
  <rect x=""180"" y=""249"" width=""22"" height=""75"" fill=""#b89e72""/>
  <rect x=""598"" y=""249"" width=""22"" height=""75"" fill=""#b89e72""/>

  <!-- Multi-Colored Montessori Building Blocks & Arches (Center-Left) -->
  <rect x=""260"" y=""195"" width=""65"" height=""30"" rx=""4"" fill=""#557c6c""/>
  <rect x=""335"" y=""195"" width=""65"" height=""30"" rx=""4"" fill=""#e9b65e""/>
  <rect x=""285"" y=""162"" width=""90"" height=""32"" rx=""4"" fill=""#d9826c""/>
  <!-- Arch Block -->
  <path d=""M 305 162 A 25 25 0 0 1 355 162 Z"" fill=""#ffffff""/>
  <polygon points=""330,110 285,160 375,160"" fill=""#3e6253""/>
  <circle cx=""330"" cy=""95"" r=""12"" fill=""#df9b3a""/>

  <!-- Sculpting Clay Pot with Sprout (Center-Right) -->
  <ellipse cx=""475"" cy=""210"" rx=""36"" ry=""15"" fill=""#c9775f""/>
  <path d=""M 439 210 Q 439 235 475 235 Q 511 235 511 210 Z"" fill=""#b3654f""/>
  <line x1=""475"" y1=""210"" x2=""485"" y2=""145"" stroke=""#855e39"" stroke-width=""5"" stroke-linecap=""round""/>
  <circle cx=""485"" cy=""140"" r=""8"" fill=""#557c6c""/>
  <circle cx=""495"" cy=""152"" r=""7"" fill=""#78998a""/>
  <circle cx=""470"" cy=""160"" r=""6"" fill=""#3e6253""/>

  <!-- Watercolor Artist Palette with Daubs (Far Left) -->
  <path d=""M 175 210 C 175 185 235 185 235 210 C 235 235 175 235 175 210 Z"" fill=""#faf5ec"" stroke=""#ab9c76"" stroke-width=""2""/>
  <circle cx=""195"" cy=""202"" r=""6"" fill=""#557c6c""/>
  <circle cx=""215"" cy=""200"" r=""6"" fill=""#df9b3a""/>
  <circle cx=""205"" cy=""216"" r=""6"" fill=""#d9826c""/>
  <circle cx=""188"" cy=""216"" r=""5"" fill=""#3e6253""/>

  <!-- Textured Craft Paper & Animal Silhouette (Far Right) -->
  <rect x=""535"" y=""190"" width=""60"" height=""35"" rx=""4"" fill=""#f4d89a"" stroke=""#df9b3a"" stroke-width=""1.5""/>
  <polygon points=""550,210 565,195 580,210"" fill=""#557c6c""/>
  <!-- Craft sticks -->
  <line x1=""530"" y1=""225"" x2=""560"" y2=""225"" stroke=""#ab9c76"" stroke-width=""4"" stroke-linecap=""round""/>
  <line x1=""565"" y1=""225"" x2=""595"" y2=""225"" stroke=""#ab9c76"" stroke-width=""4"" stroke-linecap=""round""/>

  <!-- Children Handprints Motif in background -->
  <path d=""M 195 130 C 190 120 185 122 187 135 C 185 118 177 120 181 138 C 175 122 167 126 173 142 C 170 132 160 136 167 150 C 173 162 193 165 200 155 Z"" fill=""#f4d89a"" opacity=""0.7""/>
  <path d=""M 610 130 C 615 120 620 122 618 135 C 620 118 628 120 624 138 C 630 122 638 126 632 142 C 635 132 645 136 638 150 C 632 162 612 165 605 155 Z"" fill=""#e5eee8"" stroke=""#557c6c"" stroke-width=""1.5""/>

  <!-- Stage Label Badge -->
  <rect x=""30"" y=""24"" width=""290"" height=""32"" rx=""8"" fill=""rgba(255,255,255,0.92)"" stroke=""#557c6c"" stroke-width=""1.5""/>
  <text x=""45"" y=""45"" font-size=""11.5"" font-family=""Figtree, sans-serif"" font-weight=""800"" fill=""#26332d"" letter-spacing=""0.8"">PHASE 3 · ACTIVE HANDS-ON FLOW</text>
</svg>",

            "magic" => @"
<svg viewBox=""0 0 800 360"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"" style=""width:100%;height:100%;"">
  <defs>
    <radialGradient id=""mGlow"" cx=""50%"" cy=""50%"" r=""50%"">
      <stop offset=""0%"" stop-color=""#fff9e6""/>
      <stop offset=""60%"" stop-color=""#eef7f2""/>
      <stop offset=""100%"" stop-color=""#f8faf8""/>
    </radialGradient>
    <linearGradient id=""mRainbow"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""100%"">
      <stop offset=""0%"" stop-color=""rgba(217, 130, 108, 0.75)""/>
      <stop offset=""35%"" stop-color=""rgba(233, 182, 94, 0.75)""/>
      <stop offset=""70%"" stop-color=""rgba(85, 124, 108, 0.75)""/>
      <stop offset=""100%"" stop-color=""rgba(62, 98, 83, 0.75)""/>
    </linearGradient>
  </defs>

  <rect width=""800"" height=""360"" fill=""url(#mGlow)""/>

  <!-- Luminous Starburst Rings -->
  <circle cx=""400"" cy=""175"" r=""130"" stroke=""#e6eee9"" stroke-width=""3.5"" stroke-dasharray=""7 7""/>
  <circle cx=""400"" cy=""175"" r=""90"" stroke=""#f4d89a"" stroke-width=""2.5"" stroke-dasharray=""5 5""/>

  <!-- Optical Refracting Crystal Prism -->
  <polygon points=""400,105 340,210 460,210"" fill=""rgba(255,255,255,0.92)"" stroke=""#557c6c"" stroke-width=""4.5""/>
  <polygon points=""400,105 400,210 460,210"" fill=""rgba(230,238,233,0.55)""/>

  <!-- Radiant Soft Rainbow Beam -->
  <polygon points=""400,160 620,90 660,125"" fill=""url(#mRainbow)""/>

  <!-- Giant Golden Wonder Sparkle -->
  <polygon points=""400,70 410,92 432,96 415,112 420,134 400,122 380,134 385,112 368,96 390,92"" fill=""#e9b65e""/>
  <circle cx=""400"" cy=""102"" r=""5"" fill=""white""/>

  <!-- Dancing Celebration Stars -->
  <polygon points=""240,125 244,134 253,136 245,142 248,151 240,145 232,151 235,142 227,136 236,134"" fill=""#df9b3a""/>
  <polygon points=""560,200 564,209 573,211 565,217 568,226 560,220 552,226 555,217 547,211 556,209"" fill=""#557c6c""/>
  <polygon points=""290,230 293,237 300,238 294,243 296,250 290,245 284,250 286,243 280,238 287,237"" fill=""#d9826c""/>

  <!-- Pedestal of Discovery -->
  <ellipse cx=""400"" cy=""250"" rx=""110"" ry=""18"" fill=""#e2ebe4"" stroke=""#557c6c"" stroke-width=""2.5""/>
  <text x=""400"" y=""295"" text-anchor=""middle"" font-size=""16"" font-family=""Figtree, sans-serif"" font-weight=""800"" fill=""#3e6253"" letter-spacing=""2"">A MOMENT OF PURE DISCOVERY</text>

  <!-- Stage Label Badge -->
  <rect x=""30"" y=""24"" width=""290"" height=""32"" rx=""8"" fill=""rgba(255,255,255,0.92)"" stroke=""#557c6c"" stroke-width=""1.5""/>
  <text x=""45"" y=""45"" font-size=""11.5"" font-family=""Figtree, sans-serif"" font-weight=""800"" fill=""#26332d"" letter-spacing=""0.8"">PHASE 4 · THE DISCOVERY SPARK</text>
</svg>",

            "reef" or "ocean" => @"
<svg viewBox=""0 0 800 360"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"" style=""width:100%;height:100%;"">
  <defs>
    <linearGradient id=""seaBg"" x1=""0%"" y1=""0%"" x2=""0%"" y2=""100%"">
      <stop offset=""0%"" stop-color=""#e6f4f1""/>
      <stop offset=""50%"" stop-color=""#ccebe5""/>
      <stop offset=""100%"" stop-color=""#a2dad0""/>
    </linearGradient>
  </defs>
  <rect width=""800"" height=""360"" fill=""url(#seaBg)""/>
  <!-- Gentle sea ripples -->
  <path d=""M0 120 Q 200 90 400 120 T 800 120"" stroke=""rgba(255,255,255,0.7)"" stroke-width=""5"" fill=""none""/>
  <path d=""M0 160 Q 200 130 400 160 T 800 160"" stroke=""rgba(255,255,255,0.5)"" stroke-width=""3"" fill=""none""/>
  <!-- Coral seafloor -->
  <path d=""M0 310 Q 250 280 500 310 T 800 300 L 800 360 L 0 360 Z"" fill=""#ebdcc5""/>
  <!-- Coral fans & sea anemones -->
  <path d=""M 120 310 C 100 240, 160 210, 140 310"" stroke=""#d9826c"" stroke-width=""14"" stroke-linecap=""round""/>
  <path d=""M 150 310 C 140 230, 200 220, 170 310"" stroke=""#e9b65e"" stroke-width=""10"" stroke-linecap=""round""/>
  <path d=""M 650 310 C 630 220, 710 200, 680 310"" stroke=""#557c6c"" stroke-width=""14"" stroke-linecap=""round""/>
  <!-- Colorful Fish Friends Swimming -->
  <ellipse cx=""320"" cy=""180"" rx=""24"" ry=""14"" fill=""#e9b65e""/>
  <polygon points=""340,180 358,168 358,192"" fill=""#df9b3a""/>
  <circle cx=""310"" cy=""177"" r=""3"" fill=""#26332d""/>

  <ellipse cx=""480"" cy=""140"" rx=""26"" ry=""15"" fill=""#d9826c""/>
  <polygon points=""502,140 522,128 522,152"" fill=""#c96e57""/>
  <circle cx=""470"" cy=""137"" r=""3"" fill=""#ffffff""/>

  <ellipse cx=""240"" cy=""220"" rx=""20"" ry=""11"" fill=""#557c6c""/>
  <polygon points=""258,220 274,210 274,230"" fill=""#3e6253""/>
  <circle cx=""232"" cy=""218"" r=""2.5"" fill=""#ffffff""/>

  <!-- Bubbles -->
  <circle cx=""330"" cy=""140"" r=""7"" stroke=""white"" stroke-width=""2"" fill=""rgba(255,255,255,0.4)""/>
  <circle cx=""345"" cy=""115"" r=""5"" stroke=""white"" stroke-width=""1.5"" fill=""rgba(255,255,255,0.4)""/>
  <circle cx=""490"" cy=""100"" r=""6"" stroke=""white"" stroke-width=""2"" fill=""rgba(255,255,255,0.4)""/>
  <!-- Stage Label Badge -->
  <rect x=""30"" y=""24"" width=""320"" height=""32"" rx=""8"" fill=""rgba(255,255,255,0.92)"" stroke=""#557c6c"" stroke-width=""1.5""/>
  <text x=""45"" y=""45"" font-size=""11.5"" font-family=""Figtree, sans-serif"" font-weight=""800"" fill=""#26332d"" letter-spacing=""0.8"">OCEAN EXPLORATION REEF BLUEPRINT</text>
</svg>",

            "reflection" or _ => @"
<svg viewBox=""0 0 800 360"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"" style=""width:100%;height:100%;"">
  <defs>
    <linearGradient id=""rSky"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""100%"">
      <stop offset=""0%"" stop-color=""#fdfbf7""/>
      <stop offset=""100%"" stop-color=""#f0f6f2""/>
    </linearGradient>
  </defs>

  <rect width=""800"" height=""360"" fill=""url(#rSky)""/>

  <!-- Festive Natural Linen Bunting Garland Swagged across studio -->
  <path d=""M 100 45 Q 250 90 400 55 Q 550 90 700 45"" stroke=""#7c877f"" stroke-width=""2.5"" fill=""none""/>
  <!-- Pennants -->
  <polygon points=""160,60 185,65 172,98"" fill=""#557c6c""/>
  <polygon points=""235,72 260,72 248,108"" fill=""#e9b65e""/>
  <polygon points=""315,68 340,64 328,102"" fill=""#d9826c""/>
  <polygon points=""435,64 460,68 448,102"" fill=""#3e6253""/>
  <polygon points=""515,72 540,72 528,108"" fill=""#df9b3a""/>
  <polygon points=""595,62 620,56 608,94"" fill=""#78998a""/>

  <!-- Proud Gallery Exhibition Display Shelf -->
  <rect x=""140"" y=""205"" width=""520"" height=""18"" rx=""5"" fill=""#ab9c76"" stroke=""#8c603a"" stroke-width=""2""/>
  
  <!-- Masterpiece Sculptures Proudly Displayed on Shelf -->
  <!-- 1. Wooden Arch Tower -->
  <rect x=""200"" y=""155"" width=""42"" height=""50"" rx=""6"" fill=""#557c6c""/>
  <circle cx=""221"" cy=""142"" r=""14"" fill=""#e9b65e""/>
  
  <!-- 2. Clay Pot & Leaf -->
  <polygon points=""400"" y=""125"" points=""400,125 430,203 370,203"" fill=""#d9826c""/>
  <circle cx=""400"" cy=""160"" r=""16"" fill=""#ffffff""/>
  <path d=""M 394 157 C 394 152 400 151 400 155 C 400 151 406 152 406 157 C 406 163 400 166 400 166 C 400 166 394 163 394 157 Z"" fill=""#d9826c""/>

  <!-- 3. Animal Collage & Sticks -->
  <rect x=""560"" y=""162"" width=""52"" height=""43"" rx=""6"" fill=""#3e6253""/>
  <ellipse cx=""586"" cy=""150"" rx=""18"" ry=""10"" fill=""#f4d89a""/>

  <!-- Little Stars of Accomplishment -->
  <polygon points=""170,115 174,123 182,124 175,130 178,138 170,133 162,138 165,130 158,124 166,123"" fill=""#e9b65e""/>
  <polygon points=""630,115 634,123 642,124 635,130 638,138 630,133 622,138 625,130 618,124 626,123"" fill=""#e9b65e""/>
  <polygon points=""400,85 404,95 415,97 407,105 409,116 400,110 391,116 393,105 385,97 396,95"" fill=""#e9b65e""/>

  <!-- Closing Reflection Circle Rug -->
  <ellipse cx=""400"" cy=""285"" rx=""290"" ry=""48"" fill=""#eef5f1"" stroke=""#557c6c"" stroke-width=""2.5""/>
  <text x=""400"" y=""292"" text-anchor=""middle"" font-size=""16"" font-family=""Figtree, sans-serif"" font-weight=""800"" fill=""#26332d"" letter-spacing=""0.5"">Stories Shared · Little Hands, Big Minds · Memories Made</text>

  <!-- Stage Label Badge -->
  <rect x=""30"" y=""24"" width=""290"" height=""32"" rx=""8"" fill=""rgba(255,255,255,0.92)"" stroke=""#557c6c"" stroke-width=""1.5""/>
  <text x=""45"" y=""45"" font-size=""11.5"" font-family=""Figtree, sans-serif"" font-weight=""800"" fill=""#26332d"" letter-spacing=""0.8"">FINAL PHASE · CIRCLE SHARING &amp; REFLECTION</text>
</svg>"
        };
    }
}

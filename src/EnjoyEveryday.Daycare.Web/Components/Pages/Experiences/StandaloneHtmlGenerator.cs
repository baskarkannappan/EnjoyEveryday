using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Daycare.Web.Components.Pages.Experiences;

namespace EnjoyEveryday.Daycare.Web.Components.Pages;

public static class StandaloneHtmlGenerator
{
    public static List<ExperienceStorybook.StoryChapter> BuildChapters(Experience? exp, ExperienceDna dna)
    {
        var chapters = new List<ExperienceStorybook.StoryChapter>();
        var title = exp?.Title ?? "Wonder Experience";
        var mission = !string.IsNullOrWhiteSpace(exp?.Description) ? exp.Description : "Discovering new wonders through playful hands and curious eyes.";
        var challenge = !string.IsNullOrWhiteSpace(dna.Challenge) ? dna.Challenge : "Explore materials, test ideas, and discover something surprising together.";
        var create = !string.IsNullOrWhiteSpace(dna.Create) ? dna.Create : "Children engage hands-on with organic shapes, sensory textures, and spontaneous creation.";
        var magic = !string.IsNullOrWhiteSpace(dna.MagicMoment) ? dna.MagicMoment : "A sudden flash of excitement when materials combine in an unexpected, beautiful way.";
        var accomplishment = !string.IsNullOrWhiteSpace(dna.Accomplishment) ? dna.Accomplishment : "Children beam with pride as they showcase their creation to friends.";
        var reflection = !string.IsNullOrWhiteSpace(dna.Reflection) ? dna.Reflection : "Gathering back in a gentle circle to share what we learned and felt today.";

        if (dna.ExecutionGuide?.Phases != null && dna.ExecutionGuide.Phases.Count >= 3)
        {
            var phases = dna.ExecutionGuide.Phases;
            string[] sceneKeys = { "gathering", "challenge", "explore", "magic", "reflection" };

            for (int i = 0; i < phases.Count; i++)
            {
                var p = phases[i];
                var sceneKey = sceneKeys[Math.Min(i, sceneKeys.Length - 1)];
                var prompt = i == 0 ? (dna.ExecutionGuide.Ask ?? "What curious things do you see here?") :
                             i == 1 ? challenge :
                             i == phases.Count - 1 ? reflection : magic;

                chapters.Add(new ExperienceStorybook.StoryChapter
                {
                    Number = i + 1,
                    Title = string.IsNullOrWhiteSpace(p.Name) ? $"Chapter {i + 1}" : p.Name,
                    PhaseTag = i == 0 ? "The Invitation & Spark" : i == phases.Count - 1 ? "Celebration & Memory" : $"Phase {i + 1}",
                    Duration = p.DurationMinutes > 0 ? $"{p.DurationMinutes} min" : "5 min",
                    Narrative = $"Welcome to our journey with {title}. {p.ChildrenDo}. Every little movement brings a new spark of joyful understanding.",
                    ChildrenAction = string.IsNullOrWhiteSpace(p.ChildrenDo) ? "Children explore with open hands and focused curiosity." : p.ChildrenDo,
                    TeacherSpark = string.IsNullOrWhiteSpace(p.TeacherDoes) ? "Observe and offer open-ended support when invited." : p.TeacherDoes,
                    WonderPrompt = prompt,
                    SvgSceneKey = sceneKey
                });
            }
        }
        else
        {
            chapters.Add(new ExperienceStorybook.StoryChapter
            {
                Number = 1,
                Title = "The Gathering & Curious Invitation",
                PhaseTag = "Chapter 1: The Spark",
                Duration = "5 min",
                Narrative = $"On a bright morning filled with gentle possibility, our little explorers gather closely. Today's quest is '{title}'. As we open the mystery tray, gentle eyes light up with wonder.",
                ChildrenAction = "Children gather around the sensory rug, feeling the smooth textures and whispering guesses about today's mystery.",
                TeacherSpark = !string.IsNullOrWhiteSpace(dna.ExecutionGuide?.TeacherIntroduction) ? dna.ExecutionGuide.TeacherIntroduction : "Present the mystery basket with calm excitement. Ask: 'What curious secrets are hiding inside?'",
                WonderPrompt = !string.IsNullOrWhiteSpace(dna.ExecutionGuide?.Ask) ? dna.ExecutionGuide.Ask : "What does this feel like in your hands? Have you ever seen this before?",
                SvgSceneKey = "gathering"
            });

            chapters.Add(new ExperienceStorybook.StoryChapter
            {
                Number = 2,
                Title = "The Wonder Challenge",
                PhaseTag = "Chapter 2: The Inquiry",
                Duration = "5 min",
                Narrative = $"Now comes the irresistible challenge: {challenge}. There are no right or wrong answers—only invitations to experiment, hypothesize, and build.",
                ChildrenAction = "Children examine their tools, turn them over in their fingers, and whisper ideas to their neighbor with joyful energy.",
                TeacherSpark = "Frame the challenge as an adventure. Encourage each child that every idea is valuable and worth trying.",
                WonderPrompt = "How do you think we can make this work? What if we try it together?",
                SvgSceneKey = "challenge"
            });

            chapters.Add(new ExperienceStorybook.StoryChapter
            {
                Number = 3,
                Title = "Hands-On Creation & Play",
                PhaseTag = "Chapter 3: Deep Flow",
                Duration = "10 min",
                Narrative = $"The room fills with the happy hum of active creation. {create}. Little hands mold, balance, test, and arrange natural treasures into brand new worlds.",
                ChildrenAction = "Deeply engaged in flow state: sorting colors, building structures, testing balance, and sharing supplies kindly.",
                TeacherSpark = !string.IsNullOrWhiteSpace(dna.ExecutionGuide?.Watch) ? dna.ExecutionGuide.Watch : "Observe problem-solving moments. Notice who collaborates and who finds a peaceful solo focus.",
                WonderPrompt = "Look what happens when these two come together! What story is your creation telling?",
                SvgSceneKey = "explore"
            });

            chapters.Add(new ExperienceStorybook.StoryChapter
            {
                Number = 4,
                Title = "The Magic Moment & Discovery",
                PhaseTag = "Chapter 4: The Eureka!",
                Duration = "5 min",
                Narrative = $"Suddenly, someone gasps! {magic}. A shared ripple of delight touches every heart in the room. A genuine spark of childhood discovery is born.",
                ChildrenAction = "Eyes widen with excitement, pointing out their breakthrough to friends with wide, luminous smiles.",
                TeacherSpark = "Celebrate the discovery immediately! Say: 'You discovered that all on your own! How did that happen?'",
                WonderPrompt = "Did you see that? What made it happen? Can we do it again?",
                SvgSceneKey = "magic"
            });

            chapters.Add(new ExperienceStorybook.StoryChapter
            {
                Number = 5,
                Title = "Pride, Reflection & Story Sharing",
                PhaseTag = "Chapter 5: Reflection",
                Duration = "5 min",
                Narrative = $"{accomplishment}. Each child holds up their triumph with gentle pride. {reflection}. The memories of today will stay warmly in our hearts.",
                ChildrenAction = "Children proudly present their work, explain their creative decisions, and help tidy up their sanctuary.",
                TeacherSpark = "Capture the moment with a warm photo or learning note. Guide a closing gratitude breath for our journey together.",
                WonderPrompt = "What was your favorite part of today's adventure? Who would you like to share this story with?",
                SvgSceneKey = "reflection"
            });
        }

        return chapters;
    }

    public static string GenerateStorybookHtml(Experience exp, ExperienceDna dna)
    {
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

        return GenerateHtml(
            exp.Title ?? "Wonder Experience",
            exp.Description ?? "",
            ageRange,
            setting,
            duration,
            materials,
            dna.ExecutionGuide?.Watch ?? "",
            dna.ExecutionGuide?.Ask ?? "",
            dna.ExecutionGuide?.Try ?? "",
            chapters
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
        List<ExperienceStorybook.StoryChapter> chapters)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"  <title>{HttpUtility.HtmlEncode(title)} — Interactive Storybook</title>");
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
      font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif;
      background: var(--bg);
      color: var(--text);
      line-height: 1.5;
      padding: 30px 24px 60px;
    }

    .container {
      max-width: 1440px;
      margin: 0 auto;
    }

    /* Top Hero */
    .hero {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 24px;
      margin-bottom: 30px;
      flex-wrap: wrap;
    }

    .hero-eyebrow {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 11px;
      font-weight: 800;
      letter-spacing: 1px;
      color: var(--accent-dark);
      text-transform: uppercase;
      margin-bottom: 8px;
    }

    .badge-pill {
      background: var(--accent-light);
      color: var(--accent-dark);
      padding: 3px 10px;
      border-radius: 999px;
      font-size: 11px;
      font-weight: 700;
    }

    .hero-title {
      font-size: 32px;
      font-weight: 800;
      letter-spacing: -0.5px;
      color: var(--text);
      margin-bottom: 8px;
    }

    .hero-desc {
      color: var(--muted);
      font-size: 15px;
      max-width: 780px;
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
      font-size: 14px;
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

    /* Split Grid */
    .split-layout {
      display: grid;
      grid-template-columns: 380px 1fr;
      gap: 28px;
    }

    @media (max-width: 1024px) {
      .split-layout {
        grid-template-columns: 1fr;
      }
    }

    /* Cards */
    .card {
      background: var(--surface);
      border: 1px solid var(--border);
      border-radius: var(--radius);
      box-shadow: var(--shadow);
      padding: 22px;
      margin-bottom: 22px;
    }

    .card-title-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 14px;
    }

    .card-title {
      font-size: 15px;
      font-weight: 750;
      color: var(--text);
      display: flex;
      align-items: center;
      gap: 8px;
    }

    /* Chapter Stepper */
    .stepper {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .step-btn {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 12px 14px;
      border-radius: 12px;
      background: var(--surface-soft);
      border: 1px solid transparent;
      cursor: pointer;
      text-align: left;
      width: 100%;
      transition: all 0.2s ease;
    }

    .step-btn:hover {
      background: #edf3ee;
    }

    .step-btn.active {
      background: var(--accent-light);
      border-color: var(--accent);
    }

    .step-num {
      width: 28px;
      height: 28px;
      border-radius: 50%;
      background: white;
      color: var(--accent-dark);
      font-weight: 800;
      font-size: 12px;
      display: flex;
      align-items: center;
      justify-content: center;
      border: 1px solid var(--border);
      flex-shrink: 0;
    }

    .step-btn.active .step-num {
      background: var(--accent);
      color: white;
      border-color: var(--accent);
    }

    .step-text h4 {
      font-size: 13px;
      font-weight: 700;
      color: var(--text);
      margin-bottom: 2px;
    }

    .step-text p {
      font-size: 11px;
      color: var(--muted);
    }

    /* Audio Controller */
    .narrator-box {
      background: linear-gradient(135deg, #f7faf8, var(--accent-light));
      border: 1px solid #d4e2d8;
    }

    .narrator-inner {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .play-circle {
      width: 44px;
      height: 44px;
      border-radius: 50%;
      background: var(--accent);
      color: white;
      border: none;
      display: flex;
      align-items: center;
      justify-content: center;
      cursor: pointer;
      font-size: 18px;
      flex-shrink: 0;
      transition: transform 0.2s;
    }

    .play-circle:hover {
      background: var(--accent-dark);
      transform: scale(1.05);
    }

    .speed-btns {
      margin-left: auto;
      display: flex;
      gap: 4px;
    }

    .speed-btn {
      padding: 4px 8px;
      font-size: 11px;
      border-radius: 6px;
      border: 1px solid var(--border);
      background: white;
      cursor: pointer;
      font-weight: 600;
    }

    .speed-btn.active {
      background: var(--accent-dark);
      color: white;
      border-color: var(--accent-dark);
    }

    /* Materials Checklist */
    .mat-item {
      display: flex;
      align-items: center;
      gap: 10px;
      font-size: 13px;
      color: var(--text);
      padding: 6px 0;
      border-bottom: 1px dashed var(--border);
    }
    .mat-item:last-child { border-bottom: none; }
    .mat-item input { accent-color: var(--accent); cursor: pointer; width: 16px; height: 16px; }

    /* Teacher cues */
    .cue-pill {
      display: inline-block;
      font-size: 10px;
      font-weight: 800;
      letter-spacing: 0.5px;
      padding: 2px 8px;
      border-radius: 6px;
      margin-bottom: 4px;
    }
    .cue-watch { background: #eef2ff; color: #3730a3; }
    .cue-ask { background: #fef3c7; color: #92400e; }
    .cue-try { background: #ecfdf5; color: #065f46; }

    .cue-item {
      margin-bottom: 14px;
    }
    .cue-item p {
      font-size: 13px;
      color: #3b4740;
    }

    /* Theatre Stage */
    .stage {
      background: var(--surface);
      border: 1px solid var(--border);
      border-radius: var(--radius);
      box-shadow: var(--shadow);
      padding: 32px;
      position: sticky;
      top: 24px;
    }

    .stage-top {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }

    .stage-svg-box {
      width: 100%;
      height: 280px;
      background: linear-gradient(180deg, #fbfdfb, var(--surface-soft));
      border-radius: 14px;
      overflow: hidden;
      display: flex;
      align-items: center;
      justify-content: center;
      margin-bottom: 24px;
      border: 1px solid #edf1eb;
    }

    .stage-svg-box svg {
      width: 100%;
      height: 100%;
    }

    .scene-meta-row {
      display: flex;
      gap: 10px;
      align-items: center;
      margin-bottom: 8px;
    }

    .scene-tag {
      font-size: 11px;
      font-weight: 800;
      color: var(--accent-dark);
      background: var(--accent-light);
      padding: 3px 10px;
      border-radius: 6px;
      text-transform: uppercase;
    }

    .scene-heading {
      font-size: 26px;
      font-weight: 800;
      letter-spacing: -0.4px;
      color: var(--text);
      margin-bottom: 16px;
    }

    .narration-quote {
      font-size: 18px;
      line-height: 1.6;
      color: #26332d;
      background: #fafbf9;
      border-left: 4px solid var(--accent);
      padding: 18px 22px;
      border-radius: 0 12px 12px 0;
      font-style: italic;
      margin-bottom: 24px;
    }

    /* Perspectives */
    .perspectives {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 18px;
      margin-bottom: 24px;
    }

    @media (max-width: 768px) {
      .perspectives { grid-template-columns: 1fr; }
    }

    .perspective-card {
      padding: 16px;
      border-radius: 12px;
      background: var(--surface-soft);
      border: 1px solid var(--border);
    }

    .persp-header {
      display: flex;
      align-items: center;
      gap: 8px;
      font-weight: 750;
      font-size: 13px;
      margin-bottom: 6px;
    }

    .child-card .persp-header { color: #3e6253; }
    .teacher-card .persp-header { color: #855e39; }

    .perspective-card p {
      font-size: 13px;
      color: #4b5850;
    }

    .wonder-box {
      background: #fffbeb;
      border: 1px solid #fef3c7;
      padding: 14px 18px;
      border-radius: 12px;
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 26px;
    }

    .wonder-box strong {
      color: #92400e;
      font-size: 13px;
    }

    .stage-bottom {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding-top: 18px;
      border-top: 1px solid var(--border);
      gap: 12px;
    }

    .dots-nav {
      display: flex;
      gap: 6px;
      align-items: center;
    }

    .dot {
      width: 10px;
      height: 10px;
      border-radius: 50%;
      background: #cfd8d2;
      cursor: pointer;
      transition: all 0.2s;
    }

    .dot.active {
      background: var(--accent);
      width: 24px;
      border-radius: 999px;
    }

    @media print {
      body { background: white; padding: 0; }
      .hero-actions, .narrator-box, .stage-bottom, .speed-btns { display: none !important; }
      .split-layout { display: block; }
      .stage { page-break-after: always; box-shadow: none; border: 1px solid #ccc; }
    }
  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine("  <div class=\"container\">");

        // Hero
        sb.AppendLine("    <header class=\"hero\">");
        sb.AppendLine("      <div>");
        sb.AppendLine("        <div class=\"hero-eyebrow\">");
        sb.AppendLine("          <span>✨</span>");
        sb.AppendLine("          <span>ENJOYEVERYDAY STORYBOOK SHOWCASE</span>");
        sb.AppendLine($"          <span class=\"badge-pill\">Ages {HttpUtility.HtmlEncode(ageRange)}</span>");
        sb.AppendLine($"          <span class=\"badge-pill\">{HttpUtility.HtmlEncode(setting)}</span>");
        sb.AppendLine("        </div>");
        sb.AppendLine($"        <h1 class=\"hero-title\">{HttpUtility.HtmlEncode(title)}</h1>");
        sb.AppendLine($"        <p class=\"hero-desc\">{HttpUtility.HtmlEncode(description)}</p>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div class=\"hero-actions\">");
        sb.AppendLine("        <button class=\"btn primary\" id=\"listenTopBtn\" onclick=\"toggleNarration()\">");
        sb.AppendLine("          <span id=\"playIconTop\">▶</span> <span id=\"playTextTop\">Listen to Story</span>");
        sb.AppendLine("        </button>");
        sb.AppendLine("        <button class=\"btn secondary\" onclick=\"window.print()\">🖨️ Print Storybook</button>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </header>");

        // Split Layout
        sb.AppendLine("    <div class=\"split-layout\">");

        // Sidebar
        sb.AppendLine("      <aside class=\"sidebar\">");

        // Audio Narrator Deck
        sb.AppendLine("        <div class=\"card narrator-box\">");
        sb.AppendLine("          <div class=\"narrator-inner\">");
        sb.AppendLine("            <button class=\"play-circle\" id=\"playMainBtn\" onclick=\"toggleNarration()\">▶</button>");
        sb.AppendLine("            <div>");
        sb.AppendLine("              <div style=\"font-size: 13px; font-weight: 750; color: var(--text);\">Story Narration Voice</div>");
        sb.AppendLine("              <div id=\"narratorStatus\" style=\"font-size: 11px; color: var(--muted);\">Ready to play</div>");
        sb.AppendLine("            </div>");
        sb.AppendLine("            <div class=\"speed-btns\">");
        sb.AppendLine("              <button class=\"speed-btn\" onclick=\"setRate(0.85, this)\">0.8x</button>");
        sb.AppendLine("              <button class=\"speed-btn active\" onclick=\"setRate(1.0, this)\">1.0x</button>");
        sb.AppendLine("              <button class=\"speed-btn\" onclick=\"setRate(1.15, this)\">1.2x</button>");
        sb.AppendLine("            </div>");
        sb.AppendLine("          </div>");
        sb.AppendLine("          <label style=\"display: flex; align-items: center; gap: 6px; font-size: 11px; color: var(--muted); margin-top: 10px; cursor: pointer;\">");
        sb.AppendLine("            <input type=\"checkbox\" id=\"autoAdvanceCheck\" checked style=\"accent-color: var(--accent);\" />");
        sb.AppendLine("            <span>Auto-advance slides on narration finish</span>");
        sb.AppendLine("          </label>");
        sb.AppendLine("        </div>");

        // Stepper
        sb.AppendLine("        <div class=\"card\">");
        sb.AppendLine("          <div class=\"card-title-row\">");
        sb.AppendLine("            <div class=\"card-title\"><span>📑</span> Story Chapters</div>");
        sb.AppendLine($"            <span style=\"font-size: 12px; font-weight: 700; color: var(--accent-dark);\">~{durationMinutes} min</span>");
        sb.AppendLine("          </div>");
        sb.AppendLine("          <div class=\"stepper\" id=\"stepperContainer\">");

        for (int i = 0; i < chapters.Count; i++)
        {
            var ch = chapters[i];
            var activeClass = i == 0 ? " active" : "";
            sb.AppendLine($"            <button class=\"step-btn{activeClass}\" onclick=\"goToChapter({i})\" id=\"step-{i}\">");
            sb.AppendLine($"              <div class=\"step-num\">{i + 1}</div>");
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
        sb.AppendLine("            <div class=\"card-title\"><span>🌿</span> Sensory & Material Kit</div>");
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

        // Stage Main
        sb.AppendLine("      <main>");
        sb.AppendLine("        <div class=\"stage\" id=\"stageElement\">");
        sb.AppendLine("          <div class=\"stage-top\">");
        sb.AppendLine("            <div class=\"scene-meta-row\">");
        sb.AppendLine("              <span class=\"scene-tag\" id=\"chapterPill\">Chapter 1 of " + chapters.Count + "</span>");
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

        // Illustrated Scene Artwork Box
        sb.AppendLine("          <div class=\"stage-svg-box\" id=\"svgBackdrop\">");
        if (chapters.Count > 0)
        {
            sb.AppendLine(GetSvgForKey(chapters[0].SvgSceneKey));
        }
        sb.AppendLine("          </div>");

        // Narrative Section
        sb.AppendLine("          <h2 class=\"scene-heading\" id=\"chapterTitle\">" + HttpUtility.HtmlEncode(chapters[0].Title) + "</h2>");
        sb.AppendLine("          <div class=\"narration-quote\" id=\"chapterNarrative\">“" + HttpUtility.HtmlEncode(chapters[0].Narrative) + "”</div>");

        // Perspectives
        sb.AppendLine("          <div class=\"perspectives\">");
        sb.AppendLine("            <div class=\"perspective-card child-card\">");
        sb.AppendLine("              <div class=\"persp-header\"><span>👧👦</span> The Little Explorers</div>");
        sb.AppendLine("              <p id=\"childAction\">" + HttpUtility.HtmlEncode(chapters[0].ChildrenAction) + "</p>");
        sb.AppendLine("            </div>");
        sb.AppendLine("            <div class=\"perspective-card teacher-card\">");
        sb.AppendLine("              <div class=\"persp-header\"><span>🌱</span> Teacher's Spark</div>");
        sb.AppendLine("              <p id=\"teacherSpark\">" + HttpUtility.HtmlEncode(chapters[0].TeacherSpark) + "</p>");
        sb.AppendLine("            </div>");
        sb.AppendLine("          </div>");

        // Wonder Box
        sb.AppendLine("          <div class=\"wonder-box\">");
        sb.AppendLine("            <span style=\"font-size: 20px;\">💡</span>");
        sb.AppendLine("            <div>");
        sb.AppendLine("              <strong>Spark of Wonder: </strong>");
        sb.AppendLine("              <span id=\"wonderPrompt\">" + HttpUtility.HtmlEncode(chapters[0].WonderPrompt) + "</span>");
        sb.AppendLine("            </div>");
        sb.AppendLine("          </div>");

        // Stage Bottom Nav
        sb.AppendLine("          <div class=\"stage-bottom\">");
        sb.AppendLine("            <button class=\"btn secondary\" id=\"prevBtn\" onclick=\"prevChapter()\" disabled>← Previous Chapter</button>");
        sb.AppendLine("            <button class=\"btn primary\" id=\"nextBtn\" onclick=\"nextChapter()\">Next Chapter →</button>");
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
    let isSpeaking = false;
    let speechRate = 1.0;

    function goToChapter(index) {
      if (index < 0 || index >= chapters.length) return;
      currentIndex = index;
      renderCurrentChapter();
      if (isSpeaking) {
        speakCurrent();
      }
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
      document.getElementById('chapterPill').innerText = `Chapter ${currentIndex + 1} of ${chapters.length} • ${ch.phaseTag}`;
      document.getElementById('chapterDuration').innerText = `⏱️ ${ch.duration}`;
      document.getElementById('chapterTitle').innerText = ch.title;
      document.getElementById('chapterNarrative').innerText = `“${ch.narrative}”`;
      document.getElementById('childAction').innerText = ch.childAction;
      document.getElementById('teacherSpark').innerText = ch.teacherSpark;
      document.getElementById('wonderPrompt').innerText = ch.wonderPrompt;

      // Update Artwork
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
        nextBtn.innerText = '✨ Replay Story';
        nextBtn.onclick = () => goToChapter(0);
      } else {
        nextBtn.innerText = `Next: ${chapters[currentIndex + 1].title} →`;
        nextBtn.onclick = nextChapter;
      }
    }

    // Audio narration via Web Speech API
    function toggleNarration() {
      if (isSpeaking) {
        stopNarration();
      } else {
        isSpeaking = true;
        speakCurrent();
      }
    }

    function speakCurrent() {
      if (!('speechSynthesis' in window)) {
        alert('Speech narration is not supported in this browser.');
        return;
      }
      window.speechSynthesis.cancel();
      const ch = chapters[currentIndex];
      const text = `${ch.title}. ${ch.narrative}. For the children: ${ch.childAction}. Teacher guidance: ${ch.teacherSpark}. Spark of wonder: ${ch.wonderPrompt}`;
      const utterance = new SpeechSynthesisUtterance(text);
      utterance.rate = speechRate;
      utterance.pitch = 1.05;

      const voices = window.speechSynthesis.getVoices();
      const warmVoice = voices.find(v => v.lang.startsWith('en') && (v.name.includes('Natural') || v.name.includes('Samantha') || v.name.includes('Google') || v.name.includes('Karen')));
      if (warmVoice) utterance.voice = warmVoice;

      utterance.onend = () => {
        const autoAdvance = document.getElementById('autoAdvanceCheck').checked;
        if (autoAdvance && currentIndex < chapters.length - 1) {
          setTimeout(() => {
            goToChapter(currentIndex + 1);
          }, 1200);
        } else {
          stopNarration();
        }
      };

      utterance.onerror = () => {
        stopNarration();
      };

      window.speechSynthesis.speak(utterance);
      updateAudioUi(true);
    }

    function stopNarration() {
      if ('speechSynthesis' in window) {
        window.speechSynthesis.cancel();
      }
      isSpeaking = false;
      updateAudioUi(false);
    }

    function updateAudioUi(speaking) {
      document.getElementById('playMainBtn').innerText = speaking ? '⏸' : '▶';
      document.getElementById('playIconTop').innerText = speaking ? '⏸' : '▶';
      document.getElementById('playTextTop').innerText = speaking ? 'Pause Narration' : 'Listen to Story';
      document.getElementById('narratorStatus').innerText = speaking ? 'Narrating Chapter ' + (currentIndex + 1) + '...' : 'Ready to play';
    }

    function setRate(rate, btn) {
      speechRate = rate;
      document.querySelectorAll('.speed-btn').forEach(b => b.classList.remove('active'));
      btn.classList.add('active');
      if (isSpeaking) {
        speakCurrent();
      }
    }

    // Keyboard navigation
    window.addEventListener('keydown', (e) => {
      if (e.key === 'ArrowRight') nextChapter();
      if (e.key === 'ArrowLeft') prevChapter();
      if (e.key === ' ') {
        e.preventDefault();
        toggleNarration();
      }
    });

    renderCurrentChapter();
  </script>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    public static string GetSvgForKey(string key)
    {
        return key switch
        {
            "gathering" => @"
<svg viewBox=""0 0 800 320"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"" style=""width:100%;height:100%;"">
  <defs>
    <linearGradient id=""skyWarm"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""100%"">
      <stop offset=""0%"" stop-color=""#fdf8f0""/>
      <stop offset=""100%"" stop-color=""#eef5f1""/>
    </linearGradient>
    <radialGradient id=""sunRays"" cx=""50%"" cy=""20%"" r=""60%"">
      <stop offset=""0%"" stop-color=""rgba(244, 216, 154, 0.45)""/>
      <stop offset=""100%"" stop-color=""rgba(244, 216, 154, 0)""/>
    </radialGradient>
  </defs>
  <rect width=""800"" height=""320"" fill=""url(#skyWarm)""/>
  <circle cx=""400"" cy=""70"" r=""180"" fill=""url(#sunRays)""/>
  
  <!-- Arched Daycare Window -->
  <path d=""M 320 220 L 320 110 C 320 65, 480 65, 480 110 L 480 220 Z"" fill=""#ffffff"" stroke=""#d9e2db"" stroke-width=""5""/>
  <line x1=""400"" y1=""70"" x2=""400"" y2=""220"" stroke=""#d9e2db"" stroke-width=""3""/>
  <line x1=""320"" y1=""140"" x2=""480"" y2=""140"" stroke=""#d9e2db"" stroke-width=""3""/>
  
  <!-- Gentle Sun beams -->
  <polygon points=""330,220 250,300 550,300 470,220"" fill=""rgba(255,255,255,0.4)""/>

  <!-- Floorboards -->
  <line x1=""0"" y1=""240"" x2=""800"" y2=""240"" stroke=""#e0e6e1"" stroke-width=""2""/>
  <line x1=""100"" y1=""240"" x2=""50"" y2=""320"" stroke=""#e8ede8"" stroke-width=""1.5""/>
  <line x1=""260"" y1=""240"" x2=""220"" y2=""320"" stroke=""#e8ede8"" stroke-width=""1.5""/>
  <line x1=""540"" y1=""240"" x2=""580"" y2=""320"" stroke=""#e8ede8"" stroke-width=""1.5""/>
  <line x1=""700"" y1=""240"" x2=""750"" y2=""320"" stroke=""#e8ede8"" stroke-width=""1.5""/>

  <!-- Cozy Round Rug -->
  <ellipse cx=""400"" cy=""265"" rx=""240"" ry=""42"" fill=""#e6eee9"" stroke=""#557c6c"" stroke-width=""3"" stroke-dasharray=""6 4""/>
  <ellipse cx=""400"" cy=""265"" rx=""190"" ry=""32"" fill=""#f5efe3""/>

  <!-- Hanging Potted Plant (Left) -->
  <line x1=""200"" y1=""0"" x2=""200"" y2=""90"" stroke=""#7c877f"" stroke-width=""2""/>
  <path d=""M 185 90 L 215 90 L 208 115 L 192 115 Z"" fill=""#d9826c""/>
  <path d=""M 200 90 Q 170 120 180 140 Q 195 120 200 90"" fill=""#557c6c""/>
  <path d=""M 200 90 Q 230 115 220 145 Q 205 125 200 90"" fill=""#3e6253""/>
  <path d=""M 200 90 Q 200 135 205 155 Q 210 130 200 90"" fill=""#78998a""/>

  <!-- Potted Fiddle Leaf (Right) -->
  <path d=""M 605 240 L 635 240 L 630 268 L 610 268 Z"" fill=""#df9b3a""/>
  <path d=""M 620 240 Q 610 180 625 150"" stroke=""#3e6253"" stroke-width=""4"" fill=""none""/>
  <ellipse cx=""615"" cy=""175"" rx=""18"" ry=""10"" fill=""#557c6c"" transform=""rotate(-25 615 175)""/>
  <ellipse cx=""635"" cy=""155"" rx=""20"" ry=""12"" fill=""#3e6253"" transform=""rotate(20 635 155)""/>
  <ellipse cx=""620"" cy=""140"" rx=""16"" ry=""10"" fill=""#78998a""/>

  <!-- Explorer Cushions -->
  <ellipse cx=""280"" cy=""268"" rx=""35"" ry=""16"" fill=""#f4d89a"" stroke=""#df9b3a"" stroke-width=""2""/>
  <ellipse cx=""520"" cy=""268"" rx=""35"" ry=""16"" fill=""#f2cdc3"" stroke=""#d9826c"" stroke-width=""2""/>
  <ellipse cx=""400"" cy=""282"" rx=""42"" ry=""18"" fill=""#d5e5db"" stroke=""#557c6c"" stroke-width=""2""/>

  <!-- Mystery Basket with Curiosity Items -->
  <path d=""M 380 252 L 420 252 L 415 268 L 385 268 Z"" fill=""#a87d55""/>
  <ellipse cx=""400"" cy=""252"" rx=""20"" ry=""5"" fill=""#c99c72""/>
  <!-- Sparkles -->
  <polygon points=""400,230 404,240 415,242 406,248 408,258 400,252 392,258 394,248 385,242 396,240"" fill=""#e9b65e""/>
  <circle cx=""430"" cy=""235"" r=""3"" fill=""#e9b65e""/>
  <circle cx=""370"" cy=""238"" r=""2.5"" fill=""#e9b65e""/>
</svg>",

            "challenge" => @"
<svg viewBox=""0 0 800 320"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"" style=""width:100%;height:100%;"">
  <rect width=""800"" height=""320"" fill=""#f9f7f2""/>
  
  <!-- Soft Background circles -->
  <circle cx=""400"" cy=""160"" r=""140"" fill=""#edf4f0""/>
  <circle cx=""400"" cy=""160"" r=""90"" fill=""#fdfbf7""/>

  <!-- Investigation Table -->
  <rect x=""220"" y=""200"" width=""360"" height=""20"" rx=""6"" fill=""#d8caa8"" stroke=""#ab9c76"" stroke-width=""2""/>
  <rect x=""250"" y=""220"" width=""18"" height=""75"" fill=""#ab9c76""/>
  <rect x=""532"" y=""220"" width=""18"" height=""75"" fill=""#ab9c76""/>
  <line x1=""220"" y1=""295"" x2=""580"" y2=""295"" stroke=""#e0e6e1"" stroke-width=""3""/>

  <!-- Mystery Box on Table -->
  <rect x=""340"" y=""135"" width=""120"" height=""65"" rx=""8"" fill=""#557c6c""/>
  <rect x=""335"" y=""130"" width=""130"" height=""15"" rx=""4"" fill=""#3e6253""/>
  <!-- Amber Ribbon -->
  <rect x=""393"" y=""130"" width=""14"" height=""70"" fill=""#e9b65e""/>
  <!-- Bow -->
  <ellipse cx=""390"" cy=""124"" rx=""10"" ry=""6"" fill=""#e9b65e"" transform=""rotate(-20 390 124)""/>
  <ellipse cx=""410"" cy=""124"" rx=""10"" ry=""6"" fill=""#e9b65e"" transform=""rotate(20 410 124)""/>
  <circle cx=""400"" cy=""125"" r=""4"" fill=""#df9b3a""/>

  <!-- Magnifying Glass Leaning -->
  <circle cx=""290"" cy=""165"" r=""28"" stroke=""#df9b3a"" stroke-width=""6"" fill=""rgba(230, 240, 235, 0.6)""/>
  <line x1=""310"" y1=""185"" x2=""340"" y2=""210"" stroke=""#a87d55"" stroke-width=""8"" stroke-linecap=""round""/>
  <!-- Glass Shine -->
  <path d=""M 275 155 Q 290 145 305 155"" stroke=""white"" stroke-width=""3"" stroke-linecap=""round"" fill=""none""/>

  <!-- Nature Samples on Table -->
  <!-- Pinecone -->
  <path d=""M 495 185 C 485 175, 485 160, 495 150 C 505 160, 505 175, 495 185 Z"" fill=""#855e39""/>
  <ellipse cx=""495"" cy=""160"" rx=""8"" ry=""4"" fill=""#6b4928""/>
  <ellipse cx=""495"" cy=""170"" rx=""9"" ry=""4"" fill=""#6b4928""/>
  <!-- Smooth river stone -->
  <ellipse cx=""525"" cy=""192"" rx=""18"" ry=""9"" fill=""#9ba8a0""/>

  <!-- Floating Question / Curiosity Sparkles -->
  <text x=""255"" y=""120"" font-size=""32"" font-family=""sans-serif"" font-weight=""800"" fill=""#557c6c"">?</text>
  <text x=""530"" y=""115"" font-size=""28"" font-family=""sans-serif"" font-weight=""800"" fill=""#df9b3a"">?</text>
  <polygon points=""305,95 308,103 316,105 309,110 311,118 305,113 299,118 301,110 294,105 302,103"" fill=""#e9b65e""/>
  <polygon points=""495,85 497,92 504,94 498,99 500,106 495,101 490,106 492,99 486,94 493,92"" fill=""#557c6c""/>
</svg>",

            "explore" => @"
<svg viewBox=""0 0 800 320"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"" style=""width:100%;height:100%;"">
  <rect width=""800"" height=""320"" fill=""#f5f8f6""/>
  
  <!-- Circular Creative Halo -->
  <ellipse cx=""400"" cy=""170"" rx=""280"" ry=""110"" fill=""#ffffff"" stroke=""#e1e9e3"" stroke-width=""2""/>

  <!-- Hands-On Building Blocks Tower (Center Left) -->
  <rect x=""310"" y=""190"" width=""55"" height=""28"" rx=""4"" fill=""#557c6c""/>
  <rect x=""370"" y=""190"" width=""55"" height=""28"" rx=""4"" fill=""#e9b65e""/>
  <rect x=""330"" y=""160"" width=""75"" height=""28"" rx=""4"" fill=""#d9826c""/>
  <polygon points=""367,115 330,158 405,158"" fill=""#3e6253""/>

  <!-- Sculpting Clay Pinch Pot (Center Right) -->
  <ellipse cx=""470"" cy=""195"" rx=""32"" ry=""14"" fill=""#c9775f""/>
  <path d=""M 438 195 Q 438 215 470 215 Q 502 215 502 195 Z"" fill=""#b3654f""/>
  <!-- Twig planted inside clay -->
  <line x1=""470"" y1=""195"" x2=""480"" y2=""145"" stroke=""#855e39"" stroke-width=""4"" stroke-linecap=""round""/>
  <circle cx=""480"" cy=""142"" r=""6"" fill=""#557c6c""/>
  <circle cx=""487"" cy=""152"" r=""5"" fill=""#78998a""/>

  <!-- Children Handprints Motif -->
  <path d=""M 230 160 C 225 150 220 152 222 165 C 220 148 212 150 216 168 C 210 152 202 156 208 172 C 205 162 195 166 202 180 C 208 192 228 195 235 185 Z"" fill=""#f4d89a"" opacity=""0.8""/>
  <path d=""M 570 160 C 575 150 580 152 578 165 C 580 148 588 150 584 168 C 590 152 598 156 592 172 C 595 162 605 166 598 180 C 592 192 572 195 565 185 Z"" fill=""#e5eee8"" stroke=""#557c6c"" stroke-width=""1.5""/>

  <!-- Watercolor artist palette -->
  <path d=""M 220 225 C 220 205 270 205 270 225 C 270 245 220 245 220 225 Z"" fill=""#faf5ec"" stroke=""#ab9c76"" stroke-width=""2""/>
  <circle cx=""235"" cy=""220"" r=""5"" fill=""#557c6c""/>
  <circle cx=""250"" cy=""218"" r=""5"" fill=""#df9b3a""/>
  <circle cx=""242"" cy=""230"" r=""5"" fill=""#d9826c""/>

  <!-- Floating inspiration particles -->
  <circle cx=""290"" cy=""130"" r=""4"" fill=""#e9b65e""/>
  <circle cx=""515"" cy=""125"" r=""5"" fill=""#557c6c""/>
  <circle cx=""405"" cy=""95"" r=""3"" fill=""#d9826c""/>
</svg>",

            "magic" => @"
<svg viewBox=""0 0 800 320"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"" style=""width:100%;height:100%;"">
  <defs>
    <radialGradient id=""glowLight"" cx=""50%"" cy=""50%"" r=""50%"">
      <stop offset=""0%"" stop-color=""#fff9e6""/>
      <stop offset=""60%"" stop-color=""#eef7f2""/>
      <stop offset=""100%"" stop-color=""#f8faf8""/>
    </radialGradient>
    <linearGradient id=""rainbowBeam"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""100%"">
      <stop offset=""0%"" stop-color=""rgba(217, 130, 108, 0.7)""/>
      <stop offset=""35%"" stop-color=""rgba(233, 182, 94, 0.7)""/>
      <stop offset=""70%"" stop-color=""rgba(85, 124, 108, 0.7)""/>
      <stop offset=""100%"" stop-color=""rgba(62, 98, 83, 0.7)""/>
    </linearGradient>
  </defs>
  <rect width=""800"" height=""320"" fill=""url(#glowLight)""/>

  <!-- Luminous Starburst Rings -->
  <circle cx=""400"" cy=""155"" r=""115"" stroke=""#e6eee9"" stroke-width=""3"" stroke-dasharray=""6 6""/>
  <circle cx=""400"" cy=""155"" r=""80"" stroke=""#f4d89a"" stroke-width=""2"" stroke-dasharray=""4 4""/>

  <!-- Refracting Crystal Prism -->
  <polygon points=""400,90 350,185 450,185"" fill=""rgba(255,255,255,0.85)"" stroke=""#557c6c"" stroke-width=""4""/>
  <polygon points=""400,90 400,185 450,185"" fill=""rgba(230,238,233,0.5)""/>

  <!-- Radiant Light Rays emanating -->
  <polygon points=""400,140 580,80 620,110"" fill=""url(#rainbowBeam)""/>

  <!-- Giant Golden Star Sparkle -->
  <polygon points=""400,60 408,78 428,82 413,95 417,115 400,105 383,115 387,95 372,82 392,78"" fill=""#e9b65e""/>
  <circle cx=""400"" cy=""87"" r=""4"" fill=""white""/>

  <!-- Joyful Dancing Sparkles around -->
  <polygon points=""260,110 263,118 271,120 264,125 266,133 260,128 254,133 256,125 249,120 257,118"" fill=""#df9b3a""/>
  <polygon points=""540,180 543,188 551,190 544,195 546,203 540,198 534,203 536,195 529,190 537,188"" fill=""#557c6c""/>
  <polygon points=""310,210 312,216 318,217 313,221 315,227 310,223 305,227 307,221 302,217 308,216"" fill=""#d9826c""/>
  <polygon points=""480,80 482,86 488,87 483,91 485,97 480,93 475,97 477,91 472,87 478,86"" fill=""#e9b65e""/>

  <!-- Shared Eureka Pedestal -->
  <ellipse cx=""400"" cy=""225"" rx=""90"" ry=""16"" fill=""#e2ebe4"" stroke=""#557c6c"" stroke-width=""2""/>
  <text x=""400"" y=""270"" text-anchor=""middle"" font-size=""16"" font-family=""sans-serif"" font-weight=""800"" fill=""#3e6253"" letter-spacing=""2"">A MOMENT OF PURE DISCOVERY</text>
</svg>",

            "reflection" or _ => @"
<svg viewBox=""0 0 800 320"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"" style=""width:100%;height:100%;"">
  <rect width=""800"" height=""320"" fill=""#fbf9f4""/>

  <!-- Celebration Festoon Garland Bunting -->
  <path d=""M 150 40 Q 275 80 400 50 Q 525 80 650 40"" stroke=""#7c877f"" stroke-width=""2"" fill=""none""/>
  <!-- Pennants -->
  <polygon points=""200,53 220,57 210,85"" fill=""#557c6c""/>
  <polygon points=""260,63 280,63 270,95"" fill=""#e9b65e""/>
  <polygon points=""330,59 350,56 340,90"" fill=""#d9826c""/>
  <polygon points=""440,56 460,59 450,90"" fill=""#3e6253""/>
  <polygon points=""510,63 530,63 520,95"" fill=""#df9b3a""/>
  <polygon points=""580,55 600,50 590,82"" fill=""#78998a""/>

  <!-- Proud Gallery Display Shelf -->
  <rect x=""180"" y=""190"" width=""440"" height=""14"" rx=""4"" fill=""#ab9c76""/>
  
  <!-- Masterpiece Sculptures Displayed on Shelf -->
  <!-- Child sculpture 1 -->
  <rect x=""230"" y=""150"" width=""35"" height=""40"" rx=""6"" fill=""#557c6c""/>
  <circle cx=""247"" cy=""140"" r=""12"" fill=""#e9b65e""/>
  
  <!-- Child sculpture 2 (Centerpiece with Heart medal) -->
  <polygon points=""400,125 425,188 375,188"" fill=""#d9826c""/>
  <circle cx=""400"" cy=""155"" r=""14"" fill=""#ffffff""/>
  <path d=""M 395 153 C 395 149 400 148 400 152 C 400 148 405 149 405 153 C 405 158 400 161 400 161 C 400 161 395 158 395 153 Z"" fill=""#d9826c""/>

  <!-- Child sculpture 3 -->
  <rect x=""530"" y=""155"" width=""45"" height=""35"" rx=""6"" fill=""#3e6253""/>
  <ellipse cx=""552"" cy=""145"" rx=""16"" ry=""8"" fill=""#f4d89a""/>

  <!-- Little Stars of Accomplishment -->
  <polygon points=""210,105 213,112 220,113 214,118 216,125 210,121 204,125 206,118 200,113 207,112"" fill=""#e9b65e""/>
  <polygon points=""590,105 593,112 600,113 594,118 596,125 590,121 584,125 586,118 580,113 587,112"" fill=""#e9b65e""/>
  <polygon points=""400,90 404,99 414,101 406,108 408,118 400,113 392,118 394,108 386,101 396,99"" fill=""#e9b65e""/>

  <!-- Closing Reflection Circle Rug -->
  <ellipse cx=""400"" cy=""260"" rx=""260"" ry=""42"" fill=""#eef5f1"" stroke=""#557c6c"" stroke-width=""2""/>
  <text x=""400"" y=""266"" text-anchor=""middle"" font-size=""15"" font-family=""sans-serif"" font-weight=""700"" fill=""#26332d"">Stories Shared · Hearts Full · Memories Made</text>
</svg>"
        };
    }
}

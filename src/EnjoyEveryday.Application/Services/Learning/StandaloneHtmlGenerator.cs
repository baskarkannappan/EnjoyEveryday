using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Entities.Learning;

namespace EnjoyEveryday.Application.Services.Learning;

public static partial class StandaloneHtmlGenerator
{
    public static string GenerateStorybookHtml(Experience exp, ExperienceDna dna)
    {
        return GenerateStorybookHtml(exp, dna, false, null, exp?.Id);
    }

    public static string GenerateStorybookHtml(
        Experience exp, 
        ExperienceDna dna, 
        bool hasGridImage, 
        string? geminiPrompt, 
        Guid? experienceId,
        string? gridImageBase64 = null)
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
            versionText,
            hasGridImage,
            geminiPrompt,
            experienceId ?? exp.Id,
            gridImageBase64
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
        List<StoryChapter> chapters,
        string versionInfo = "",
        bool hasGridImage = false,
        string? geminiPrompt = null,
        Guid? experienceId = null,
        string? gridImageBase64 = null)
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
      --accent-dark: #31523c;
      --warm: #f4eee1;
      --shadow: 0 8px 30px rgba(40, 55, 47, .06);
      --radius: 18px;
    }

    * { box-sizing: border-box; margin: 0; padding: 0; }
    body {
      font-family: ""Roboto"", -apple-system, BlinkMacSystemFont, ""Segoe UI"", ""Helvetica Neue"", Arial, sans-serif;
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

    /* Gemini AI Storyboard Studio Toolbar */
    .ai-toolbar {
      background: var(--surface);
      border: 1px solid var(--border);
      border-radius: var(--radius);
      padding: 12px 18px;
      margin-bottom: 22px;
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 14px;
      flex-wrap: wrap;
      box-shadow: var(--shadow);
    }
    .ai-toolbar-left {
      display: flex;
      align-items: center;
      gap: 10px;
      flex-wrap: wrap;
    }
    .ai-toolbar-title {
      font-weight: 750;
      font-size: 13.5px;
      color: var(--text);
      display: flex;
      align-items: center;
      gap: 6px;
    }
    .ai-toolbar-tag {
      background: var(--accent-light);
      color: var(--accent-dark);
      padding: 2px 8px;
      border-radius: 999px;
      font-size: 11px;
      font-weight: 750;
    }
    .ai-toolbar-actions {
      display: flex;
      align-items: center;
      gap: 8px;
      flex-wrap: wrap;
    }
    .btn-ai {
      background: linear-gradient(135deg, var(--accent), var(--accent-dark));
      color: white;
      border: none;
      padding: 8px 14px;
      border-radius: 10px;
      font-size: 12.5px;
      font-weight: 700;
      cursor: pointer;
      display: inline-flex;
      align-items: center;
      gap: 6px;
      transition: all 0.2s ease;
      text-decoration: none;
    }
    .btn-ai:hover {
      transform: translateY(-1px);
      box-shadow: 0 4px 12px rgba(62, 98, 83, 0.25);
    }
    .btn-ai-outline {
      background: white;
      color: var(--text);
      border: 1px solid var(--border);
      padding: 8px 14px;
      border-radius: 10px;
      font-size: 12.5px;
      font-weight: 700;
      cursor: pointer;
      display: inline-flex;
      align-items: center;
      gap: 6px;
      text-decoration: none;
    }
    .btn-ai-outline:hover {
      background: var(--surface-soft);
      border-color: #cbd3c9;
    }

    /* 6-Panel Illustrated Grid Showcase */
    .grid-showcase-card {
      background: var(--surface);
      border: 1px solid var(--border);
      border-radius: var(--radius);
      padding: 20px;
      margin-bottom: 24px;
      box-shadow: var(--shadow);
    }
    .grid-showcase-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 14px;
      flex-wrap: wrap;
      gap: 10px;
    }
    .grid-showcase-title {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 15px;
      font-weight: 800;
      color: var(--text);
    }
    .grid-image-frame {
      border-radius: 14px;
      overflow: hidden;
      border: 1px solid var(--border);
      background: #fafbfa;
      box-shadow: 0 4px 20px rgba(0,0,0,0.06);
    }
    .storybook-grid-img {
      width: 100%;
      height: auto;
      display: block;
      border-radius: 13px;
    }
    .toast-popup {
      position: fixed;
      bottom: 24px;
      right: 24px;
      background: var(--text);
      color: white;
      padding: 12px 20px;
      border-radius: 12px;
      font-size: 13.5px;
      font-weight: 600;
      box-shadow: 0 8px 30px rgba(0,0,0,0.25);
      z-index: 9999;
      opacity: 0;
      transform: translateY(10px);
      transition: all 0.25s ease;
      pointer-events: none;
    }
    .toast-popup.visible {
      opacity: 1;
      transform: translateY(0);
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

    /* Full-Screen Lightbox Modal */
    .lightbox-modal {
      position: fixed;
      top: 0;
      left: 0;
      width: 100vw;
      height: 100vh;
      background: rgba(20, 30, 25, 0.92);
      backdrop-filter: blur(6px);
      -webkit-backdrop-filter: blur(6px);
      z-index: 10000;
      display: none;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 20px;
      box-sizing: border-box;
    }
    .lightbox-modal.active {
      display: flex;
    }
    .lightbox-bar {
      width: 100%;
      max-width: 1300px;
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 12px;
      color: white;
    }
    .lightbox-title {
      font-size: 15px;
      font-weight: 750;
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .lightbox-actions {
      display: flex;
      align-items: center;
      gap: 10px;
    }
    .lightbox-close-btn {
      background: rgba(255, 255, 255, 0.15);
      border: 1px solid rgba(255, 255, 255, 0.3);
      color: white;
      font-size: 16px;
      font-weight: 700;
      width: 36px;
      height: 36px;
      border-radius: 50%;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: background 0.2s;
    }
    .lightbox-close-btn:hover {
      background: rgba(255, 255, 255, 0.3);
    }
    .lightbox-content {
      width: 100%;
      max-width: 1300px;
      max-height: calc(100vh - 100px);
      display: flex;
      align-items: center;
      justify-content: center;
    }
    .lightbox-image {
      max-width: 100%;
      max-height: calc(100vh - 110px);
      object-fit: contain;
      border-radius: 12px;
      box-shadow: 0 16px 50px rgba(0, 0, 0, 0.6);
    }

    /* Print-Only Steps Container (Hidden on Screen) */
    .print-only-steps {
      display: none;
    }

    /* Print View */
    @media print {
      @page {
        size: auto;
        margin: 12mm 15mm;
      }
      html, body {
        background: white !important;
        padding: 0 !important;
        margin: 0 !important;
        color: #1a221e !important;
        font-size: 11pt !important;
      }
      .container {
        max-width: 100% !important;
        padding: 0 !important;
        margin: 0 !important;
      }
      .hero {
        margin-bottom: 16px !important;
        padding-bottom: 12px !important;
        border-bottom: 1px solid #d4ded7 !important;
      }
      .hero-actions,
      .ai-toolbar,
      .view-toggle-bar,
      .stage-bottom,
      .dots-nav,
      .btn-ai,
      .btn-ai-outline,
      .toast-popup,
      .lightbox-modal,
      .grid-header-actions {
        display: none !important;
      }

      /* Hide interactive single-stage view, show all print steps */
      .stage {
        display: none !important;
      }
      .print-only-steps {
        display: block !important;
      }

      .split-layout {
        display: block !important;
      }

      /* When 6-panel grid is present, grid card is on Page 1, sidebar on Page 2 */
      .has-grid-image .grid-showcase-card {
        box-shadow: none !important;
        border: 1px solid #d4ded7 !important;
        margin: 0 0 20px 0 !important;
        padding: 14px !important;
        page-break-inside: avoid !important;
        break-inside: avoid !important;
        page-break-after: always !important;
        break-after: page !important;
      }
      .has-grid-image .sidebar {
        page-break-before: always !important;
        break-before: page !important;
      }

      .grid-image-frame {
        box-shadow: none !important;
        border: none !important;
        overflow: visible !important;
        background: transparent !important;
      }
      .storybook-grid-img {
        max-height: 480px !important;
        width: auto !important;
        max-width: 100% !important;
        margin: 0 auto !important;
        display: block !important;
        object-fit: contain !important;
        page-break-inside: avoid !important;
        break-inside: avoid !important;
      }

      .sidebar {
        display: block !important;
        page-break-inside: avoid !important;
        break-inside: avoid !important;
        margin-bottom: 24px !important;
      }
      .sidebar .card {
        box-shadow: none !important;
        border: 1px solid #d4ded7 !important;
        page-break-inside: avoid !important;
        break-inside: avoid !important;
        margin-bottom: 14px !important;
        padding: 14px 18px !important;
        background: white !important;
      }

      /* Each step gets its own clean printed page */
      .print-step-card {
        page-break-before: always !important;
        break-before: page !important;
        page-break-inside: avoid !important;
        break-inside: avoid !important;
        margin-bottom: 24px !important;
        border: 1px solid #d4ded7 !important;
        border-radius: 14px !important;
        padding: 18px 20px !important;
        background: white !important;
      }
      .diy-step-grid {
        display: grid !important;
        grid-template-columns: 320px 1fr !important;
        gap: 18px !important;
        page-break-inside: avoid !important;
        break-inside: avoid !important;
      }
      .diagram-canvas {
        height: 220px !important;
      }
    }
  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine($"<body class=\"{(hasGridImage || !string.IsNullOrEmpty(gridImageBase64) ? "has-grid-image" : "")}\">");

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

        // AI Storyboard Studio Toolbar
        sb.AppendLine("    <div class=\"ai-toolbar\">");
        sb.AppendLine("      <div class=\"ai-toolbar-left\">");
        sb.AppendLine("        <div class=\"ai-toolbar-title\">");
        sb.AppendLine("          <span>✨</span>");
        sb.AppendLine("          <span>Gemini 3D Storyboard Studio</span>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <span class=\"ai-toolbar-tag\">100% Free Web Flow</span>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div class=\"ai-toolbar-actions\">");
        sb.AppendLine("        <button class=\"btn-ai\" onclick=\"copyGeminiPrompt()\">📋 Copy Prompt for Gemini</button>");
        sb.AppendLine("        <a href=\"https://gemini.google.com\" target=\"_blank\" class=\"btn-ai-outline\">↗ Open Gemini Web</a>");
        sb.AppendLine("        <button class=\"btn-ai-outline\" onclick=\"document.getElementById('imgUploadInput').click()\">📷 Upload 6-Panel Image</button>");
        sb.AppendLine("        <input type=\"file\" id=\"imgUploadInput\" accept=\"image/*\" style=\"display: none;\" onchange=\"uploadStoryboardImage(this)\" />");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");

        // Illustrated Grid Showcase (if uploaded)
        if ((hasGridImage || !string.IsNullOrEmpty(gridImageBase64)) && experienceId.HasValue)
        {
            var imgSrc = !string.IsNullOrEmpty(gridImageBase64)
                ? gridImageBase64
                : $"/api/experiences/{experienceId.Value}/storybook/image";

            sb.AppendLine("    <div class=\"grid-showcase-card\">");
            sb.AppendLine("      <div class=\"grid-showcase-header\">");
            sb.AppendLine("        <div class=\"grid-showcase-title\">");
            sb.AppendLine("          <span>🎨</span>");
            sb.AppendLine("          <span>6-Page Illustrated Storybook (Pixar-Inspired 3D Scene)</span>");
            sb.AppendLine("        </div>");
            sb.AppendLine("        <div style=\"display: flex; gap: 8px;\" class=\"grid-header-actions\">");
            sb.AppendLine("          <button type=\"button\" class=\"btn-ai-outline\" onclick=\"openFullSizeImage()\">🔍 View Full Size</button>");
            sb.AppendLine("          <button type=\"button\" class=\"btn-ai-outline\" onclick=\"document.getElementById('imgUploadInput').click()\">🔄 Replace Image</button>");
            sb.AppendLine("        </div>");
            sb.AppendLine("      </div>");
            sb.AppendLine("      <div class=\"grid-image-frame\">");
            sb.AppendLine($"        <img src=\"{imgSrc}\" alt=\"6-Panel Storybook Grid\" class=\"storybook-grid-img\" style=\"cursor: pointer;\" onclick=\"openFullSizeImage()\" title=\"Click to view full size\" />");
            sb.AppendLine("      </div>");
            sb.AppendLine("    </div>");

            // Lightbox Modal for Full Size Viewing
            sb.AppendLine("    <div id=\"lightboxModal\" class=\"lightbox-modal\" onclick=\"if(event.target===this)closeLightbox()\">");
            sb.AppendLine("      <div class=\"lightbox-bar\">");
            sb.AppendLine("        <div class=\"lightbox-title\"><span>🎨</span> 6-Page Illustrated Storybook (Full Size)</div>");
            sb.AppendLine("        <div class=\"lightbox-actions\">");
            sb.AppendLine("          <button type=\"button\" class=\"btn-ai-outline\" style=\"background:rgba(255,255,255,0.12); color:white; border-color:rgba(255,255,255,0.3);\" onclick=\"openImageInNewWindow()\">↗ Open in New Tab</button>");
            sb.AppendLine("          <button type=\"button\" class=\"btn-ai-outline\" style=\"background:rgba(255,255,255,0.12); color:white; border-color:rgba(255,255,255,0.3);\" onclick=\"downloadStorybookImage()\">⬇ Download Image</button>");
            sb.AppendLine("          <button type=\"button\" class=\"lightbox-close-btn\" onclick=\"closeLightbox()\" title=\"Close\">✕</button>");
            sb.AppendLine("        </div>");
            sb.AppendLine("      </div>");
            sb.AppendLine("      <div class=\"lightbox-content\" onclick=\"if(event.target===this)closeLightbox()\">");
            sb.AppendLine($"        <img src=\"{imgSrc}\" alt=\"6-Panel Storybook Grid Full Size\" class=\"lightbox-image\" />");
            sb.AppendLine("      </div>");
            sb.AppendLine("    </div>");
        }

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

        // Print-Only Steps: Render ALL chapters so printing includes the complete manual!
        sb.AppendLine("        <div class=\"print-only-steps\">");
        for (int i = 0; i < chapters.Count; i++)
        {
            var ch = chapters[i];
            sb.AppendLine("          <div class=\"print-step-card\">");
            sb.AppendLine("            <div class=\"stage-top\" style=\"margin-bottom: 14px;\">");
            sb.AppendLine("              <div class=\"scene-meta-row\">");
            sb.AppendLine($"                <span class=\"scene-tag\">Step {i + 1} of {chapters.Count} • {HttpUtility.HtmlEncode(ch.PhaseTag)}</span>");
            sb.AppendLine($"                <span style=\"font-size: 12px; color: var(--muted);\">⏱️ {HttpUtility.HtmlEncode(ch.Duration)}</span>");
            sb.AppendLine("              </div>");
            sb.AppendLine("            </div>");
            sb.AppendLine("            <div class=\"diy-step-grid\">");
            sb.AppendLine("              <div class=\"diy-diagram-box\">");
            sb.AppendLine("                <div class=\"diagram-header\">");
            sb.AppendLine("                  <span>🎨 Visual Blueprint Scene</span>");
            sb.AppendLine($"                  <span>STEP {i + 1}</span>");
            sb.AppendLine("                </div>");
            sb.AppendLine("                <div class=\"diagram-canvas\">");
            sb.AppendLine(GetSvgForKey(ch.SvgSceneKey));
            sb.AppendLine("                </div>");
            sb.AppendLine("              </div>");
            sb.AppendLine("              <div class=\"diy-info-box\">");
            sb.AppendLine($"                <h2 class=\"scene-heading\">{HttpUtility.HtmlEncode(ch.Title)}</h2>");
            sb.AppendLine("                <div class=\"instruction-callout\">");
            sb.AppendLine("                  <h5>Pedagogical Story Flow</h5>");
            sb.AppendLine($"                  <p>{HttpUtility.HtmlEncode(ch.Narrative)}</p>");
            sb.AppendLine("                </div>");
            if (!string.IsNullOrWhiteSpace(ch.TeacherScript))
            {
                sb.AppendLine("                <div class=\"teacher-script-callout\">");
                sb.AppendLine("                  <div class=\"script-header\">");
                sb.AppendLine("                    <span>🗣️</span>");
                sb.AppendLine("                    <strong>Teacher's Spoken Script (Say Aloud)</strong>");
                sb.AppendLine("                  </div>");
                sb.AppendLine($"                  <p class=\"script-text\">{HttpUtility.HtmlEncode(ch.TeacherScript)}</p>");
                sb.AppendLine("                </div>");
            }
            sb.AppendLine("                <div class=\"actions-grid\">");
            sb.AppendLine("                  <div class=\"action-card child-action\">");
            sb.AppendLine("                    <div class=\"action-header\">👧👦 Children Action</div>");
            sb.AppendLine($"                    <p>{HttpUtility.HtmlEncode(ch.ChildrenAction)}</p>");
            sb.AppendLine("                  </div>");
            sb.AppendLine("                  <div class=\"action-card teacher-action\">");
            sb.AppendLine("                    <div class=\"action-header\">🌱 Teacher Guidance</div>");
            sb.AppendLine($"                    <p>{HttpUtility.HtmlEncode(ch.TeacherSpark)}</p>");
            sb.AppendLine("                  </div>");
            sb.AppendLine("                </div>");
            if (!string.IsNullOrWhiteSpace(ch.WonderPrompt))
            {
                sb.AppendLine("                <div class=\"wonder-box\">");
                sb.AppendLine("                  <span style=\"font-size: 18px;\">💡</span>");
                sb.AppendLine("                  <div>");
                sb.AppendLine("                    <strong>Wonder Prompt: </strong>");
                sb.AppendLine($"                    <span>{HttpUtility.HtmlEncode(ch.WonderPrompt)}</span>");
                sb.AppendLine("                  </div>");
                sb.AppendLine("                </div>");
            }
            sb.AppendLine("              </div>");
            sb.AppendLine("            </div>");
            sb.AppendLine("          </div>");
        }
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

    function copyGeminiPrompt() {
      const promptText = document.getElementById('geminiPromptData')?.value || '';
      if (!promptText) {
        showToast('Prompt not available.');
        return;
      }
      if (navigator.clipboard) {
        navigator.clipboard.writeText(promptText).then(() => {
          showToast('✨ Gemini Master Prompt copied to clipboard! Paste into gemini.google.com');
        }).catch(() => {
          fallbackCopy(promptText);
        });
      } else {
        fallbackCopy(promptText);
      }
    }

    function fallbackCopy(text) {
      const ta = document.createElement('textarea');
      ta.value = text;
      document.body.appendChild(ta);
      ta.select();
      document.execCommand('copy');
      document.body.removeChild(ta);
      showToast('✨ Gemini Master Prompt copied to clipboard!');
    }

    function showToast(msg) {
      const toast = document.getElementById('toastNotification');
      if (!toast) return;
      toast.textContent = msg;
      toast.classList.add('visible');
      setTimeout(() => toast.classList.remove('visible'), 4000);
    }

    async function uploadStoryboardImage(input) {
      if (!input.files || input.files.length === 0) return;
      const file = input.files[0];
      const expId = document.getElementById('experienceIdData')?.value;
      if (!expId) return;

      const fd = new FormData();
      fd.append('file', file);
      showToast('Uploading illustrated storyboard image...');

      try {
        const res = await fetch('/api/experiences/' + expId + '/storybook/image', {
          method: 'POST',
          body: fd
        });
        if (res.ok) {
          showToast('🎉 Illustrated storyboard uploaded successfully! Reloading...');
          setTimeout(() => window.location.reload(), 1000);
        } else {
          showToast('Failed to upload image. Please try again.');
        }
      } catch (e) {
        showToast('Upload error: ' + e.message);
      }
    }

    // Full Size Image Viewer & Lightbox Helpers
    function openFullSizeImage() {
      const modal = document.getElementById('lightboxModal');
      if (modal) {
        modal.classList.add('active');
        document.body.style.overflow = 'hidden';
      } else {
        openImageInNewWindow();
      }
    }

    function closeLightbox() {
      const modal = document.getElementById('lightboxModal');
      if (modal) {
        modal.classList.remove('active');
        document.body.style.overflow = '';
      }
    }

    document.addEventListener('keydown', function(e) {
      if (e.key === 'Escape') closeLightbox();
    });

    function openImageInNewWindow() {
      const img = document.querySelector('.storybook-grid-img');
      if (!img) return;
      const expId = document.getElementById('experienceIdData')?.value || '';
      if (window.location.protocol.startsWith('http') && expId) {
        window.open('/api/experiences/' + expId + '/storybook/image', '_blank');
        return;
      }
      const win = window.open('');
      if (win) {
        win.document.write('<!DOCTYPE html><html><head><meta charset=""utf-8""><title>Full Size Storybook</title>' +
          '<style>body{margin:0;background:#181c19;display:flex;justify-content:center;align-items:center;min-height:100vh;padding:20px;box-sizing:border-box;}' +
          'img{max-width:100%;max-height:96vh;object-fit:contain;border-radius:10px;box-shadow:0 12px 40px rgba(0,0,0,0.6);}' +
          '</style></head><body><img src=""' + img.src + '"" alt=""Full Size Storybook"" /></body></html>');
        win.document.close();
      }
    }

    function downloadStorybookImage() {
      const img = document.querySelector('.storybook-grid-img');
      if (!img) return;
      const a = document.createElement('a');
      a.href = img.src;
      a.download = 'Illustrated_Storybook_Grid.png';
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
    }
  </script>");

        sb.AppendLine($"  <textarea id=\"geminiPromptData\" style=\"display:none;\">{HttpUtility.HtmlEncode(geminiPrompt ?? "")}</textarea>");
        sb.AppendLine($"  <input type=\"hidden\" id=\"experienceIdData\" value=\"{experienceId?.ToString() ?? ""}\" />");
        sb.AppendLine("  <div id=\"toastNotification\" class=\"toast-popup\"></div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

}

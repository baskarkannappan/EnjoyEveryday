---
okf_version: "0.2.0"
type: concept
title: "EnjoyEveryday UI Theme & Design System"
description: "Aesthetic philosophy, color tokens, typography scale, page structure, and reusable component patterns."
parent: "index.md"
status: active
generated: "2026-11-05"
tags:
  - ui
  - design-system
  - css-tokens
  - typography
  - components
---

# EnjoyEveryday UI Theme & Design System

## 1. Aesthetic Philosophy & Brand Identity
EnjoyEveryday is a daycare and early-childhood education management platform.
* **Tone**: Warm, calming, nature-inspired, organic, clean, Scandinavian-crafted.
* **Strictly Prohibited**: Cold Tailwind blues (`#2563eb`), dark mode, neon accents, tech-bro SaaS templates, or generic unstyled `<div>` tables.

## 2. Core CSS Color Tokens

| Variable | Hex Value | Purpose |
|:---|:---|:---|
| `--bg` | `#f5f5ef` / `#f7f8f5` | Page background |
| `--surface` | `#ffffff` | Primary card / container surface |
| `--surface-soft` | `#f8f9f5` | Inner sections, tags, subtle input background |
| `--border` | `#e4e7df` / `#e1e6e0` | Subtle warm border lines |
| `--text` | `#26332d` | Primary body and heading text (deep forest slate) |
| `--muted` | `#6c7870` / `#7c877f` | Subtitles, labels, timestamps, secondary details |
| `--accent` | `#557c6c` / `#52745e` | Signature brand sage green (primary buttons & highlights) |
| `--accent-light` | `#e6eee9` / `#e5eee8` | Active badges, avatar background, highlighted pills |
| `--accent-dark` | `#3e6253` | Button hover, icon color, strong status badges |
| `--warm` | `#f4eee1` / `#f5efe3` | Warm sand / cream accent |
| `--shadow` | `0 8px 30px rgba(40, 55, 47, .06)` | Standard natural card elevation shadow |
| `--radius` | `18px` | Standard border radius for cards & major containers |

## 3. Typography & Sizing
* **Font Family**: `"Roboto", -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif` — defined once as `--ee-font` in `src/EnjoyEveryday.UI.Shared/wwwroot/css/ee-foundation.css`.
* **Container Zoom**: Enforces `zoom: 0.82` globally.
* **Focused Pages**: Compact views (e.g. `TeacherWorkspace`) must use `max-width: 960px !important; margin: 0 auto !important;` to avoid Chromium flex stretching bugs.

## 4. Standard Component Patterns
* **Primary Button**: `<button class="primary">` (Sage green `var(--accent)`, hover `var(--accent-dark)`, white text, 11px radius, font weight 700).
* **Secondary Button**: `<button class="secondary">` (White surface, 1px border `var(--border)`, text `var(--text)`).
* **Card Container**: `background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius); box-shadow: var(--shadow); padding: 20px;`.
* **Mobile Layouts**: KPI metric strips keep 3 cards in a compact 3-column row (`repeat(3, 1fr)`). Primary action grids keep 2 columns (`repeat(2, 1fr)`).

## 5. Mobile Fixed Navigation & Safe Area Clearance
* Portals using a fixed bottom navigation bar (e.g. `ParentMobileNav` in the Parent Portal) must ensure scrollable viewports never obscure bottom elements (e.g., chat composers, submit buttons, action cards).
* **Standard Clearance**:
  * Main scroll container (`.parent-portal-main`): `padding-bottom: calc(84px + env(safe-area-inset-bottom, 0px)) !important;`
  * Content wrapper (`.parent-content-wrapper`): `padding-bottom: calc(100px + env(safe-area-inset-bottom, 0px)) !important;`
  * Bottom DOM spacer (`.mobile-bottom-spacer`): `display: block; height: calc(76px + env(safe-area-inset-bottom, 0px));` to physically guarantee scroll space across all mobile WebKit and Chromium engines.
  * Nav bar height: `height: calc(64px + env(safe-area-inset-bottom, 0px))` with `padding-bottom: env(safe-area-inset-bottom, 0px)`.

## 6. Role-Based Navigation Architecture (`NavMenu.razor`)
* **Role Views**: Navigation separates completely into 4 distinct roles organized with lightweight independent functional groups and nested expandable submenus:
  * **Organization Admin** (8 groups): Workspace, Organization (with Branch Management & Staff submenus), People & Admissions (with Admissions submenu), Daycare Operations, Experiences, Child Development, Family Engagement, Business & Settings.
  * **Branch Admin** (8 groups): Workspace, Branch Management, People & Admissions (with Admissions submenu), Daycare Operations (with Safety & Compliance submenu), Experiences, Child Development, Family Engagement, Reports & Settings.
  * **Lead Teacher** (7 groups): My Workspace, Classroom Management, Child Care, Experiences & Activities, Child Development, Family Engagement, My Account (with Safety submenu).
  * **Teacher / Educator** (6 groups): My Workspace, My Classroom, Daily Care, Activities, Child Development, My Account (with Safety submenu).
* **Typography & Casing**: Group headers, main menu items, and submenus are consistently formatted in Title Case / Camel Case (`My Workspace`, `Daycare Operations`, `People & Admissions`, `Child Development`) with `text-transform: none` to maintain a calm, natural, Scandinavian aesthetic rather than aggressive all-caps.
* **Nested Submenus**: Multi-level hierarchical submenus use `HashSet<string> _openSubmenus` for independent expand/collapse states with `border-left: 1.5px solid var(--border)` visual rails.
* **Context Display & Switchers**: Sidebar footer houses the user avatar, name, active role badge, quick workspace switchers (`/select-branch`, `/select-organization`), and view dropdown selector for multi-role users.
* **Smart Auto-Expansion**: Active child routes automatically expand their parent submenu and highlight active links, while unrelated submenus remain unaffected.

## 7. Web Performance & Core Web Vitals Standards (Lighthouse 90+)
To guarantee high Lighthouse performance scores across all portals (`Daycare.Web`, `Parent.Web`, `Admin.Web`), the following standards are mandatory:
1. **No CSS `@import` for Fonts**: Never use `@import url('https://fonts...')` inside `.css` files. Doing so halts CSS parsing and creates critical request waterfalls. Fonts must be loaded asynchronously via `<link rel="preload" as="style">` and `<link rel="stylesheet" media="print" onload="this.media='all'">` in `App.razor`.
2. **Server-Side Response Compression**: Every portal `Program.cs` configures `AddResponseCompression` with Brotli and Gzip providers covering HTML, CSS, JS, and SVG.
3. **Static Asset Caching**: 1-year immutable caching (`Cache-Control: public, max-age=31536000, immutable`) is injected into HTTP response headers for `/_content/`, `/css/`, `/js/`, `.woff2`, and static files.
4. **On-Demand Script Loading**: Large third-party libraries (e.g. Razorpay checkout) must be loaded dynamically on demand in JS modules rather than globally in `<head>`. Secondary utility scripts must include `defer`.
5. **Zero-CLS Blazor Server Instant Shell Architecture**: Never wrap entire page contents in `@if (_loading) { <LoadingState> } else { <PageContent> }`. This pattern delays Largest Contentful Paint (LCP) by 3+ seconds and causes 100% Cumulative Layout Shift (CLS) when the spinner is replaced. Pages must render their `<PageHero>`, navigation cards, and layout shells immediately on frame 1; metrics and asynchronous datasets populate dynamically in-place.

## 8. Typography Scale, Modal System & Directory Grid Hierarchy
To eliminate inconsistent font families, oversized headings, illegible micro-text, and fragmented modal styles across portals:
1. **Universal Font Family**: All DOM elements (`*, *::before, *::after`) cascade to `var(--ee-font)` (`"Roboto", -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif`). Fluent UI components receive `:root` token overrides (`--font-family`, `--body-font`, `--type-ramp-*`, `--colorNeutralForeground*`).
2. **Standardized Typography Ramp**:
   * **Heading 1 / Page Title**: 22px (`--fs-2xl`), font-weight 800, letter-spacing -0.4px, `var(--text, #26332d)`.
   * **Heading 2 / Section Title**: 17px (`--fs-xl`), font-weight 750, letter-spacing -0.3px, `var(--text, #26332d)`.
   * **Heading 3 / Subheader / Card Title**: 14px–15px (`--fs-lg`), font-weight 750, `var(--text, #26332d)`.
   * **Heading 4 / Micro-header**: 13.5px (`--fs-md`), font-weight 700.
   * **Body Text**: 12.5px (`--fs-base`), line-height 1.45, `var(--text, #26332d)`.
   * **Subtitles & Secondary Text**: 11.5px–12px (`--fs-sm`), `var(--muted, #6c7870)`.
   * **Eyebrows & Micro-labels**: 10px–10.5px (`--fs-xs`), font-weight 800 uppercase, letter-spacing 0.8px, `var(--accent, #557c6c)`.
   * **Action Buttons**: 12px, font-weight 700, padding 5px 12px, border-radius 0px. Primary: `#31523c` (`#23412f` hover); Secondary: white with `var(--border)`.
   * **Stat & Metric Numbers**: 24px–28px, font-weight 800, `var(--text, #26332d)`.
3. **Universal Dialog & Modal System**:
   * Standardizes `.modal`, `.modal-card`, `.plan-experience-dialog`, `.invoice-modal-card`, `.modal-card-pipeline`, `fluent-dialog::part(control)`.
   * All modals enforce `zoom: 0.82`, `border-radius: 0px`, `background: #ffffff`, and 1px border `var(--border)`.
   * **Header**: `background: var(--surface-soft, #f8f9f5); padding: 14px 20px; border-bottom: 1px solid var(--border)`. Title is 17px 750 weight. Close button is 15px.
   * **Body**: `padding: 18px 20px; background: #ffffff; color: var(--text)`. Input controls inherit `var(--ee-font)` with 12px–12.5px font-size and 0px radius.
   * **Footer**: `background: var(--surface-soft, #f8f9f5); padding: 12px 20px; border-top: 1px solid var(--border)`.
4. **Universal Directory & People Grids**:
   * `.people-grid`: CSS grid with `repeat(auto-fill, minmax(280px, 1fr))` and 14px gap.
   * `.person`: standard border, 0px border-radius, 16px padding.
   * `.person h3`: 14px 750 weight; `.person p`: 12px `var(--muted)`; `.person-interest`: 12px with uppercase 10.5px bold tag. Replaces legacy illegible 9px text.




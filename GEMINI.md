# EnjoyEveryday Web Application Theme & Design System

Every agent (including Antigravity) working in this repository MUST strictly follow the EnjoyEveryday design system and theme when creating, modifying, or refactoring pages.

## 1. Aesthetic Philosophy & Brand Identity

EnjoyEveryday is a daycare and early-childhood education management platform.
- **Tone**: Warm, calming, nature-inspired, organic, clean, and Scandinavian-crafted.
- **Strictly Prohibited**:
  - Do NOT use generic cold Tailwind blues (`#2563eb`, `#1d4ed8`, `#3b82f6`).
  - Do NOT use dark mode, neon colors, heavy purple gradients, or tech-bro SaaS templates.
  - Do NOT create ad-hoc generic bootstrap-style tables or unstyled `<div>` grids.

---

## 2. Core CSS Variables & Color Tokens

Always reference these CSS variables or their hex equivalents:

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

---

## 3. Typography & Fonts

- **Font Family**: `"Figtree", Inter, -apple-system, sans-serif`
- **Heading 1 / 2**: `font-weight: 750` to `800`, letter-spacing `-0.3px` to `-0.5px`, color `var(--text, #26332d)`.
- **Subtitles**: `font-size: 13px` to `14px`, color `var(--muted, #6c7870)`.

---

## 4. Standard Page Structure

Every page in `EnjoyEveryday.Daycare.Web` should follow this structure:

```razor
@page "/your-route"
@using EnjoyEveryday.UI.Shared.Components

<PageTitle>Page Title — EnjoyEveryday</PageTitle>

<div class="content">
    <PageHero Eyebrow="CATEGORY"
              Title="Page Title"
              Subtitle="Concise, friendly description of this page's purpose.">
        <RightContent>
            <a href="/secondary" class="btn secondary">Secondary Action</a>
            <button class="primary" @onclick="HandleAction">+ Primary Action</button>
        </RightContent>
    </PageHero>

    <!-- Page Content (Cards, Grids, Forms) -->
</div>
```

---

## 5. Standard Component Patterns

### A. Buttons
- **Primary**: `<button class="primary">` or `<a class="btn primary">`
  - Rounded `11px`, background `var(--accent)`, hover `var(--accent-dark)`, white text, `font-weight: 700`.
- **Secondary**: `<button class="secondary">` or `<a class="btn secondary">`
  - Rounded `11px`, background `white`, border `1px solid var(--border)`, color `var(--text)`.

### B. Directory & Listing Grids (`people-grid` & `person`)
When listing people (teachers, children, users, parents):
```razor
<!-- Search & Filter Row -->
<div class="search-row">
    <input class="search" @bind="searchQuery" placeholder="Search..." />
    <select class="filter" @bind="selectedFilter">
        <option value="All">All Items</option>
    </select>
</div>

<!-- Filter Tabs -->
<div class="tabs">
    <button class="tab @(activeTab == "All" ? "active" : "")" @onclick='() => SetTab("All")'>All</button>
</div>

<!-- People Grid -->
<div class="people-grid">
    <div class="person">
        <div class="person-header">
            <div class="person-avatar" style="background: var(--accent-light); color: var(--accent-dark);">
                A
            </div>
            <div>
                <h3>Name</h3>
                <p>Email or Subtitle</p>
            </div>
        </div>
        <div class="person-interest">
            <strong>Assignment</strong>
            <p>Classroom or Role details</p>
        </div>
    </div>
</div>
```

### C. Cards & Containers
- Cards must use `background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius); box-shadow: var(--shadow); padding: 20px;`.
- Avatar badges must use `background: var(--accent-light); color: var(--accent-dark);` or gradient `linear-gradient(135deg, var(--accent), var(--accent-dark))`.

### D. Badges & Tags
- Active status: `background: var(--accent-light, #e6eee9); color: var(--accent-dark, #3e6253); border: 1px solid #c8dacd; border-radius: 6px; font-weight: 700;`
- Inactive status: `background: #fff5f5; color: #9b2c2c; border: 1px solid #fed7d7; border-radius: 6px; font-weight: 700;`

---

## 6. Testing Policy

- **CRITICAL**: Do NOT run automated tests (`dotnet test`, unit tests, integration tests, test projects, or test runner scripts) unless the user explicitly instructs you to do so.

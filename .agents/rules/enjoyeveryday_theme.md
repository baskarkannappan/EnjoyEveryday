# Rule: EnjoyEveryday Web Application Theme & Design System

Every agent modifying or creating web components in this workspace MUST adhere to the EnjoyEveryday design system:

1. **Brand Palette**:
   - Primary: Sage Green `var(--accent, #557c6c)`, Dark Sage `var(--accent-dark, #3e6253)`, Sage Light `var(--accent-light, #e6eee9)`
   - Backgrounds: Warm Sand/Cream `var(--bg, #f5f5ef)`, Surface `var(--surface, #ffffff)`, Soft Surface `var(--surface-soft, #f8f9f5)`
   - Borders: Warm subtle grey `var(--border, #e4e7df)`
   - Text: Dark forest slate `var(--text, #26332d)`, Muted `var(--muted, #6c7870)`
   - Shadows & Radius: `var(--shadow, 0 8px 30px rgba(40, 55, 47, .06))`, `var(--radius, 18px)`

2. **Prohibited Styles**:
   - Never use Tailwind blue `#2563eb`, `#1d4ed8`, `#3b82f6` for buttons, headers, or active cards.
   - Never use dark-mode styling unless explicitly requested.

3. **Required Page Anatomy**:
   - Root wrapper: `<div class="content">`
   - Hero: `<PageHero Eyebrow="..." Title="..." Subtitle="...">` with `<RightContent>`
   - Directory/People layout: Use `.search-row` + `.tabs` + `.people-grid` + `.person` (matching `Teachers.razor` and `Children.razor`).
   - Buttons: `.primary` (`#557c6c`) or `.secondary` (white with `#e4e7df` border).

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

## 6. Delight-Crafted Cards System (24px Radius & Whimsical Visuals)
Key interactive cards in EnjoyEveryday adopt a rounded, early-childhood crafted look with cheerful organic decorations:
* **`ExperienceCard` (`src/EnjoyEveryday.UI.Shared/Components/ExperienceCard/`)**:
  * `border-radius: 22px;` with subtle whimsical rainbow & twinkling stars corner decoration.
  * Soft pastel category pill badge (`💖 Creative Art`, `🌿 Nature`, etc.) and `✓ Published` status pill.
  * Attributes ribbon with pill badges for age (`👥 3-4 yrs`), duration (`⏱ 15 min`), setting (`🏠 Indoor`), and session runs (`🛡 1 run`).
  * Pill action buttons: `📖 Storybook` pill button and deep forest sage `View Activity →` pill CTA (`border-radius: 999px;`).
* **`CelebrationCard` (`src/EnjoyEveryday.UI.Shared/Components/CelebrationCard/`)**:
  * `border-radius: 22px;` with festive party popper / confetti horn corner illustration SVG.
  * Pill tags: `🏆 Celebration` (soft pink) and `✓ Completed` (soft mint green).
  * Unified date, time, and location pill bar (`📅 Oct 21, 2026 • 🕒 1:00 • 📍 Daycare Center`).
  * Deep forest green pill CTA: `View Details →`.
* **`AdmissionsApplicationCard` (`src/EnjoyEveryday.UI.Shared/Components/Admissions/`)**:
  * `border-radius: 24px;` with soft rainbow corner accent.
  * Circular avatar with verified green check badge `✓`, large child name, and status pill (`✓ Enrolled Active`).
  * 2x2 grid with icon containers (Parent/Guardian, Contact, Room & Schedule, Tuition Plan).
  * Tour scheduled green pill strip and collapsible admissions notes box.
* **`ClassroomCard` (`src/EnjoyEveryday.UI.Shared/Components/Home/ClassroomCard/`)**:
  * 3D glossy emerald sphere avatar with classroom initial and twinkling yellow stars.
  * Progress / occupancy bar (`100% Complete`), `✓ Active` pill badge, and `View Details →` pill CTA.



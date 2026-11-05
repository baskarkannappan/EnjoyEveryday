---
okf_version: "0.2.0"
type: concept
title: "Parent Portal & Child Journeys"
description: "Parent portal architecture, daily feed, photo moments, media consent, and pickup notifications."
parent: "index.md"
status: active
generated: "2026-11-05"
tags:
  - parent-portal
  - families
  - journey
  - media-consent
---

# Parent Portal & Child Journeys

## Portal Context
Hosted in `src/EnjoyEveryday.Parent.Web` (port `7005`). Designed specifically for guardians and families across mobile and desktop.

## Security & Scoping
* **Scope Boundary**: Strictly scoped by `family_relationships` (parent user ID to child ID).
* **Media Consent Verification**: Before photos or videos are published to a parent's feed, the media service verifies `child_media_consents`. If consent is declined or missing, faces are blurred or media is withheld.

## Primary Pages & Features
* **Today's Overview (`/`, `/today`)**: Live updates on daily care (naps, diaper changes, meals, bottle ounces) recorded by teachers in the Daycare app, plus verified attendance badges.
* **Child Journey (`/journey`)**: Developmental milestones, curiosity pillars, educator observations, and child voice moments.
* **Memories (`/memories`)**: Visual chronological photo and moment gallery with media consent gating.
* **Classroom Messages (`/messages`)**: Direct, secure messaging with verified classroom educators with quiet hours enforcement and quick replies. Backed by stored procedures `sp_get_parent_messages` and `sp_send_parent_message`.
* **Family Profile (`/family`)**: Connected guardians, emergency contacts, authorized pickups, and invoice summaries.

## Mobile Layout Standards
* Bottom navigation bar (`ParentMobileNav.razor`) provides quick access to Today, Journey, Memories, Messages, and Family.
* Clearance standard: `.parent-content-wrapper` enforces `padding-bottom: calc(100px + env(safe-area-inset-bottom, 0px)) !important` and `.mobile-bottom-spacer` ensures content never hides behind the fixed menu bar.


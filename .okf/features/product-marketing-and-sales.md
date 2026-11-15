---
okf_version: "0.2.0"
type: concept
title: "Product Marketing, Sales Landing Page & Social Campaigns"
description: "High-converting B2B sales landing page, LinkedIn document carousel/video simulator, Meta (Facebook/Instagram) creative studio, and outbound sales playbook."
parent: "index.md"
status: active
generated: "2026-11-14"
tags:
  - marketing
  - sales
  - landing-page
  - linkedin
  - social-media
  - meta-ads
  - real-screens
---

# Product Marketing, Sales Landing Page & Social Campaigns

## Overview

The `marketing/` directory contains an end-to-end B2B digital marketing and sales suite for EnjoyEveryday, tailored specifically for daycare center directors, preschool founders, and multi-campus childcare network operators.

All assets are built using EnjoyEveryday's Scandinavian design system tokens (`--accent: #557c6c`, `--accent-dark: #31523c`, `--surface: #ffffff`, `--bg: #f7f8f5`) and incorporate real production screenshots from the running portals:
* **Daycare Web (Port 7003)**: `https://localhost:7003/`
* **Parent & Family Portal (Port 7005)**: `https://localhost:7005/`
* **Admin Control Plane (Port 7004)**: `https://localhost:7004/`

## Asset Directory Structure

* **`marketing/index.html`**: Complete responsive sales landing page with hero screen mockup, dual-portal showcase (Daycare Web vs. Parent Portal), interactive deep dive gallery, live ROI calculator, transparent pricing matrix, and demo lead capture modal.
* **`marketing/README.md`**: Master sales playbook containing outbound cold email templates to daycare directors and a 15-minute live demo closing script.
* **`marketing/assets/`**: High-resolution production screenshots:
  * `daycare_organization_management.png` (Live Daycare Web dashboard)
  * `parent_portal_today.jpg` (Live Parent Portal today screen)
  * `daycare_campus_directory.jpg` (Live Campus Directory & About Us)
  * `daycare_organization_hierarchy.jpg` (Multi-campus organizational tree)
  * `platform_admin_organizations_tariffs.jpg` (Platform Admin tariff directory)
  * `subscription_tariff_card.png` (Sprout Starter regional tariff card)
* **`marketing/linkedin-campaign/`**:
  * `linkedin-slides.html`: Interactive 6-slide presentation deck with Auto-Play Video Mode simulator and Save-as-PDF for LinkedIn Document Carousel posts.
  * `linkedin-post-copy.md`: 3 ready-to-publish B2B post copy variants with hashtags.
* **`marketing/social-media-campaign/`**:
  * `social-post-cards.html`: Interactive Creative Studio with aspect ratio toggles (1:1 Feed, 4:5 Portrait, 9:16 Story/Reel) and animated Reel video simulator.
  * `meta-posts-copy.md`: Organic Instagram/Facebook captions, 30-second Reel script with voiceover, Story poll prompts, and Meta Ads Manager audience targeting setup.

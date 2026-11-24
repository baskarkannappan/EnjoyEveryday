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
* **Today's Overview (`/`, `/today`)**: Live updates on daily care (naps, diaper changes, meals, bottle ounces) recorded by teachers in the Daycare app, verified attendance badges, and the **Daily Experience Story Card** showcasing the classroom's developmental activity, inquiry wonder prompts, **Bedtime Conversation Starters**, and **Try-at-Home** family extensions.
* **Child Journey (`/journey`)**: Developmental milestones, curiosity pillars, educator observations, and child voice moments.
* **Memories (`/memories`)**: Visual chronological photo and moment gallery with media consent gating and bulk photo download feature.
* **Photo Storage & Retention Policy Notice**:
  - A top-bar notification banner renders on the Parent Portal (`MainLayout.razor`) when parents log in.
  - Informs parents that photos and media are stored for **3 months** and encourages downloading cherished moments before expiration.
  - Automatically fades out and disappears after **10 seconds** (with an immediate "✕" dismissal option) and tracks dismissal within `ParentSessionState` to prevent spamming sub-navigation.
* **Classroom Messages (`/messages`)**: Direct, secure messaging with verified classroom educators with quiet hours enforcement (`IsQuietHours` mutes push sounds after 8:00 PM and before 7:00 AM) and quick replies. Backed by stored procedures `sp_get_parent_messages`, `sp_send_parent_message`, and SignalR real-time notification broadcasting.
* **Family Profile (`/family`, `/profile`, `/settings`)**: Connected guardians, emergency contacts, authorized pickups (synced to educator checkout gates), and self-service language and communication preferences.
* **Native Consent Signing (`/consent/sign/{Token}`, `/consent/document/{DocumentId}`)**:
  - Embedded electronic signature pad with drawn or typed signatures, legal consent declarations, and automatic PDF audit copy generation.
  - Documents transition from `SentToParent` → `Opened` → `Signed` with cryptographic verification.
* **My Billing & Invoices (`/billing`)**:
  - Dedicated page (`src/EnjoyEveryday.Parent.Web/Components/Pages/Billing.razor`) and modal (`src/EnjoyEveryday.Parent.Web/Components/Shared/InvoicesListModal.razor`) strictly scoped to `OrgToParent` childcare invoices for the selected child (`fn_get_child_invoices`).
  - Displays child selector, summary KPIs, month-by-month billing history, payment history, snapshotted bank/QR instructions, and **Pay Now** (uses the invoice-owning organization's active Razorpay gateway when configured, or falls back to manual QR / Bank Transfer / Cash submission with staff verification).
* **Birthday Greeting & Classroom Giving (`/giving`, `/giving/orders`)**:
  - **Birthday Greeting Banner**: Detected automatically on child's birthday on Today's Overview (`Today.razor`). Features a celebratory birthday greeting with quick CTA (`🎁 Send a Birthday Gift`) and one-tap dismissibility stored in session storage.
  - **Classroom Giving Page (`/giving`)**: Two giving modes:
    1. *Birthday Gift*: Select a celebrating classmate peer in the child's room (respecting privacy consent settings), add greeting message.
    2. *General Classroom Contribution*: Contribute educational toys, art kits, books, or activity supplies for general classroom use.
  - **Product Catalog Reuse & Multi-Image Lightbox**: Dynamically queries existing marketplace products (`material_products` and `material_offerings`) without duplicating the product catalog. Features:
    * Interactive multi-image product carousels with next/previous arrow navigation (`‹` / `›`) and pagination dots.
    * Click-to-enlarge full-resolution image popup lightbox modal with image scrolling arrows, thumbnails drawer, product details, and quick "Add to Gift" CTA.
    * Real asset fallback supporting both uploaded WebP media endpoints (`/api/media/{id}/display`) and localized static educational assets.
  - **Mandatory Facility Destination**: Enforces delivery strictly to either the child's branch registered address or organization headquarters (pulled from `facility_profiles`). Home delivery is prohibited.
  - **Giving Orders Tracker (`/giving/orders`)**: Real-time order progress timeline, approval status, payment modal supporting 4 payment methods (Cash, Org Razorpay, UPI QR with UTR submission, Direct Bank Transfer), official receipt modal, and immutable audit timeline drawer.

## Mobile Layout Standards
* Bottom navigation bar (`ParentMobileNav.razor`) provides quick access to Today, Journey, Memories, Messages, and Family.
* Clearance standard: `.parent-content-wrapper` enforces `padding-bottom: calc(100px + env(safe-area-inset-bottom, 0px)) !important` and `.mobile-bottom-spacer` ensures content never hides behind the fixed menu bar.


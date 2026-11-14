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
* **Memories (`/memories`)**: Visual chronological photo and moment gallery with media consent gating.
* **Classroom Messages (`/messages`)**: Direct, secure messaging with verified classroom educators with quiet hours enforcement (`IsQuietHours` mutes push sounds after 8:00 PM and before 7:00 AM) and quick replies. Backed by stored procedures `sp_get_parent_messages`, `sp_send_parent_message`, and SignalR real-time notification broadcasting.
* **Family Profile (`/family`, `/profile`, `/settings`)**: Connected guardians, emergency contacts, authorized pickups (synced to educator checkout gates), and self-service language and communication preferences.
* **Native Consent Signing (`/consent/sign/{Token}`, `/consent/document/{DocumentId}`)**:
  - Embedded electronic signature pad with drawn or typed signatures, legal consent declarations, and automatic PDF audit copy generation.
  - Documents transition from `SentToParent` → `Opened` → `Signed` with cryptographic verification.
* **Tuition Billing & Razorpay Online Payments**:
  - Modal: `src/EnjoyEveryday.Parent.Web/Components/Shared/InvoicesListModal.razor`.
  - Service: `ParentService.CreateInvoicePaymentOrderAsync` and `VerifyAndApplyPaymentAsync`.
  - JS Interop: `wwwroot/js/razorpayCheckout.js` launches standard Razorpay checkout modal for UPI, debit/credit cards, and net banking.
  - Verification: Enforces HMAC-SHA256 signature verification via `IRazorpayGateway` before recording payment in `payments` and marking `invoices` balance. Supports simulation mode for local development.

## Mobile Layout Standards
* Bottom navigation bar (`ParentMobileNav.razor`) provides quick access to Today, Journey, Memories, Messages, and Family.
* Clearance standard: `.parent-content-wrapper` enforces `padding-bottom: calc(100px + env(safe-area-inset-bottom, 0px)) !important` and `.mobile-bottom-spacer` ensures content never hides behind the fixed menu bar.


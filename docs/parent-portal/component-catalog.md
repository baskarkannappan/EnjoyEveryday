# EnjoyEveryday Parent Portal — Reusable Component Catalog

This catalog documents the reusable UI components designed specifically for the Parent Portal and shared across family-facing views.

## Component Overview

| Component | Responsibility | Owning Project | Parameters & Events | Consumers |
| :--- | :--- | :--- | :--- | :--- |
| `ParentPageHeader` | Renders category eyebrow, page title, subtitle, and optional right-aligned child status pill or action. | `EnjoyEveryday.Parent.Web` / `Shared` | `string Eyebrow`, `string Title`, `string? Subtitle`, `RenderFragment? RightContent` | `Today.razor`, `ChildJourney.razor`, `Messages.razor`, `Events.razor`, `FamilyProfile.razor`, `Memories.razor` |
| `ChildSwitcher` | Mobile-friendly child pill / selector displaying active child avatar, classroom name, and dropdown when multiple children are authorized. | `EnjoyEveryday.Parent.Web` / `Shared` | `ParentAuthorizedChild? SelectedChild`, `IReadOnlyList<ParentAuthorizedChild> Children`, `EventCallback<Guid> OnChildSelected` | `NavMenu.razor`, `MainLayout.razor`, `ParentMobileNav.razor` |
| `ParentMobileNav` | Clean fixed bottom navigation bar with icons for phone screens (Today, Journey, Memories, Messages, More). | `EnjoyEveryday.Parent.Web` / `Layout` | `int? UnreadMessagesCount`, `int? PendingConsentCount` | `MainLayout.razor` |
| `DailyUpdateCard` | Formatted card for timeline items (Attendance arrival/departure, scheduled experiences, moments) with category badges. | `EnjoyEveryday.Parent.Web` / `Shared` | `ChildDailyTimelineItem Item` | `Today.razor` |
| `CareSummaryCard` | High-density 2x2 grid displaying diaper changes, rest/nap duration, meal status, and emotional comfort/mood. | `EnjoyEveryday.Parent.Web` / `Shared` | `ParentDailyCareReport CareReport` | `Today.razor` |
| `ActivityTimeline` | Vertical container with gentle line markers grouping the child's daily schedule and milestones. | `EnjoyEveryday.Parent.Web` / `Shared` | `IReadOnlyList<ChildDailyTimelineItem> Items` | `Today.razor` |
| `MediaThumbnail` | Responsive image thumbnail with lazy loading (`loading="lazy"`), smooth hover elevation, and click-to-view callback. | `EnjoyEveryday.Parent.Web` / `Shared` | `string ImageUrl`, `string? Caption`, `string? Timestamp`, `EventCallback<string> OnClick` | `Memories.razor`, `Events.razor`, `Today.razor` |
| `MediaGallery` | Grid layout adapting from 2 columns on phones to 4 columns on tablets/desktops, supporting filter pills (All, Celebrations, Daily). | `EnjoyEveryday.Parent.Web` / `Shared` | `IEnumerable<MediaItemModel> MediaItems`, `EventCallback<MediaItemModel> OnSelectMedia` | `Memories.razor`, `ChildJourney.razor` |
| `MediaViewer` | Accessible full-screen modal lightbox for browsing photos/videos with previous/next controls, keyboard navigation (Escape to close), and high-contrast labels. | `EnjoyEveryday.Parent.Web` / `Shared` | `bool IsOpen`, `MediaItemModel? CurrentItem`, `EventCallback OnClose`, `EventCallback OnPrevious`, `EventCallback OnNext` | `Memories.razor`, `Events.razor` |
| `ChatMessageBubble` | Clean message balloon with distinct styles for inbound teacher messages (surface-soft with teacher avatar) and outbound parent messages (signature brand sage green with timestamp and status). | `EnjoyEveryday.Parent.Web` / `Shared` | `string SenderName`, `string Content`, `DateTimeOffset Timestamp`, `bool IsFromParent`, `string? DeliveryStatus` | `Messages.razor` |
| `ChatComposer` | Mobile-optimized message text input with send button, auto-expanding text box, and enter-to-send support. | `EnjoyEveryday.Parent.Web` / `Shared` | `EventCallback<string> OnSendMessage`, `bool IsSending` | `Messages.razor` |
| `ConsentStatusCard` | Card indicating pending, signed, or required consent documents (e.g. photo consent, medical release) with one-click signing link. | `EnjoyEveryday.Parent.Web` / `Shared` | `ChildConsentDocument Document`, `EventCallback<Guid> OnReviewConsent` | `FamilyProfile.razor`, `Today.razor` |
| `AnnouncementCard` | Highlight card for center-wide announcements, emergency notices, or classroom reminders. | `EnjoyEveryday.Parent.Web` / `Shared` | `string Title`, `string Content`, `DateTime Date`, `string? UrgentTag` | `Today.razor`, `NavMenu.razor` |
| `LoadingState` | Nature-inspired loading indicator with reassuring message. | `EnjoyEveryday.Parent.Web` / `Shared` | `string Message` | All pages |
| `EmptyState` | Warm empty state with illustration/emoji, title, helpful guidance, and optional call to action button. | `EnjoyEveryday.Parent.Web` / `Shared` | `string Emoji`, `string Title`, `string Subtitle`, `RenderFragment? ActionContent` | All pages |
| `ErrorState` | Error display with retry trigger and clear non-technical explanation. | `EnjoyEveryday.Parent.Web` / `Shared` | `string Title`, `string Message`, `EventCallback OnRetry` | All pages |
| `LanguageSwitcher` | Regional language selector pill with dropdown supporting English, Tamil, Telugu, Hindi, and Kannada. | `EnjoyEveryday.Parent.Web` / `Shared` | Uses scoped `ParentLocalizationService` | `ParentPageHeader.razor`, `Today.razor`, `Messages.razor`, `FamilyProfile.razor` |
| `ReportAbsenceModal` | Touch-first modal for reporting illness, late arrival, or travel with automatic staff attendance notification. | `EnjoyEveryday.Parent.Web` / `Shared` | `bool IsOpen`, `Guid ChildId`, `string ChildName`, `EventCallback<bool> IsOpenChanged` | `Today.razor` |
| `InvoicesListModal` | Statement modal summarizing tuition invoices, due dates, balance due, and payment status via `IBillingService`. | `EnjoyEveryday.Parent.Web` / `Shared` | `bool IsOpen`, `Guid ChildId`, `EventCallback<bool> IsOpenChanged` | `Today.razor` |
| `AuthorizedPickupsModal` | Security modal detailing verified pickup contacts and custody safety checks from `ISecurePickupService`. | `EnjoyEveryday.Parent.Web` / `Shared` | `bool IsOpen`, `Guid ChildId`, `string ChildName`, `EventCallback<bool> IsOpenChanged` | `Today.razor` |

---

## Guidelines for Component Use

1. **Presentation Purity:** Reusable components only accept typed parameters and emit `EventCallback`s. They never inject database connection factories or execute raw SQL.
2. **Responsive Styling:** All components are styled using CSS variables defined in `GEMINI.md` (`--bg`, `--surface`, `--surface-soft`, `--border`, `--text`, `--accent`, `--accent-light`, `--accent-dark`, `--radius`).
3. **Accessibility:** Form fields, interactive buttons, and modals contain `aria-label`, visible focus indicators, and mobile touch targets of at least 44x44px.

# EnjoyEveryday Parent Portal — Performance Baseline & Measurement Report

**Date:** October 2026  
**Environment:** ASP.NET Core .NET 10.0, Blazor Interactive Server, PostgreSQL 16  
**Hosting Architecture:** Single Blazor Server circuit per active browser tab with WebSocket transport.

---

## 1. Baseline Performance Profile

| Workflow / Page | Initial Payload Size | Database Queries Executed | Circuit Memory Footprint | Observed Render Characteristics |
| :--- | :--- | :--- | :--- | :--- |
| **Initial App Load (`/`)** | HTML: ~18 KB<br>CSS: ~42 KB<br>JS framework: ~120 KB | 2 (Authorized children lookup, Attendance check) | ~85 KB circuit state | Fast initial server prerender. WebSocket established within 120ms. |
| **Today's Overview (`/today`)** | Dynamic DOM update | 4 (Authorized children, Attendance, Timeline items, Daily care report) | ~110 KB circuit state | 340+ lines of monolithic inline HTML. All sub-sections rendered synchronously. |
| **Child Journey (`/journey`)** | Dynamic DOM update | 2 (`child_journey_entries`, `child_milestones`) | ~140 KB (scales linearly with child age) | Unbounded query `ORDER BY observation_date DESC` without `LIMIT` or pagination. Memory could balloon for children with 3+ years of records. |
| **Events & Celebrations (`/events`)** | Dynamic DOM update | 1 (`events` query with classroom join) | ~90 KB | Inline full image URLs rendered without client thumbnail optimization or lazy-loading tags. |
| **Messages (`/messages`)** | Dynamic DOM update | 0 (Was hardcoded in-memory placeholder!) | ~40 KB | Instantaneous because it was a mock placeholder without database integration. |
| **Family Profile (`/family`)** | Dynamic DOM update | 1 (`persons` lookup by user_id) | ~50 KB | Very fast, simple profile view. |

---

## 2. Identified Performance Bottlenecks & Remedies

### 1. Unbounded History Queries (N+1 and Missing Pagination)
- **Risk:** In `ChildJourney.razor`, `GetApprovedChildJourneyAsync(childId)` fetched all records ever recorded for a child.
- **Remedy:** Implement pagination/load-more pattern with default batch size of 15 records.

### 2. Excessive Memory in Blazor Circuits
- **Risk:** Holding large collections of high-resolution image URLs or unneeded columns in circuit memory.
- **Remedy:** Map database records to compact view models (`MediaItemModel`, `JourneyEntryViewModel`) containing only presentation fields.

### 3. Missing Image Optimization & Lazy Loading
- **Risk:** Loading dozens of photos simultaneously upon visiting `/events` or `/journey`, slowing mobile downlinks and wasting user data.
- **Remedy:** 
  - Add native `loading="lazy"` and `decoding="async"` attributes to all media thumbnails.
  - Render small thumbnail dimensions (`160x120px` or `240x180px`) with CSS `object-fit: cover` and open full resolution only upon user click in `MediaViewer`.

### 4. Frequent WebSocket Round-Trips for State Transitions
- **Risk:** Re-executing child authorization checks and queries multiple times during child switching.
- **Remedy:** Single consolidated call in `ParentSessionState`, triggering `StateHasChanged()` only when the active child identifier genuinely changes.

---

## 3. Performance Targets for Modernization

| Metric | Target | Measurement Strategy |
| :--- | :--- | :--- |
| **Time to Interactive (Mobile 4G)** | < 1.5 seconds | Browser dev tools network throttling (Fast 4G / Slow 4G profile). |
| **Circuit State per Parent** | < 200 KB | Garbage Collection profiling / Process memory tracking. |
| **First Contentful Paint (Mobile)** | < 800 ms | Server-side prerendering with static fallback shell. |
| **Image Transfer Overhead** | Reduced by 60%+ | Thumbnail previews + lazy loading off-screen cards. |

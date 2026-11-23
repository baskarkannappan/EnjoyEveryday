using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;
using EnjoyEveryday.Shared.Authorization;

namespace EnjoyEveryday.Daycare.Web.Components.Layout;

public partial class NavMenu
{
    [CascadingParameter(Name = "CloseMobileMenu")]
    public Func<Task>? CloseMobileMenu { get; set; }

    [CascadingParameter(Name = "OpenQuickSearch")]
    public Func<Task>? OpenQuickSearch { get; set; }

    private async Task HandleOpenQuickSearchAsync()
    {
        if (_isDisposed) return;
        await HandleNavItemClickAsync();
        if (OpenQuickSearch is not null)
        {
            await OpenQuickSearch.Invoke();
        }
    }

    private bool _isDisposed = false;

    private async Task HandleNavItemClickAsync()
    {
        if (_isDisposed) return;
        if (CloseMobileMenu is not null)
        {
            try
            {
                await CloseMobileMenu.Invoke();
            }
            catch
            {
                // Ignore if mobile drawer is already closed or closing
            }
        }
    }

    // Expandable Group State (Accordion: only one group expanded at a time; collapsed by default)
    private readonly HashSet<string> _openGroups = new(StringComparer.OrdinalIgnoreCase);

    private bool IsGroupOpen(string groupKey) => _openGroups.Contains(groupKey);

    private void ToggleGroup(string groupKey)
    {
        var wasOpen = _openGroups.Contains(groupKey);
        _openGroups.Clear();
        _openSubmenus.Clear();
        if (!wasOpen)
        {
            _openGroups.Add(groupKey);
        }
    }

    // Expandable Submenu State (Accordion: only one submenu expanded at a time)
    private readonly HashSet<string> _openSubmenus = new(StringComparer.OrdinalIgnoreCase);

    private bool IsSubmenuOpen(string submenuKey) => _openSubmenus.Contains(submenuKey);

    private void ToggleSubmenu(string submenuKey)
    {
        var wasOpen = _openSubmenus.Contains(submenuKey);
        _openSubmenus.Clear();
        if (!wasOpen)
        {
            _openSubmenus.Add(submenuKey);
        }
    }

    private bool IsSubmenuActive(string submenuKey)
    {
        var relativePath = GetNormalizedRelativePath();
        foreach (var group in GetActiveNavGroups())
        {
            foreach (var item in group.Items)
            {
                if (string.Equals(item.SubmenuKey, submenuKey, StringComparison.OrdinalIgnoreCase) && item.SubItems is not null)
                {
                    return item.SubItems.Any(sub => MatchesRoute(sub.Href, relativePath));
                }
            }
        }
        return false;
    }

    private int _chatUnreadCount = 0;

    // Roles and Scopes
    private bool _isOrgAdmin = false;
    private bool _isBranchAdmin = false;
    private bool _isLeadTeacher = false;
    private bool _isTeacher = false;
    private HashSet<Guid> _assignedBranchIds = new();
    private List<string> _availableViews = new();
    private int _availableViewsCount = 0;
    private string _currentNavView = "Teacher";
    private string _userDisplayName = "";

    private bool IsBranchUiLocked() =>
        (_currentNavView == "Teacher" || _currentNavView == "LeadTeacher") && BranchUiGuard.CurrentState.IsBranchClosed;

    private bool IsNavLocked(string href) =>
        (_currentNavView == "Teacher" || _currentNavView == "LeadTeacher") && BranchUiGuard.IsOperationalNavItemLocked(href);


    protected override async Task OnInitializedAsync()
    {
        NavigationManager.LocationChanged += HandleLocationChanged;
        ViewPreference.OnViewChanged += HandlePreferenceViewChanged;
        BranchUiGuard.OnAvailabilityChanged += HandleBranchAvailabilityChangedAsync;
        await ResolveNavigationScopeAsync();
        await BranchUiGuard.GetAvailabilityAsync(forceRefresh: true);
        AutoExpandCurrentRoute();

        NotificationCenter.OnParentMessageReceived += HandleIncomingMessage;
        NotificationCenter.OnMessagesRead += HandleMessagesRead;

        try
        {
            _chatUnreadCount = await CommunicationRepo.GetUnreadCountForEducatorAsync(TenantContext.TenantId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NavMenu] Error loading unread count: {ex.Message}");
        }
    }

    private async Task HandleBranchAvailabilityChangedAsync()
    {
        if (_isDisposed) return;
        try
        {
            await InvokeAsync(() =>
            {
                if (!_isDisposed) StateHasChanged();
            });
        }
        catch (ObjectDisposedException) { }
    }

    private async void HandlePreferenceViewChanged()
    {
        if (_isDisposed) return;
        try
        {
            await ResolveNavigationScopeAsync();
            if (!_isDisposed)
            {
                await InvokeAsync(() =>
                {
                    if (!_isDisposed)
                    {
                        StateHasChanged();
                    }
                });
            }
        }
        catch (ObjectDisposedException)
        {
            // Component or service scope is disposed in mobile view
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NavMenu] HandlePreferenceViewChanged ignored: {ex.Message}");
        }
    }

    private async void HandleLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        if (_isDisposed) return;
        try
        {
            await ResolveNavigationScopeAsync();
            AutoExpandCurrentRoute();
            if (!_isDisposed)
            {
                await InvokeAsync(() =>
                {
                    if (!_isDisposed)
                    {
                        StateHasChanged();
                    }
                });
            }
        }
        catch (ObjectDisposedException)
        {
            // Component or service scope is disposed in mobile view
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NavMenu] HandleLocationChanged ignored: {ex.Message}");
        }
    }

    private async Task ResolveNavigationScopeAsync()
    {
        if (_isDisposed) return;
        try
        {
            var profile = await PageAuth.GetUserProfileAsync();
            if (_isDisposed) return;

            _isOrgAdmin = profile.IsOrgAdmin;
            _isBranchAdmin = profile.IsBranchAdmin;
            _isLeadTeacher = profile.IsLeadTeacher;
            _isTeacher = profile.IsTeacher;
            _assignedBranchIds = profile.AssignedBranchIds;
            _availableViews = profile.AvailableViews;
            _availableViewsCount = _availableViews.Count;
            _userDisplayName = profile.DisplayName;

            // 1. Detect active view from URL query if present and authorized
            var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
            if (Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query).TryGetValue("view", out var v))
            {
                var requested = v.ToString();
                var match = _availableViews.FirstOrDefault(x => x.Equals(requested, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    _currentNavView = match;
                    ViewPreference.SetCurrentView(match);
                    return;
                }
            }

            // If user is OrgAdmin and has no assigned classrooms, default to OrgAdmin navigation view
            if (_isOrgAdmin && profile.AssignedClassroomIds.Count == 0)
            {
                _currentNavView = "OrgAdmin";
                ViewPreference.SetCurrentView(_currentNavView);
                return;
            }

            // 2. Active Session Preference (persisted across menu clicks)
            if (!string.IsNullOrEmpty(ViewPreference.CurrentView))
            {
                var match = _availableViews.FirstOrDefault(x => x.Equals(ViewPreference.CurrentView, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    _currentNavView = match;
                    return;
                }
            }

            // 3. Safe authorized default view (Role Hierarchy fallback)
            if (_isOrgAdmin) _currentNavView = "OrgAdmin";
            else if (_isBranchAdmin) _currentNavView = "BranchAdmin";
            else if (_isLeadTeacher) _currentNavView = "LeadTeacher";
            else _currentNavView = "Teacher";

            ViewPreference.SetCurrentView(_currentNavView);
        }
        catch (ObjectDisposedException)
        {
            // Scope disposed
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NavMenu] Error in ResolveNavigationScopeAsync: {ex.Message}");
            if (!_isDisposed)
            {
                _currentNavView = "OrgAdmin";
            }
        }
    }

    private void HandleSwitchView(ChangeEventArgs e)
    {
        if (_isDisposed) return;
        var targetUrl = e.Value?.ToString() ?? "/";
        var uri = NavigationManager.ToAbsoluteUri(targetUrl);
        if (Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query).TryGetValue("view", out var v))
        {
            var requested = v.ToString();
            var match = _availableViews.FirstOrDefault(x => x.Equals(requested, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                ViewPreference.SetCurrentView(match);
                _currentNavView = match;
            }
        }
        NavigationManager.NavigateTo(targetUrl, forceLoad: false);
    }

    private async Task HandleIncomingMessage(EnjoyEveryday.Application.Services.ParentMessageNotification notification)
    {
        if (_isDisposed) return;
        try
        {
            _chatUnreadCount++;
            if (!_isDisposed)
            {
                await InvokeAsync(() =>
                {
                    if (!_isDisposed)
                    {
                        StateHasChanged();
                    }
                });
            }
        }
        catch (ObjectDisposedException) { }
    }

    private async Task HandleMessagesRead(Guid childId)
    {
        if (_isDisposed) return;
        try
        {
            try
            {
                _chatUnreadCount = await CommunicationRepo.GetUnreadCountForEducatorAsync(TenantContext.TenantId);
            }
            catch
            {
                _chatUnreadCount = Math.Max(0, _chatUnreadCount - 1);
            }

            if (!_isDisposed)
            {
                await InvokeAsync(() =>
                {
                    if (!_isDisposed)
                    {
                        StateHasChanged();
                    }
                });
            }
        }
        catch (ObjectDisposedException) { }
    }

    public void Dispose()
    {
        _isDisposed = true;
        NavigationManager.LocationChanged -= HandleLocationChanged;
        ViewPreference.OnViewChanged -= HandlePreferenceViewChanged;
        BranchUiGuard.OnAvailabilityChanged -= HandleBranchAvailabilityChangedAsync;
        NotificationCenter.OnParentMessageReceived -= HandleIncomingMessage;
        NotificationCenter.OnMessagesRead -= HandleMessagesRead;
    }

    private void AutoExpandCurrentRoute()
    {
        _openGroups.Clear();
        _openSubmenus.Clear();

        var relativePath = GetNormalizedRelativePath();
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            // By default, when opened on dashboard/root (/), keep all groups collapsed.
            return;
        }

        foreach (var group in GetActiveNavGroups())
        {
            foreach (var item in group.Items)
            {
                if (item.SubItems is not null)
                {
                    if (item.SubItems.Any(sub => MatchesRoute(sub.Href, relativePath)))
                    {
                        _openGroups.Add(group.Key);
                        if (!string.IsNullOrEmpty(item.SubmenuKey))
                        {
                            _openSubmenus.Add(item.SubmenuKey);
                        }
                        return;
                    }
                }
                else if (MatchesRoute(item.Href, relativePath))
                {
                    _openGroups.Add(group.Key);
                    return;
                }
            }
        }
    }

    private string GetNormalizedRelativePath()
    {
        var path = NavigationManager.ToBaseRelativePath(NavigationManager.Uri).ToLowerInvariant();
        var queryIndex = path.IndexOf('?');
        return queryIndex >= 0 ? path[..queryIndex] : path;
    }

    private static bool MatchesRoute(string href, string relativePath)
    {
        var cleanHref = href.TrimStart('/').ToLowerInvariant();
        var queryIndex = cleanHref.IndexOf('?');
        if (queryIndex >= 0)
        {
            cleanHref = cleanHref[..queryIndex];
        }

        if (string.IsNullOrEmpty(cleanHref))
        {
            return string.IsNullOrEmpty(relativePath);
        }

        return relativePath == cleanHref || relativePath.StartsWith(cleanHref + "/");
    }

    private int GetBadgeCount(string? badgeKey) => badgeKey == "chat" ? _chatUnreadCount : 0;

    private IReadOnlyList<NavGroupConfig> GetActiveNavGroups() =>
        GetNavGroupsForView(_currentNavView, _isOrgAdmin);

    internal static IReadOnlyList<NavGroupConfig> GetNavGroupsForView(string navView, bool isOrgAdmin) => navView switch
    {
        "Teacher" => TeacherNavGroups,
        "LeadTeacher" => LeadTeacherNavGroups,
        "BranchAdmin" => BranchAdminNavGroups,
        _ => isOrgAdmin
            ? OrgAdminNavGroups
            : OrgAdminNavGroups
                .Select(g => g with { Items = g.Items.Where(i => !i.RequiresOrgAdmin).ToList() })
                .ToList()
    };

    internal sealed record NavItemConfig(
        string Href,
        string Text,
        Icon? Icon = null,
        string? CustomColor = null,
        bool MatchAll = false,
        bool CheckBranchLock = false,
        bool RequiresOrgAdmin = false,
        string? BadgeKey = null,
        string? SubmenuKey = null,
        IReadOnlyList<NavItemConfig>? SubItems = null);

    internal sealed record NavGroupConfig(
        string Key,
        string Title,
        Icon Icon,
        string Color,
        IReadOnlyList<NavItemConfig> Items);

    private static readonly IReadOnlyList<NavGroupConfig> TeacherNavGroups =
    [
        new("t_workspace", "Today", new Icons.Regular.Size20.WeatherSunny(), "#b47836",
        [
            new("/", "My Dashboard", new Icons.Regular.Size20.WeatherSunny(), "#31523c", MatchAll: true, CheckBranchLock: true),
            new("/today-activities", "Today's Schedule", new Icons.Regular.Size20.CalendarMonth(), "#d97706", CheckBranchLock: true),
            new("/notifications", "Notifications", new Icons.Regular.Size20.Alert(), "#f59e0b")
        ]),
        new("t_classroom", "Classroom & Care", new Icons.Regular.Size20.PeopleCommunity(), "#1b7a54",
        [
            new("/children", "Children", new Icons.Regular.Size20.PersonAvailable(), "#059669"),
            new("/management/attendance", "Attendance", new Icons.Regular.Size20.CheckmarkCircle(), "#1b7a54", MatchAll: true, CheckBranchLock: true),
            new("/daily-care", "Daily Care", new Icons.Regular.Size20.ClipboardTask(), "#e11d48", CheckBranchLock: true),
            new("/daily-care/plans", "Care Plans", new Icons.Regular.Size20.Heart(), "#ea580c"),
            new("/daily-care/supplies", "Supplies", new Icons.Regular.Size20.ClipboardTask(), "#b45309", CheckBranchLock: true)
        ]),
        new("t_activities", "Learning", new Icons.Regular.Size20.Sparkle(), "#c06c3a",
        [
            new("/experiences", "Experiences", new Icons.Regular.Size20.Library(), "#0891b2"),
            new("/harvest", "Harvest", new Icons.Regular.Size20.Sparkle(), "#059669", CheckBranchLock: true),
            new("/journey", "Child Journey", new Icons.Regular.Size20.LeafOne(), "#16a34a", CheckBranchLock: true),
            new("/memories", "Memories", new Icons.Regular.Size20.Camera(), "#db2777"),
            new("/events", "Events", new Icons.Regular.Size20.CalendarMonth(), "#8b5cf6", CheckBranchLock: true)
        ]),
        new("t_care", "Comms & Safety", new Icons.Regular.Size20.Chat(), "#1e6091",
        [
            new("/classroom-messages", "Parent Chat", new Icons.Regular.Size20.Chat(), "#31523c", BadgeKey: "chat"),
            new("/announcements", "Announcements", new Icons.Regular.Size20.Megaphone(), "#ea580c"),
            new("", "Safety & Emergency", new Icons.Regular.Size20.Shield(), "#dc2626", SubmenuKey: "t_safety", SubItems:
            [
                new("/safety-trust/floor-maps", "Floor Maps"),
                new("/safety-trust/emergency-contacts", "Emergency Contacts")
            ])
        ]),
        new("t_account", "Account", new Icons.Regular.Size20.Person(), "#5c4a72",
        [
            new("/profile", "My Profile", new Icons.Regular.Size20.Person(), "#6366f1"),
            new("/organization/branch-info", "Campus Info", new Icons.Regular.Size20.Building(), "#52745e"),
            new("/support", "Help & Support", new Icons.Regular.Size20.QuestionCircle(), "#0ea5e9"),
            new("/version", "Version", new Icons.Regular.Size20.Info(), "#52745e")
        ])
    ];

    private static readonly IReadOnlyList<NavGroupConfig> LeadTeacherNavGroups =
    [
        new("lt_workspace", "Today", new Icons.Regular.Size20.WeatherSunny(), "#b47836",
        [
            new("/", "My Dashboard", new Icons.Regular.Size20.WeatherSunny(), "#31523c", MatchAll: true, CheckBranchLock: true),
            new("/today-activities", "Today's Schedule", new Icons.Regular.Size20.CalendarMonth(), "#d97706", CheckBranchLock: true),
            new("/notifications", "Notifications", new Icons.Regular.Size20.Alert(), "#f59e0b")
        ]),
        new("lt_care", "Classroom & Care", new Icons.Regular.Size20.PeopleCommunity(), "#1b7a54",
        [
            new("/children", "Children", new Icons.Regular.Size20.PersonAvailable(), "#059669"),
            new("/management/attendance", "Attendance", new Icons.Regular.Size20.CheckmarkCircle(), "#1b7a54", MatchAll: true, CheckBranchLock: true),
            new("/daily-care", "Daily Care & Handover", new Icons.Regular.Size20.ClipboardTask(), "#e11d48", CheckBranchLock: true),
            new("/daily-care/plans", "Care Plans", new Icons.Regular.Size20.Heart(), "#ea580c"),
            new("/daily-care/supplies", "Supplies", new Icons.Regular.Size20.ClipboardTask(), "#b45309", CheckBranchLock: true)
        ]),
        new("lt_classroom", "Lead & Roster", new Icons.Regular.Size20.PeopleTeam(), "#2b5c8f",
        [
            new("/classrooms", "My Classrooms", new Icons.Regular.Size20.Building(), "#d97706"),
            new("/management/roster", "Staff Coverage", new Icons.Regular.Size20.PersonCall(), "#dc2626")
        ]),
        new("lt_activities", "Learning", new Icons.Regular.Size20.Sparkle(), "#c06c3a",
        [
            new("/experiences", "Experiences", new Icons.Regular.Size20.Library(), "#0891b2"),
            new("/marketplace", "Marketplace", new Icons.Regular.Size20.Sparkle(), "#059669"),
            new("/studio", "Studio", new Icons.Regular.Size20.Sparkle(), "#9333ea"),
            new("/planning", "Planning", new Icons.Regular.Size20.CalendarLtr(), "#b47836", CheckBranchLock: true),
            new("/harvest", "Harvest", new Icons.Regular.Size20.Sparkle(), "#d97706", CheckBranchLock: true),
            new("/materials-marketplace", "Materials", new Icons.Regular.Size20.Receipt(), "#c2410c"),
            new("/journey", "Child Journey", new Icons.Regular.Size20.LeafOne(), "#16a34a", CheckBranchLock: true),
            new("/memories", "Memories", new Icons.Regular.Size20.Camera(), "#db2777"),
            new("/insights", "Progress & Insights", new Icons.Regular.Size20.BrainCircuit(), "#7c3aed")
        ]),
        new("lt_family", "Comms & Safety", new Icons.Regular.Size20.Chat(), "#1e6091",
        [
            new("/classroom-messages", "Parent Chat", new Icons.Regular.Size20.Chat(), "#31523c", BadgeKey: "chat"),
            new("/announcements", "Announcements", new Icons.Regular.Size20.Megaphone(), "#ea580c"),
            new("", "Safety & Emergency", new Icons.Regular.Size20.Shield(), "#dc2626", SubmenuKey: "lt_safety", SubItems:
            [
                new("/safety-trust/floor-maps", "Floor Maps"),
                new("/safety-trust/emergency-contacts", "Emergency Contacts")
            ])
        ]),
        new("lt_account", "Account", new Icons.Regular.Size20.Person(), "#5c4a72",
        [
            new("/profile", "My Profile", new Icons.Regular.Size20.Person(), "#6366f1"),
            new("/organization/branch-info", "Campus Info", new Icons.Regular.Size20.Building(), "#52745e"),
            new("/support", "Help & Support", new Icons.Regular.Size20.QuestionCircle(), "#0ea5e9"),
            new("/version", "Version", new Icons.Regular.Size20.Info(), "#52745e")
        ])
    ];

    private static readonly IReadOnlyList<NavGroupConfig> BranchAdminNavGroups =
    [
        new("ba_workspace", "Today", new Icons.Regular.Size20.Board(), "#31523c",
        [
            new("/", "Branch Dashboard", new Icons.Regular.Size20.Board(), "#31523c", MatchAll: true),
            new("/today-activities", "Today's Live", new Icons.Regular.Size20.WeatherSunny(), "#d97706"),
            new("/notifications", "Notifications", new Icons.Regular.Size20.Alert(), "#f59e0b")
        ]),
        new("ba_branch", "Branch & Staff", new Icons.Regular.Size20.Building(), "#31523c",
        [
            new("/classrooms", "Classrooms", new Icons.Regular.Size20.Building(), "#d97706"),
            new("/management/roster", "Staff Roster", new Icons.Regular.Size20.PersonCall(), "#dc2626"),
            new("/teachers", "Teachers", new Icons.Regular.Size20.Person(), "#4f46e5"),
            new("/users-by-role", "Role Assignments", new Icons.Regular.Size20.PeopleTeam(), "#4f46e5"),
            new("/organization/branches", "Branch Settings", new Icons.Regular.Size20.Settings(), "#4b5563"),
            new("/organization/profile-customizer", "Customize Profile", new Icons.Regular.Size20.Edit(), "#52745e"),
            new("/organization/branch-info", "Campus Info", new Icons.Regular.Size20.Building(), "#059669")
        ]),
        new("ba_people", "People & Admissions", new Icons.Regular.Size20.PeopleTeam(), "#2b5c8f",
        [
            new("/children", "Children", new Icons.Regular.Size20.PersonAvailable(), "#059669"),
            new("/organization/inquiries", "Inquiries", new Icons.Regular.Size20.Mail(), "#31523c"),
            new("", "Admissions", new Icons.Regular.Size20.CalendarAdd(), "#0284c7", SubmenuKey: "ba_admissions", SubItems:
            [
                new("/enrollment?action=new-inquiry", "New Inquiry"),
                new("/enrollment", "Pipeline"),
                new("/children", "Enrolled")
            ])
        ]),
        new("ba_ops", "Operations", new Icons.Regular.Size20.CalendarClock(), "#b85d43",
        [
            new("/management/attendance", "Attendance", new Icons.Regular.Size20.CheckmarkCircle(), "#1b7a54", MatchAll: true),
            new("/attendance/monthly-children", "Child Register", new Icons.Regular.Size20.CalendarMonth(), "#0284c7"),
            new("/attendance/monthly-teachers", "Staff Register", new Icons.Regular.Size20.PeopleCommunity(), "#6366f1"),
            new("/daily-care", "Daily Care", new Icons.Regular.Size20.Heart(), "#e11d48"),
            new("/daily-care/plans", "Care Plans", new Icons.Regular.Size20.Heart(), "#ea580c"),
            new("/daily-care/supplies", "Supplies", new Icons.Regular.Size20.ClipboardTask(), "#b45309"),
            new("/events", "Events", new Icons.Regular.Size20.CalendarMonth(), "#8b5cf6"),
            new("", "Safety & Compliance", new Icons.Regular.Size20.ShieldCheckmark(), "#059669", SubmenuKey: "ba_safety", SubItems:
            [
                new("/safety-compliance", "Compliance"),
                new("/safety-trust/floor-maps", "Floor Maps"),
                new("/safety-trust/emergency-contacts", "Emergency Contacts")
            ])
        ]),
        new("ba_exp", "Learning & Experiences", new Icons.Regular.Size20.Sparkle(), "#c06c3a",
        [
            new("/experiences", "Experiences", new Icons.Regular.Size20.Library(), "#0891b2"),
            new("/marketplace", "Marketplace", new Icons.Regular.Size20.Sparkle(), "#059669"),
            new("/studio", "Studio", new Icons.Regular.Size20.Sparkle(), "#9333ea"),
            new("/planning", "Planning", new Icons.Regular.Size20.CalendarLtr(), "#b47836"),
            new("/harvest", "Harvest", new Icons.Regular.Size20.Sparkle(), "#d97706"),
            new("/journey", "Child Journey", new Icons.Regular.Size20.LeafOne(), "#16a34a", CheckBranchLock: true),
            new("/memories", "Memories", new Icons.Regular.Size20.Camera(), "#db2777"),
            new("/materials-marketplace", "Materials", new Icons.Regular.Size20.Receipt(), "#c2410c"),
            new("/management/gifting-orders", "Gifting Orders", new Icons.Regular.Size20.Gift(), "#d97706"),
            new("/management/gifting-reports", "Gifting Reports", new Icons.Regular.Size20.ChartMultiple(), "#2563eb")
        ]),
        new("ba_family", "Comms", new Icons.Regular.Size20.Chat(), "#1e6091",
        [
            new("/classroom-messages", "Parent Chat", new Icons.Regular.Size20.Chat(), "#31523c", BadgeKey: "chat"),
            new("/announcements", "Announcements", new Icons.Regular.Size20.Megaphone(), "#ea580c")
        ]),
        new("ba_reports", "Reports & Account", new Icons.Regular.Size20.BrainCircuit(), "#7c3aed",
        [
            new("/insights", "Branch Reports", new Icons.Regular.Size20.BrainCircuit(), "#7c3aed"),
            new("/attendance/monthly", "Monthly Attendance", new Icons.Regular.Size20.CalendarMonth(), "#0284c7"),
            new("/profile", "My Profile", new Icons.Regular.Size20.Person(), "#6366f1"),
            new("/support", "Help & Support", new Icons.Regular.Size20.QuestionCircle(), "#0ea5e9"),
            new("/version", "Version", new Icons.Regular.Size20.Info(), "#52745e")
        ])
    ];

    private static readonly IReadOnlyList<NavGroupConfig> OrgAdminNavGroups =
    [
        new("org_workspace", "Today", new Icons.Regular.Size20.Board(), "#31523c",
        [
            new("/", "Dashboard", new Icons.Regular.Size20.Board(), "#31523c", MatchAll: true),
            new("/insights", "Org Overview", new Icons.Regular.Size20.BrainCircuit(), "#7c3aed"),
            new("/notifications", "Notifications", new Icons.Regular.Size20.Alert(), "#f59e0b")
        ]),
        new("org_mgmt", "Org", new Icons.Regular.Size20.Building(), "#2b5c8f",
        [
            new("/organization/profile", "Org Profile", new Icons.Regular.Size20.Building(), "#31523c"),
            new("", "Branches", new Icons.Regular.Size20.Building(), "#4f46e5", SubmenuKey: "org_branches", SubItems:
            [
                new("/organization/branches", "All Branches"),
                new("/management/branches/new", "New Branch")
            ]),
            new("", "Staff & Roles", new Icons.Regular.Size20.PeopleTeam(), "#4f46e5", SubmenuKey: "org_staff", SubItems:
            [
                new("/users-by-role", "All Staff"),
                new("/teachers", "Teachers"),
                new("/management/roster", "Coverage Roster")
            ]),
            new("/roles", "Role Matrix", new Icons.Regular.Size20.Key(), "#b45309")
        ]),
        new("org_people", "People & Admissions", new Icons.Regular.Size20.PersonAvailable(), "#059669",
        [
            new("/children", "Children & Families", new Icons.Regular.Size20.PersonAvailable(), "#059669"),
            new("/organization/inquiries", "Inquiries", new Icons.Regular.Size20.Mail(), "#31523c"),
            new("", "Admissions", new Icons.Regular.Size20.CalendarAdd(), "#0284c7", SubmenuKey: "org_admissions", SubItems:
            [
                new("/enrollment?action=new-inquiry", "New Inquiry"),
                new("/enrollment", "Pipeline"),
                new("/children", "Enrolled")
            ])
        ]),
        new("org_ops", "Operations", new Icons.Regular.Size20.CalendarClock(), "#b85d43",
        [
            new("/classrooms", "Classrooms", new Icons.Regular.Size20.Board(), "#d97706"),
            new("/management/attendance", "Attendance", new Icons.Regular.Size20.CheckmarkCircle(), "#1b7a54", MatchAll: true),
            new("/attendance/monthly-children", "Child Register", new Icons.Regular.Size20.CalendarMonth(), "#0284c7"),
            new("/attendance/monthly-teachers", "Staff Register", new Icons.Regular.Size20.PeopleCommunity(), "#6366f1"),
            new("/daily-care/plans", "Care Plans", new Icons.Regular.Size20.Heart(), "#ea580c"),
            new("/daily-care/supplies", "Supplies", new Icons.Regular.Size20.ClipboardTask(), "#b45309"),
            new("/events", "Events", new Icons.Regular.Size20.CalendarMonth(), "#8b5cf6")
        ]),
        new("org_exp", "Learning & Experiences", new Icons.Regular.Size20.Sparkle(), "#c06c3a",
        [
            new("/experiences", "Experiences", new Icons.Regular.Size20.Library(), "#0891b2"),
            new("/marketplace", "Marketplace", new Icons.Regular.Size20.Sparkle(), "#059669"),
            new("/studio", "Studio", new Icons.Regular.Size20.Sparkle(), "#9333ea"),
            new("/planning", "Planning", new Icons.Regular.Size20.CalendarMonth(), "#b47836"),
            new("/today-activities", "Live Execution", new Icons.Regular.Size20.WeatherSunny(), "#d97706"),
            new("/harvest", "Harvest", new Icons.Regular.Size20.Sparkle(), "#d97706"),
            new("/journey", "Child Journey", new Icons.Regular.Size20.LeafOne(), "#16a34a"),
            new("/memories", "Memories", new Icons.Regular.Size20.Camera(), "#db2777"),
            new("/materials-marketplace", "Materials", new Icons.Regular.Size20.Receipt(), "#c2410c"),
            new("/management/gifting-orders", "Gifting Orders", new Icons.Regular.Size20.Gift(), "#d97706"),
            new("/management/gifting-reports", "Gifting Reports", new Icons.Regular.Size20.ChartMultiple(), "#2563eb")
        ]),
        new("org_family", "Comms", new Icons.Regular.Size20.Chat(), "#1e6091",
        [
            new("/classroom-messages", "Parent Chat", new Icons.Regular.Size20.Chat(), "#31523c", BadgeKey: "chat"),
            new("/announcements", "Announcements", new Icons.Regular.Size20.Megaphone(), "#ea580c"),
            new("/stories", "Family Stories", new Icons.Regular.Size20.Chat(), "#0284c7")
        ]),
        new("org_business", "Billing & Settings", new Icons.Regular.Size20.Receipt(), "#b47836",
        [
            new("/management/billing", "Parent Billing", new Icons.Regular.Size20.Receipt(), "#059669"),
            new("/management/fee-reports", "Fee Reports", new Icons.Regular.Size20.DataTrending(), "#059669"),
            new("/management/payment-gateway", "Payment Gateway", new Icons.Regular.Size20.Payment(), "#31523c", RequiresOrgAdmin: true),
            new("/management/platform-billing", "Platform Billing", new Icons.Regular.Size20.Building(), "#52745e", RequiresOrgAdmin: true),
            new("/management/subscription", "Subscription", new Icons.Regular.Size20.Ribbon(), "#31523c", RequiresOrgAdmin: true),
            new("/billing/reports", "Financial Reports", new Icons.Regular.Size20.ChartMultiple(), "#0891b2"),
            new("/audit", "Audit Book", new Icons.Regular.Size20.Book(), "#475569"),
            new("/organization/settings", "Org Settings", new Icons.Regular.Size20.Settings(), "#4b5563"),
            new("/organization/profile-customizer", "Customize Profile", new Icons.Regular.Size20.Edit(), "#52745e"),
            new("/organization/branch-info", "Campus Info", new Icons.Regular.Size20.Building(), "#059669"),
            new("/organization/storage-metrics", "Storage & Media", new Icons.Regular.Size20.Storage(), "#0d9488"),
            new("/settings/ai-agents", "AI Agents", new Icons.Regular.Size20.Bot(), "#7c3aed"),
            new("/profile", "My Profile", new Icons.Regular.Size20.Person(), "#6366f1"),
            new("/support", "Help & Support", new Icons.Regular.Size20.QuestionCircle(), "#0ea5e9"),
            new("/version", "Version", new Icons.Regular.Size20.Info(), "#52745e")
        ])
    ];
}

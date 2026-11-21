using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using EnjoyEveryday.Shared.Authorization;

namespace EnjoyEveryday.Daycare.Web.Components.Layout;

public partial class NavMenu
{
    [CascadingParameter(Name = "CloseMobileMenu")]
    public Func<Task>? CloseMobileMenu { get; set; }

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
        var relativePath = NavigationManager.ToBaseRelativePath(NavigationManager.Uri).ToLowerInvariant();
        return submenuKey switch
        {
            "org_branches" => relativePath.StartsWith("organization/branches") || relativePath.StartsWith("management/branches"),
            "org_staff" => relativePath.StartsWith("users-by-role") || relativePath.StartsWith("teachers") || relativePath.StartsWith("management/roster"),
            "org_admissions" or "ba_admissions" => relativePath.StartsWith("enrollment"),
            "ba_safety" or "lt_safety" or "t_safety" => relativePath.StartsWith("safety"),
            _ => false
        };
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

        var relativePath = NavigationManager.ToBaseRelativePath(NavigationManager.Uri).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            // By default, when opened on dashboard/root (/), keep all groups collapsed!
            return;
        }

        // When navigating to a specific sub-page, auto-expand its parent group and submenu
        if (_currentNavView == "Teacher")
        {
            if (relativePath.StartsWith("today-activities")) _openGroups.Add("t_workspace");
            else if (relativePath.StartsWith("children") || relativePath.StartsWith("events") || relativePath.StartsWith("management/attendance")) _openGroups.Add("t_classroom");
            else if (relativePath.StartsWith("daily-care")) _openGroups.Add("t_care");
            else if (relativePath.StartsWith("experiences") || relativePath.StartsWith("harvest")) _openGroups.Add("t_activities");
            else if (relativePath.StartsWith("journey") || relativePath.StartsWith("memories")) _openGroups.Add("t_dev");
            else if (relativePath.StartsWith("classroom-messages") || relativePath.StartsWith("announcements") || relativePath.StartsWith("safety") || relativePath.StartsWith("profile") || relativePath.StartsWith("support"))
            {
                _openGroups.Add("t_account");
                if (relativePath.StartsWith("safety")) _openSubmenus.Add("t_safety");
            }
        }
        else if (_currentNavView == "LeadTeacher")
        {
            if (relativePath.StartsWith("today-activities")) _openGroups.Add("lt_workspace");
            else if (relativePath.StartsWith("classrooms") || relativePath.StartsWith("management/roster")) _openGroups.Add("lt_classroom");
            else if (relativePath.StartsWith("children") || relativePath.StartsWith("daily-care") || relativePath.StartsWith("management/attendance")) _openGroups.Add("lt_care");
            else if (relativePath.StartsWith("experiences") || relativePath.StartsWith("marketplace") || relativePath.StartsWith("studio") || relativePath.StartsWith("planning") || relativePath.StartsWith("harvest") || relativePath.StartsWith("materials")) _openGroups.Add("lt_activities");
            else if (relativePath.StartsWith("journey") || relativePath.StartsWith("memories") || relativePath.StartsWith("insights")) _openGroups.Add("lt_dev");
            else if (relativePath.StartsWith("classroom-messages") || relativePath.StartsWith("announcements")) _openGroups.Add("lt_family");
            else if (relativePath.StartsWith("safety") || relativePath.StartsWith("profile") || relativePath.StartsWith("support"))
            {
                _openGroups.Add("lt_account");
                if (relativePath.StartsWith("safety")) _openSubmenus.Add("lt_safety");
            }
        }
        else if (_currentNavView == "BranchAdmin")
        {
            if (relativePath.StartsWith("today-activities")) _openGroups.Add("ba_workspace");
            else if (relativePath.StartsWith("organization/branches") || relativePath.StartsWith("classrooms") || relativePath.StartsWith("management/roster") || relativePath.StartsWith("users-by-role")) _openGroups.Add("ba_branch");
            else if (relativePath.StartsWith("children") || relativePath.StartsWith("enrollment") || relativePath.StartsWith("teachers"))
            {
                _openGroups.Add("ba_people");
                if (relativePath.StartsWith("enrollment")) _openSubmenus.Add("ba_admissions");
            }
            else if (relativePath.StartsWith("attendance") || relativePath.StartsWith("management/attendance") || relativePath.StartsWith("daily-care") || relativePath.StartsWith("events") || relativePath.StartsWith("safety"))
            {
                _openGroups.Add("ba_ops");
                if (relativePath.StartsWith("safety")) _openSubmenus.Add("ba_safety");
            }
            else if (relativePath.StartsWith("experiences") || relativePath.StartsWith("marketplace") || relativePath.StartsWith("studio") || relativePath.StartsWith("planning") || relativePath.StartsWith("harvest") || relativePath.StartsWith("materials")) _openGroups.Add("ba_exp");
            else if (relativePath.StartsWith("journey") || relativePath.StartsWith("memories")) _openGroups.Add("ba_dev");
            else if (relativePath.StartsWith("classroom-messages") || relativePath.StartsWith("announcements")) _openGroups.Add("ba_family");
            else if (relativePath.StartsWith("insights") || relativePath.StartsWith("attendance/monthly") || relativePath.StartsWith("profile") || relativePath.StartsWith("support")) _openGroups.Add("ba_reports");
        }
        else // OrgAdmin
        {
            if (relativePath.StartsWith("insights") && !relativePath.StartsWith("insights/child")) _openGroups.Add("org_workspace");
            else if (relativePath.StartsWith("organization/profile") || relativePath.StartsWith("organization/branches") || relativePath.StartsWith("management/branches") || relativePath.StartsWith("users-by-role") || relativePath.StartsWith("teachers") || relativePath.StartsWith("roles") || relativePath.StartsWith("management/roster"))
            {
                _openGroups.Add("org_mgmt");
                if (relativePath.StartsWith("organization/branches") || relativePath.StartsWith("management/branches")) _openSubmenus.Add("org_branches");
                if (relativePath.StartsWith("users-by-role") || relativePath.StartsWith("teachers") || relativePath.StartsWith("management/roster")) _openSubmenus.Add("org_staff");
            }
            else if (relativePath.StartsWith("children") || relativePath.StartsWith("enrollment") || relativePath.StartsWith("classroom-messages"))
            {
                _openGroups.Add("org_people");
                if (relativePath.StartsWith("enrollment")) _openSubmenus.Add("org_admissions");
            }
            else if (relativePath.StartsWith("classrooms") || relativePath.StartsWith("management/attendance") || relativePath.StartsWith("attendance/monthly") || relativePath.StartsWith("daily-care") || relativePath.StartsWith("events")) _openGroups.Add("org_ops");
            else if (relativePath.StartsWith("experiences") || relativePath.StartsWith("marketplace") || relativePath.StartsWith("studio") || relativePath.StartsWith("planning") || relativePath.StartsWith("today-activities") || relativePath.StartsWith("harvest") || relativePath.StartsWith("materials")) _openGroups.Add("org_exp");
            else if (relativePath.StartsWith("journey") || relativePath.StartsWith("memories")) _openGroups.Add("org_dev");
            else if (relativePath.StartsWith("announcements") || relativePath.StartsWith("stories")) _openGroups.Add("org_family");
            else if (relativePath.StartsWith("billing") || relativePath.StartsWith("management/billing") || relativePath.StartsWith("management/fee-reports") || relativePath.StartsWith("management/subscription") || relativePath.StartsWith("audit") || relativePath.StartsWith("organization/settings") || relativePath.StartsWith("settings") || relativePath.StartsWith("profile") || relativePath.StartsWith("support")) _openGroups.Add("org_business");
        }
    }
}

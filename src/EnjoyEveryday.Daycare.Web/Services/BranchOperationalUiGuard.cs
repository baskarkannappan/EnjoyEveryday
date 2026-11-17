using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Daycare.Web.Services;

public sealed class BranchUiAvailabilityResult
{
    public bool IsLoaded { get; init; }
    public bool IsBranchResolved { get; init; }
    public bool IsOperational { get; init; } = true;
    public bool IsDailyOpened { get; init; } = true;
    public bool IsBranchClosed { get; init; }
    public bool IsRouteBlocked { get; init; }
    public BranchRouteCategory RouteCategory { get; init; } = BranchRouteCategory.ReadOnly;
    public Guid OrganizationId { get; init; }
    public string OrganizationName { get; init; } = string.Empty;
    public Guid BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public string OperationalStatus { get; init; } = "Open";
    public string? ClosureReason { get; init; }
    public DateTimeOffset? ReopeningDate { get; init; }
    public string ActiveView { get; init; } = "Teacher";
}

public interface IBranchOperationalUiGuard
{
    BranchUiAvailabilityResult CurrentState { get; }
    event Func<Task>? OnAvailabilityChanged;

    Task<BranchUiAvailabilityResult> GetAvailabilityAsync(bool forceRefresh = false, CancellationToken ct = default);
    Task<BranchUiAvailabilityResult> EvaluateRouteAsync(string? relativePath, bool forceRefresh = false, CancellationToken ct = default);
    bool IsOperationalNavItemLocked(string href);
}

public sealed class BranchOperationalUiGuard : IBranchOperationalUiGuard, IDisposable
{
    private readonly IPageAuthorizationService _pageAuth;
    private readonly IUserViewPreferenceService _viewPreference;
    private readonly IBranchOperationalStatusService _branchOpService;
    private readonly IDailyOperationsService _dailyOpsService;
    private readonly BranchService _branchService;
    private readonly ClassroomService _classroomService;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly DaycareRealtimeNotificationCenter _notificationCenter;
    private readonly ILogger<BranchOperationalUiGuard> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly PeriodicTimer _pollTimer;
    private readonly CancellationTokenSource _disposeCts = new();

    private BranchUiAvailabilityResult _currentState = new();
    private DateTimeOffset _lastLoadedUtc = DateTimeOffset.MinValue;
    private bool _disposed;

    public BranchUiAvailabilityResult CurrentState => _currentState;
    public event Func<Task>? OnAvailabilityChanged;

    public BranchOperationalUiGuard(
        IPageAuthorizationService pageAuth,
        IUserViewPreferenceService viewPreference,
        IBranchOperationalStatusService branchOpService,
        IDailyOperationsService dailyOpsService,
        BranchService branchService,
        ClassroomService classroomService,
        ITenantContext tenantContext,
        IUserContext userContext,
        DaycareRealtimeNotificationCenter notificationCenter,
        ILogger<BranchOperationalUiGuard> logger)
    {
        _pageAuth = pageAuth;
        _viewPreference = viewPreference;
        _branchOpService = branchOpService;
        _dailyOpsService = dailyOpsService;
        _branchService = branchService;
        _classroomService = classroomService;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _notificationCenter = notificationCenter;
        _logger = logger;

        _notificationCenter.OnBranchStatusChanged += HandleBranchStatusChangedAsync;
        _viewPreference.OnBranchChanged += HandlePreferenceChanged;
        _viewPreference.OnViewChanged += HandlePreferenceChanged;

        _pollTimer = new PeriodicTimer(TimeSpan.FromSeconds(120));
        _ = RunBackgroundRevalidationAsync();
    }

    private async Task RunBackgroundRevalidationAsync()
    {
        try
        {
            while (await _pollTimer.WaitForNextTickAsync(_disposeCts.Token))
            {
                if (_disposed || !_userContext.IsAuthenticated) continue;
                var prevClosed = _currentState.IsBranchClosed;
                var prevBranchId = _currentState.BranchId;
                var updated = await GetAvailabilityAsync(forceRefresh: true, _disposeCts.Token);
                if (updated.IsBranchClosed != prevClosed || updated.BranchId != prevBranchId)
                {
                    await NotifySubscribersAsync();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal on dispose
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Background branch status revalidation skipped");
        }
    }

    private async Task HandleBranchStatusChangedAsync(BranchStatusChangedNotification notification)
    {
        if (_disposed) return;
        if (_currentState.BranchId == Guid.Empty || notification.BranchId == Guid.Empty || notification.BranchId == _currentState.BranchId)
        {
            await GetAvailabilityAsync(forceRefresh: true);
            await NotifySubscribersAsync();
        }
    }

    private async void HandlePreferenceChanged()
    {
        if (_disposed) return;
        try
        {
            await GetAvailabilityAsync(forceRefresh: true);
            await NotifySubscribersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error handling view/branch preference change");
        }
    }

    private async Task NotifySubscribersAsync()
    {
        if (OnAvailabilityChanged == null) return;
        foreach (Func<Task> handler in OnAvailabilityChanged.GetInvocationList())
        {
            try
            {
                await handler();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error notifying BranchOperationalUiGuard subscriber");
            }
        }
    }

    public async Task<BranchUiAvailabilityResult> GetAvailabilityAsync(bool forceRefresh = false, CancellationToken ct = default)
    {
        if (_disposed) return _currentState;
        var currentPrefView = _viewPreference.CurrentView;
        if (!forceRefresh && _currentState.IsLoaded
            && (DateTimeOffset.UtcNow - _lastLoadedUtc) < TimeSpan.FromSeconds(15)
            && (string.IsNullOrWhiteSpace(currentPrefView) || string.Equals(currentPrefView, _currentState.ActiveView, StringComparison.OrdinalIgnoreCase))
            && (!_viewPreference.ActiveBranchId.HasValue || _viewPreference.ActiveBranchId.Value == _currentState.BranchId))
        {
            return _currentState;
        }

        await _lock.WaitAsync(ct);
        try
        {
            currentPrefView = _viewPreference.CurrentView;
            if (!forceRefresh && _currentState.IsLoaded
                && (DateTimeOffset.UtcNow - _lastLoadedUtc) < TimeSpan.FromSeconds(15)
                && (string.IsNullOrWhiteSpace(currentPrefView) || string.Equals(currentPrefView, _currentState.ActiveView, StringComparison.OrdinalIgnoreCase))
                && (!_viewPreference.ActiveBranchId.HasValue || _viewPreference.ActiveBranchId.Value == _currentState.BranchId))
            {
                return _currentState;
            }

            var profile = await _pageAuth.GetUserProfileAsync(ct);
            if (!profile.IsAuthenticated || _tenantContext.TenantId == Guid.Empty)
            {
                _currentState = new BranchUiAvailabilityResult { IsLoaded = true, IsBranchClosed = false };
                _lastLoadedUtc = DateTimeOffset.UtcNow;
                return _currentState;
            }

            var activeView = ResolveActiveView(profile);
            var (branchId, branchName) = await ResolveActiveBranchAsync(profile, ct);

            if (branchId == Guid.Empty)
            {
                _currentState = new BranchUiAvailabilityResult
                {
                    IsLoaded = true,
                    IsBranchResolved = false,
                    IsBranchClosed = false,
                    OrganizationId = profile.OrganizationId,
                    OrganizationName = profile.OrganizationName,
                    ActiveView = activeView
                };
                _lastLoadedUtc = DateTimeOffset.UtcNow;
                return _currentState;
            }

            var opStatusTask = _branchOpService.GetBranchStatusAsync(_tenantContext.TenantId, branchId, null, ct);
            var dailyStatusTask = _dailyOpsService.GetBranchDailyStatusAsync(branchId, null, ct);
            await Task.WhenAll(opStatusTask, dailyStatusTask);
            var opStatus = opStatusTask.Result;
            var dailyStatus = dailyStatusTask.Result;
            var bypassed = _dailyOpsService.IsBypassedForTesting();

            var isOperational = opStatus.IsOperational;
            var isDailyOpened = dailyStatus?.IsOpen ?? true;
            var isClosed = !isOperational || (!bypassed && !isDailyOpened);

            _currentState = new BranchUiAvailabilityResult
            {
                IsLoaded = true,
                IsBranchResolved = true,
                IsOperational = isOperational,
                IsDailyOpened = isDailyOpened,
                IsBranchClosed = isClosed,
                OrganizationId = profile.OrganizationId,
                OrganizationName = profile.OrganizationName,
                BranchId = branchId,
                BranchName = string.IsNullOrWhiteSpace(opStatus.BranchName) ? branchName : opStatus.BranchName,
                OperationalStatus = !isOperational ? opStatus.OperationalStatus : (!isDailyOpened ? "Closed (Facility Not Opened)" : "Open"),
                ClosureReason = opStatus.ActiveClosure?.Reason ?? (!isDailyOpened ? "The Branch Admin has not opened the facility for operations today." : null),
                ReopeningDate = opStatus.ActiveClosure?.EffectiveEndAt,
                ActiveView = activeView
            };
            _lastLoadedUtc = DateTimeOffset.UtcNow;
            return _currentState;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to evaluate branch operational availability");
            return _currentState;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<BranchUiAvailabilityResult> EvaluateRouteAsync(string? relativePath, bool forceRefresh = false, CancellationToken ct = default)
    {
        var state = await GetAvailabilityAsync(forceRefresh, ct);
        var profile = await _pageAuth.GetUserProfileAsync(ct);

        var category = BranchRoutePolicy.ClassifyRoute(relativePath, state.ActiveView);
        bool blocked = false;

        if (state.IsBranchClosed && category == BranchRouteCategory.Operational)
        {
            // PlatformAdmin and OrgAdmin in OrgAdmin view retain access so they can switch between open/closed branches
            var isOrgAdminView = profile.IsOrgAdmin && string.Equals(state.ActiveView, "OrgAdmin", StringComparison.OrdinalIgnoreCase);
            if (!profile.IsPlatformAdmin && !isOrgAdminView)
            {
                var isTeacherView = string.Equals(state.ActiveView, "Teacher", StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(state.ActiveView, "LeadTeacher", StringComparison.OrdinalIgnoreCase);
                var isBranchAdminView = string.Equals(state.ActiveView, "BranchAdmin", StringComparison.OrdinalIgnoreCase);

                if (isTeacherView || isBranchAdminView || !profile.IsOrgAdmin)
                {
                    blocked = true;
                }
            }
        }

        _currentState = new BranchUiAvailabilityResult
        {
            IsLoaded = state.IsLoaded,
            IsBranchResolved = state.IsBranchResolved,
            IsOperational = state.IsOperational,
            IsDailyOpened = state.IsDailyOpened,
            IsBranchClosed = state.IsBranchClosed,
            IsRouteBlocked = blocked,
            RouteCategory = category,
            OrganizationId = state.OrganizationId,
            OrganizationName = state.OrganizationName,
            BranchId = state.BranchId,
            BranchName = state.BranchName,
            OperationalStatus = state.OperationalStatus,
            ClosureReason = state.ClosureReason,
            ReopeningDate = state.ReopeningDate,
            ActiveView = state.ActiveView
        };

        return _currentState;
    }

    public bool IsOperationalNavItemLocked(string href)
    {
        if (!_currentState.IsLoaded || !_currentState.IsBranchClosed) return false;
        if (string.Equals(_currentState.ActiveView, "OrgAdmin", StringComparison.OrdinalIgnoreCase)) return false;
        var category = BranchRoutePolicy.ClassifyRoute(href, _currentState.ActiveView);
        return category == BranchRouteCategory.Operational;
    }

    private string ResolveActiveView(UserRoleProfile profile)
    {
        var pref = _viewPreference.CurrentView;
        if (!string.IsNullOrWhiteSpace(pref) && profile.AvailableViews.Contains(pref, StringComparer.OrdinalIgnoreCase))
        {
            return pref;
        }
        if (profile.IsOrgAdmin) return "OrgAdmin";
        if (profile.IsBranchAdmin) return "BranchAdmin";
        if (profile.IsLeadTeacher) return "LeadTeacher";
        return "Teacher";
    }

    private async Task<(Guid BranchId, string BranchName)> ResolveActiveBranchAsync(UserRoleProfile profile, CancellationToken ct)
    {
        if (_viewPreference.ActiveBranchId.HasValue && _viewPreference.ActiveBranchId.Value != Guid.Empty)
        {
            return (_viewPreference.ActiveBranchId.Value, _viewPreference.ActiveBranchName ?? string.Empty);
        }

        if (_userContext.BranchId.HasValue && _userContext.BranchId.Value != Guid.Empty)
        {
            return (_userContext.BranchId.Value, string.Empty);
        }

        if (profile.AssignedBranchIds.Count > 0)
        {
            return (profile.AssignedBranchIds.First(), string.Empty);
        }

        if (profile.AssignedClassroomIds.Count > 0)
        {
            var allClassrooms = await _classroomService.GetAllClassroomsAsync();
            var firstRoom = allClassrooms.FirstOrDefault(c => profile.AssignedClassroomIds.Contains(c.Id));
            if (firstRoom != null)
            {
                return (firstRoom.BranchId, string.Empty);
            }
        }

        var targetOrgId = _userContext.OrganizationId.HasValue && _userContext.OrganizationId.Value != Guid.Empty
            ? _userContext.OrganizationId.Value
            : profile.OrganizationId;

        if (targetOrgId != Guid.Empty)
        {
            var branches = (await _branchService.GetBranchesAsync(targetOrgId)).ToList();
            if (branches.Count > 0)
            {
                return (branches[0].Id, branches[0].Name);
            }
        }

        return (Guid.Empty, string.Empty);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _notificationCenter.OnBranchStatusChanged -= HandleBranchStatusChangedAsync;
        _viewPreference.OnBranchChanged -= HandlePreferenceChanged;
        _viewPreference.OnViewChanged -= HandlePreferenceChanged;
        _disposeCts.Cancel();
        _pollTimer.Dispose();
        _disposeCts.Dispose();
    }
}


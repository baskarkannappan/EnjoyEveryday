using System;

namespace EnjoyEveryday.Daycare.Web.Services;

public interface IUserViewPreferenceService
{
    string? CurrentView { get; }
    void SetCurrentView(string view);
    event Action? OnViewChanged;

    Guid? ActiveBranchId { get; }
    string? ActiveBranchName { get; }
    void SetActiveBranch(Guid branchId, string branchName);
    event Action? OnBranchChanged;
}

public sealed class UserViewPreferenceService : IUserViewPreferenceService
{
    private string? _currentView;
    private Guid? _activeBranchId;
    private string? _activeBranchName;

    public string? CurrentView => _currentView;
    public Guid? ActiveBranchId => _activeBranchId;
    public string? ActiveBranchName => _activeBranchName;

    public event Action? OnViewChanged;
    public event Action? OnBranchChanged;

    public void SetCurrentView(string view)
    {
        if (string.IsNullOrWhiteSpace(view)) return;
        if (!string.Equals(_currentView, view, StringComparison.OrdinalIgnoreCase))
        {
            _currentView = view;
            OnViewChanged?.Invoke();
        }
    }

    public void SetActiveBranch(Guid branchId, string branchName)
    {
        if (_activeBranchId != branchId || _activeBranchName != branchName)
        {
            _activeBranchId = branchId;
            _activeBranchName = branchName;
            OnBranchChanged?.Invoke();
        }
    }
}

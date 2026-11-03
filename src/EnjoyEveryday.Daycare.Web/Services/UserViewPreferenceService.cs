using System;

namespace EnjoyEveryday.Daycare.Web.Services;

public interface IUserViewPreferenceService
{
    string? CurrentView { get; }
    void SetCurrentView(string view);
    event Action? OnViewChanged;
}

public sealed class UserViewPreferenceService : IUserViewPreferenceService
{
    private string? _currentView;

    public string? CurrentView => _currentView;

    public event Action? OnViewChanged;

    public void SetCurrentView(string view)
    {
        if (string.IsNullOrWhiteSpace(view)) return;
        if (!string.Equals(_currentView, view, StringComparison.OrdinalIgnoreCase))
        {
            _currentView = view;
            OnViewChanged?.Invoke();
        }
    }
}

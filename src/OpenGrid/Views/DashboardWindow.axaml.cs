using Avalonia.Controls;
using OpenGrid.Models;

namespace OpenGrid.Views;

public sealed record DashboardWindowState(
    string DashboardId,
    string Url,
    string Title,
    double Width,
    double Height,
    DashboardLaunchTrigger Trigger,
    int? GameAppId);

public partial class DashboardWindow : Window
{
    // The last entry is the dashboard currently shown. Pushing an override keeps the
    // previous dashboard underneath so it can be restored when the trigger ends.
    private readonly List<DashboardWindowState> _dashboardStack = [];
    private bool _isAdapterCreated;

    public DashboardWindow(string url, string title, double width, double height)
        : this(new DashboardWindowState(string.Empty, url, title, width, height, DashboardLaunchTrigger.None, null))
    {
    }

    public DashboardWindow(DashboardWindowState initialState)
    {
        InitializeComponent();
        PushDashboard(initialState);
        WebView.AdapterCreated += OnAdapterCreated;
        WebView.WebMessageReceived += WebViewOnWebMessageReceived;
    }

    public int DashboardCount => _dashboardStack.Count;

    public DashboardWindowState? CurrentDashboard => DashboardCount > 0 ? _dashboardStack[^1] : null;

    public void PushDashboard(DashboardWindowState state)
    {
        _dashboardStack.Add(state);
        ApplyDashboard(state);
    }

    public bool RemoveDashboardsForGame(int gameAppId)
    {
        return RemoveDashboards(state => state.GameAppId == gameAppId);
    }

    public bool RemoveDashboardsForTrigger(DashboardLaunchTrigger trigger)
    {
        return RemoveDashboards(state => state.Trigger == trigger);
    }

    private bool RemoveDashboards(Func<DashboardWindowState, bool> predicate)
    {
        var changed = false;

        for (var i = _dashboardStack.Count - 2; i >= 0; i--)
        {
            if (!predicate(_dashboardStack[i]))
                continue;

            _dashboardStack.RemoveAt(i);
            changed = true;
        }

        if (_dashboardStack.Count == 0 || !predicate(_dashboardStack[^1]))
            return changed;

        _dashboardStack.RemoveAt(_dashboardStack.Count - 1);
        changed = true;

        if (_dashboardStack.Count > 0)
            ApplyDashboard(_dashboardStack[^1]);

        return changed;
    }

    private void ApplyDashboard(DashboardWindowState state)
    {
        Title = state.Title;
        Width = state.Width;
        Height = state.Height;

        if (_isAdapterCreated)
            WebView.Source = new Uri(state.Url);
    }

    private void WebViewOnWebMessageReceived(object? sender, WebMessageReceivedEventArgs e)
    {
        if (e.Body is not {} message)
        {
            return;
        }

        switch (message)
        {
            case "ToggleFullscreen":
                WindowState = WindowState == WindowState.FullScreen 
                    ? WindowState.Normal 
                    : WindowState.FullScreen;
                break;
            case "ToggleWindowDecorations":
                WindowDecorations = WindowDecorations == WindowDecorations.Full 
                    ? WindowDecorations.None 
                    : WindowDecorations.Full;
                break;
        }
    }
    
    private void OnAdapterCreated(object? sender, EventArgs e)
    {
        _isAdapterCreated = true;

        // Workaround for https://github.com/AvaloniaUI/Avalonia.Controls.WebView/issues/48
        // On Wayland/XWayland the embedded GTK widget gets a wrong initial allocation (~200x200).
        // Toggling visibility forces the widget to re-allocate at the correct bounds.
        WebView.IsVisible = false;
        WebView.IsVisible = true;

        if (CurrentDashboard is { } dashboard)
            WebView.Source = new Uri(dashboard.Url);
    }
}

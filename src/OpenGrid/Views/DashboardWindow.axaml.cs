using Avalonia.Controls;

namespace OpenGrid.Views;

public partial class DashboardWindow : Window
{
    private readonly string _url;

    public DashboardWindow(string url, string title)
    {
        InitializeComponent();
        Title = title;
        _url = url;

        WebView.AdapterCreated += OnAdapterCreated;
    }

    private void OnAdapterCreated(object? sender, EventArgs e)
    {
        // Workaround for https://github.com/AvaloniaUI/Avalonia.Controls.WebView/issues/48
        // On Wayland/XWayland the embedded GTK widget gets a wrong initial allocation (~200x200).
        // Toggling visibility forces the widget to re-allocate at the correct bounds.
        WebView.IsVisible = false;
        WebView.IsVisible = true;

        WebView.Source = new Uri(_url);
    }
}

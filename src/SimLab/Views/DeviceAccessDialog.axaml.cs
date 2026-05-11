using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace SimLab.Views;

public partial class DeviceAccessDialog : Window
{
    private static readonly HttpClient _httpClient = new();

    public DeviceAccessDialog(string dashboardName, string url)
    {
        InitializeComponent();
        Title = dashboardName;
        DashboardNameText.Text = dashboardName;
        UrlText.Text = url;

        Loaded += async (_, _) => await LoadQrCode(url);
    }

    private async Task LoadQrCode(string url)
    {
        try
        {
            var encoded = Uri.EscapeDataString(url);
            var apiUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={encoded}";
            var bytes = await _httpClient.GetByteArrayAsync(apiUrl);
            using var ms = new MemoryStream(bytes);
            var bitmap = new Bitmap(ms);
            Dispatcher.UIThread.Post(() => QrCodeImage.Source = bitmap);
        }
        catch
        {
            UrlText.Text = $"{url}\n\n(QR code unavailable offline)";
        }
    }

    private void CloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}

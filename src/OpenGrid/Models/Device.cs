namespace OpenGrid.Models;

public enum DeviceConnectionStatus
{
    Disconnected,
    Connected,
}

public enum DeviceType
{
    Display,
    Serial,
    Sound,
}

public enum DashboardLaunchTrigger
{
    None,
    OnAppStart,
    OnGameStart,
    OnTelemetryConnected,
}
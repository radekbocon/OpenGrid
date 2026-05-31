using System;
using Avalonia.Controls;

namespace SimLab;

public static class UriLauncher
{
    private static TopLevel _topLevel;
    
    public static void Initialize(TopLevel topLevel) => _topLevel = topLevel;

    public static void Open(string uri) => _topLevel?.Launcher.LaunchUriAsync(new Uri(uri));
}
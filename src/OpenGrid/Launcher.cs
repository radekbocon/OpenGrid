using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace OpenGrid;

public static class Launcher
{
    private static ILauncher? _launcher;
    
    public static void Initialize(ILauncher launcher) => _launcher = launcher;

    public static async Task<bool> LaunchUriAsync(string uri)
    {
        if (string.IsNullOrEmpty(uri))
        {
            return false;
        }

        if (_launcher is null)
        {
            return false;
        }

        return await _launcher.LaunchUriAsync(new Uri(uri));
    }
    public static async Task LaunchDirectoryAsync(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        if (_launcher is null)
        {
            return;
        }

        await _launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(path));
    }
}
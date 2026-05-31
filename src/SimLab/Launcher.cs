using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace SimLab;

public static class Launcher
{
    private static ILauncher? _launcher;
    
    public static void Initialize(ILauncher launcher) => _launcher = launcher;

    public static async Task<bool> LaunchUriAsync(string uri) => await _launcher?.LaunchUriAsync(new Uri(uri))!;
    public static async Task<bool> LaunchDirectoryAsync(string path) => await _launcher?.LaunchDirectoryInfoAsync(new DirectoryInfo(path))!;
}
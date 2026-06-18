using System.IO;

namespace OpenGrid.Models;

public class SteamGameProcess
{
    public required SteamGame SteamGame { get; set; }

    public string LibraryPath { get; init; } = "";

    public string FullInstallPath => Path.Combine(LibraryPath, "steamapps", "common", SteamGame.InstallDirectory);
}
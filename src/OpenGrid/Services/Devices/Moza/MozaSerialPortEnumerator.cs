using System.Globalization;
using System.IO.Ports;
using System.Text.RegularExpressions;
using Serilog;

namespace OpenGrid.Services.Devices.Moza;

public sealed record MozaPortInfo(
    string Id,
    string PortName,
    string Name,
    string Description,
    ushort VendorId,
    ushort ProductId,
    string? SerialNumber);

/// <summary>
/// Finds MOZA devices on the serial bus. Discovery mirrors boxflat: prefer
/// stable /dev/serial/by-id links with a "gudsen" or "moza" name, then verify
/// the USB vendor/product ID from sysfs (MOZA VID 0x346E).
/// </summary>
public static class MozaSerialPortEnumerator
{
    private const string ByIdDirectory = "/dev/serial/by-id";

    /// <summary>
    /// Checks whether a persisted port path still points at a device node.
    /// The by-id symlink itself stays on disk after unplugging, so File.Exists
    /// (which follows the link) is used instead of checking the link entry.
    /// </summary>
    public static bool IsPortPresent(string portName)
    {
        return !string.IsNullOrWhiteSpace(portName) && File.Exists(portName);
    }

    public static IReadOnlyList<MozaPortInfo> Enumerate()
    {
        var candidates = new List<PortCandidate>();

        if (Directory.Exists(ByIdDirectory))
        {
            foreach (var link in Directory.GetFiles(ByIdDirectory))
            {
                var linkName = Path.GetFileName(link);
                if (!linkName.Contains("gudsen", StringComparison.OrdinalIgnoreCase) &&
                    !linkName.Contains("moza", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Store the resolved tty as PortPath so the generic port scan
                // below can recognise it as the same device and keep the
                // stable by-id link (and its serial number) as metadata.
                var portPath = ResolveLink(link);
                if (portPath is null)
                {
                    continue;
                }

                candidates.Add(new PortCandidate(portPath, link, linkName));
            }
        }

        try
        {
            foreach (var port in SerialPort.GetPortNames())
            {
                if (!candidates.Any(x => string.Equals(x.PortPath, port, StringComparison.OrdinalIgnoreCase)))
                {
                    candidates.Add(new PortCandidate(port, null, null));
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to enumerate serial ports");
        }

        var devices = new List<MozaPortInfo>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            var (vendorId, productId) = ReadUsbIds(candidate.PortPath);
            if (vendorId != MozaProtocol.VendorId || productId == 0)
            {
                continue;
            }

            var sysPortName = Path.GetFileName(candidate.PortPath);
            var serialNumber = ExtractSerialNumber(candidate.LinkName);
            var id = "moza_" + SanitizeId(serialNumber ?? sysPortName);
            if (!seenIds.Add(id))
            {
                continue;
            }

            var name = MozaDeviceCatalog.Describe(productId, PrettyLinkName(candidate.LinkName));
            devices.Add(new MozaPortInfo(
                id,
                candidate.LinkPath ?? candidate.PortPath,
                name,
                candidate.LinkName ?? $"{sysPortName} (VID {vendorId:X4}, PID {productId:X4})",
                vendorId,
                productId,
                serialNumber));
        }

        return devices;
    }

    private static string? ResolveLink(string path)
    {
        try
        {
            var file = new FileInfo(path);
            var target = file.ResolveLinkTarget(returnFinalTarget: true)?.FullName;
            if (string.IsNullOrEmpty(target) && file.LinkTarget is { } linkTarget)
            {
                target = Path.GetFullPath(Path.Combine(file.DirectoryName ?? "/", linkTarget));
            }

            return !string.IsNullOrEmpty(target) && File.Exists(target) ? target : null;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to resolve serial link {Path}", path);
            return null;
        }
    }

    private static (ushort VendorId, ushort ProductId) ReadUsbIds(string portPath)
    {
        try
        {
            var sysPort = Path.GetFileName(ResolveLink(portPath) ?? portPath);
            var ueventPath = Path.Combine("/sys", "class", "tty", sysPort, "device", "uevent");
            if (!File.Exists(ueventPath))
            {
                return (0, 0);
            }

            foreach (var line in File.ReadLines(ueventPath))
            {
                if (!line.StartsWith("PRODUCT=", StringComparison.Ordinal))
                {
                    continue;
                }

                var parts = line["PRODUCT=".Length..].Split('/');
                if (parts.Length >= 2 &&
                    ushort.TryParse(parts[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var vendorId) &&
                    ushort.TryParse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var productId))
                {
                    return (vendorId, productId);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to read USB IDs for serial port {Path}", portPath);
        }

        return (0, 0);
    }

    private static string? ExtractSerialNumber(string? linkName)
    {
        if (linkName is null)
        {
            return null;
        }

        var name = Regex.Replace(linkName, @"-if\d+$", string.Empty);
        var token = name.Split('_').LastOrDefault();
        return token is { Length: >= 8 } && token.All(Uri.IsHexDigit) ? token : null;
    }

    private static string? PrettyLinkName(string? linkName)
    {
        if (linkName is null)
        {
            return null;
        }

        var name = linkName.StartsWith("usb-", StringComparison.OrdinalIgnoreCase)
            ? linkName[4..]
            : linkName;
        name = Regex.Replace(name, @"-if\d+$", string.Empty);

        var tokens = name.Split('_').ToList();
        if (tokens.Count > 2 && tokens[^1] is { Length: >= 8 } serial && serial.All(Uri.IsHexDigit))
        {
            tokens.RemoveAt(tokens.Count - 1);
        }

        return string.Join(' ', tokens);
    }

    private static string SanitizeId(string value)
    {
        var chars = value.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_');
        return new string(chars.ToArray());
    }

    private sealed record PortCandidate(string PortPath, string? LinkPath, string? LinkName);
}

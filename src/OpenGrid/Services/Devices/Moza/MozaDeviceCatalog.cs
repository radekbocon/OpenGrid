using OpenGrid.Models;

namespace OpenGrid.Services.Devices.Moza;

/// <summary>
/// MOZA USB product IDs, from the boxflat/AZOM device inventories.
/// </summary>
public static class MozaDeviceCatalog
{
    public static string Describe(ushort productId, string? fallback = null)
    {
        var name = productId switch
        {
            0x0000 => "MOZA R16 / R21 Wheelbase",
            0x0001 => "MOZA CRP Pedals",
            0x0002 => "MOZA R9 Wheelbase",
            0x0003 => "MOZA SRP Pedals",
            0x0004 => "MOZA R5 Wheelbase",
            0x0005 => "MOZA R3 Wheelbase",
            0x0006 => "MOZA R12 Wheelbase",
            0x0008 => "MOZA mBooster Pedals",
            0x0011 => "MOZA CRP2 Pedals",
            0x0012 => "MOZA R9 Wheelbase (variant)",
            0x0016 => "MOZA R12 Wheelbase (variant)",
            0x001E => "MOZA HGP Shifter",
            0x001F => "MOZA HBP Handbrake",
            0x0020 => "MOZA Universal HUB",
            0x0023 => "MOZA SGP Shifter",
            0x0024 => "MOZA Stalks",
            0x0025 => "MOZA CM2 Dash",
            0x1000 => "MOZA AB9 Shifter",
            _ => fallback ?? "MOZA Serial Device",
        };

        return name;
    }

    public static MozaDeviceCategory Categorize(ushort productId)
    {
        return productId switch
        {
            0x0000 or 0x0002 or 0x0004 or 0x0005 or 0x0006 or 0x0012 or 0x0016 =>
                MozaDeviceCategory.WheelBase,
            0x0001 or 0x0003 or 0x0008 or 0x0011 => MozaDeviceCategory.Pedals,
            0x0020 => MozaDeviceCategory.Hub,
            0x0023 or 0x001E or 0x1000 => MozaDeviceCategory.Shifter,
            0x001F => MozaDeviceCategory.Handbrake,
            0x0025 => MozaDeviceCategory.Dashboard,
            0x0024 => MozaDeviceCategory.Other,
            _ => MozaDeviceCategory.Unknown,
        };
    }
}

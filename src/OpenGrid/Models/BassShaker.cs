namespace OpenGrid.Models;

public enum TelemetryInput
{
    Abs,
    Tc,
    WheelSpin,
    WheelLock,
}

public enum BassShakerChannel
{
    All,
    Left,
    Right,
}

[Flags]
public enum WheelSelection
{
    None = 0,
    FrontLeft = 1,
    FrontRight = 2,
    RearLeft = 4,
    RearRight = 8,
    All = FrontLeft | FrontRight | RearLeft | RearRight,
}

public class BassShakerConfiguration
{
    public double Volume { get; set; } = 1.0;
    public List<BassShakerInputSettings> Inputs { get; set; } = [];
}

public class BassShakerInputSettings
{
    public TelemetryInput Input { get; set; }
    public BassShakerChannel Channel { get; set; } = BassShakerChannel.All;
    public WheelSelection Wheels { get; set; } = WheelSelection.All;
    public bool IsEnabled { get; set; } = true;
    public double Volume { get; set; } = 1.0;
    public double Frequency { get; set; } = 45.0;
    public double MinPercent { get; set; } = 0;
    public double MaxPercent { get; set; } = 100;
}

public static class TelemetryInputExtensions
{
    public static string GetDisplayName(this TelemetryInput input)
    {
        return input switch
        {
            TelemetryInput.Abs => "ABS",
            TelemetryInput.Tc => "Traction Control",
            TelemetryInput.WheelSpin => "Wheel Spin",
            TelemetryInput.WheelLock => "Wheel Lock",
            _ => input.ToString(),
        };
    }
}

public static class WheelSelectionExtensions
{
    public static bool Includes(this WheelSelection wheels, WheelSelection wheel) => (wheels & wheel) == wheel;
}

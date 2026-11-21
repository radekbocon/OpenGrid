using OpenGrid.Models.Telemetry;

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
    private List<BassShakerInputSettings> _inputs = [];

    public double Volume { get; set; } = 1.0;

    public List<BassShakerInputSettings> Inputs
    {
        get => _inputs;
        set => _inputs = value ?? [];
    }

    public BassShakerConfiguration Clone()
    {
        return new BassShakerConfiguration
        {
            Volume = Volume,
            Inputs = Inputs
                .Where(static input => input is not null)
                .Select(static input => new BassShakerInputSettings
                {
                    Input = input.Input,
                    IsEnabled = input.IsEnabled,
                    Volume = input.Volume,
                    Frequency = input.Frequency,
                    MinPercent = input.MinPercent,
                    MaxPercent = input.MaxPercent,
                    Mappings = input.Mappings
                        .Where(static mapping => mapping is not null)
                        .Select(static mapping => new BassShakerMapping
                        {
                            Channel = mapping.Channel,
                            Wheels = mapping.Wheels,
                        })
                        .ToList(),
                })
                .ToList(),
        };
    }
}

public class BassShakerPreset
{
    private BassShakerConfiguration _configuration = new();

    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public BassShakerConfiguration Configuration
    {
        get => _configuration;
        set => _configuration = value ?? new BassShakerConfiguration();
    }
}

public class BassShakerPresetOverride
{
    /// <summary>
    /// Steam application id of the game this override applies to. Null matches any game.
    /// </summary>
    public int? GameAppId { get; set; }

    /// <summary>
    /// Telemetry car key this override applies to. Null matches any car.
    /// </summary>
    public CarProfile? Car { get; set; }

    public string PresetId { get; set; } = string.Empty;
}

public class BassShakerInputSettings
{
    private List<BassShakerMapping> _mappings = [];

    public TelemetryInput Input { get; set; }

    public List<BassShakerMapping> Mappings
    {
        get => _mappings;
        set => _mappings = value ?? [];
    }

    public bool IsEnabled { get; set; } = true;
    public double Volume { get; set; } = 1.0;
    public double Frequency { get; set; } = 45.0;
    public double MinPercent { get; set; } = 0;
    public double MaxPercent { get; set; } = 100;
}

public class BassShakerMapping
{
    public BassShakerChannel Channel { get; set; } = BassShakerChannel.All;
    public WheelSelection Wheels { get; set; } = WheelSelection.All;
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

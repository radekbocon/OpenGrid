namespace OpenGrid.Models;

public enum TelemetryInput
{
    EngineRpm,
}

public class BassShakerConfiguration
{
    public bool IsEnabled { get; set; }
    public List<BassShakerInputSettings> Inputs { get; set; } = [];
}

public class BassShakerInputSettings
{
    public TelemetryInput Input { get; set; }
    public bool IsEnabled { get; set; } = true;
    public double Volume { get; set; } = 1.0;
    public double Frequency { get; set; } = 45.0;
    public double MinRpmPercent { get; set; } = 20;
    public double MaxRpmPercent { get; set; } = 100;
}

namespace OpenGrid.Models;

public enum TelemetryInput
{
    Abs,
    Tc,
}

public enum BassShakerChannel
{
    All,
    Left,
    Right,
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
    public bool IsEnabled { get; set; } = true;
    public double Volume { get; set; } = 1.0;
    public double Frequency { get; set; } = 45.0;
    public double MinPercent { get; set; } = 0;
    public double MaxPercent { get; set; } = 100;
}

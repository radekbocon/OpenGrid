namespace OpenGrid.Models;

public class CarProfile
{
    public string Name { get; init; } = string.Empty;
    public int MaxRpm { get; set; } = 8000;
    public int RedlineRpm { get; set; } = 7600;
    public float BrakeBiasOffset { get; set; }
}

using System.Text.Json.Serialization;

namespace OpenGrid.Models;

public class CarProfile
{
    public string CarKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int MaxRpm { get; set; } = 8000;
    public int RedlineRpm { get; set; } = 7600;
    public float BrakeBiasOffset { get; set; }
}

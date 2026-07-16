using System;
using System.Text.Json.Serialization;

namespace OpenGrid.Models;

public class CarProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string CarKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int MaxRpm { get; set; } = 8000;
    public int RedlineRpm { get; set; } = 7600;

    [JsonIgnore]
    public double RedlinePercentage
    {
        get => MaxRpm > 0 ? (double)RedlineRpm / MaxRpm * 100 : 95.0;
        set => RedlineRpm = (int)Math.Round(MaxRpm * value / 100);
    }
}

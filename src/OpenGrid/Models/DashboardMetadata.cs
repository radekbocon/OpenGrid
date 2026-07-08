using System.Text.Json.Serialization;

namespace OpenGrid.Models;

public class DashboardMetadata
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Untitled Dashboard";
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";
    [JsonPropertyName("type")]
    public string Type { get; set; } = "dashboard";
    [JsonPropertyName("author")]
    public string Author { get; set; } = "";
    [JsonPropertyName("width")]
    public double Width { get; set; } = 1280.0;
    [JsonPropertyName("height")]
    public double Height { get; set; } = 720.0;
}

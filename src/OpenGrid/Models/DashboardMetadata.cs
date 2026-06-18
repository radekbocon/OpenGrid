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
}

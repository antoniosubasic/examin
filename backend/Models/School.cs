using System.Text.Json.Serialization;

namespace backend.Models;

public class School
{
    [JsonPropertyName("server")]
    public string ServerURL { get; set; } = "";

    [JsonPropertyName("displayName")]
    public string Name { get; set; } = "";

    [JsonPropertyName("loginName")]
    public string LoginName { get; set; } = "";
}

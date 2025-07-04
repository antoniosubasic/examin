using System.Text.Json.Serialization;

namespace backend.Models;

public class Exam
{
    [JsonPropertyName("examType")]
    public string? Type { get; init; }


    [JsonPropertyName("name")]
    public string? Name { get; init; }


    [JsonPropertyName("examDate")]
    public DateOnly Date { get; init; }


    [JsonPropertyName("startTime")]
    public TimeOnly StartTime { get; init; }


    [JsonPropertyName("endTime")]
    public TimeOnly EndTime { get; init; }


    [JsonPropertyName("subject")]
    public string? Subject { get; set; }


    [JsonPropertyName("text")]
    public string? Description { get; init; }


    [JsonIgnore]
    public DateTimeOffset Start
    {
        get
        {
            var dateTime = Date.ToDateTime(StartTime);
            return new DateTimeOffset(dateTime);
        }
    }

    [JsonIgnore]
    public DateTimeOffset End
    {
        get
        {
            var dateTime = Date.ToDateTime(EndTime);
            return new DateTimeOffset(dateTime);
        }
    }

    public override bool Equals(object? obj) => obj is Exam other && Name == other.Name && Date == other.Date && StartTime == other.StartTime && EndTime == other.EndTime;

    public override int GetHashCode() => HashCode.Combine(Name, Date, StartTime, EndTime);
}

using System.Text.Json.Serialization;

namespace examin
{
    internal class Exam
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

        public static Exam FromGoogleEvent(Google.Apis.Calendar.v3.Data.Event e)
        {
            var start = e.Start.DateTimeDateTimeOffset!.Value;
            var end = e.End.DateTimeDateTimeOffset!.Value;

            return new()
            {
                Type = null,
                Name = e.Summary,
                Date = DateOnly.FromDateTime(start.DateTime),
                StartTime = TimeOnly.FromDateTime(start.DateTime),
                EndTime = TimeOnly.FromDateTime(end.DateTime),
                Subject = null,
                Description = e.Description
            };
        }

        public Google.Apis.Calendar.v3.Data.Event ToGoogleEvent()
        {
            TimeZoneInfo.TryConvertWindowsIdToIanaId(TimeZoneInfo.Local.Id, out var timezone);

            return new()
            {
                Summary = Subject,
                Description = Description,
                Start = new() { DateTimeDateTimeOffset = Start, TimeZone = timezone },
                End = new() { DateTimeDateTimeOffset = End, TimeZone = timezone },
                Reminders = new() { UseDefault = true }
            };
        }

        public override bool Equals(object? obj) => obj is Exam other && Name == other.Name && Date == other.Date && StartTime == other.StartTime && EndTime == other.EndTime;
    
        public override int GetHashCode() => HashCode.Combine(Name, Date, StartTime, EndTime);
    }
}

using System.Text.Json.Serialization;

namespace examin
{
    internal class Exam
    {
        [JsonPropertyName("examType")]
        internal string? Type { get; init; }


        [JsonPropertyName("name")]
        internal string? Name { get; init; }


        [JsonPropertyName("examDate")]
        internal DateOnly Date { get; init; }


        [JsonPropertyName("startTime")]
        internal TimeOnly StartTime { get; init; }


        [JsonPropertyName("endTime")]
        internal TimeOnly EndTime { get; init; }


        [JsonPropertyName("subject")]
        internal string? Subject { get; set; }


        [JsonPropertyName("text")]
        internal string? Description { get; init; }


        [JsonIgnore]
        internal DateTimeOffset Start
        {
            get
            {
                var dateTime = Date.ToDateTime(StartTime);
                return new DateTimeOffset(dateTime);
            }
        }

        [JsonIgnore]
        internal DateTimeOffset End
        {
            get
            {
                var dateTime = Date.ToDateTime(EndTime);
                return new DateTimeOffset(dateTime);
            }
        }

        internal void TranslateSubject()
        {
            // translate subjects here
            // for example:

            // Subject = Subject switch
            // {
            //     "English" => "E",
            //     "Math" => "M",
            //     ...
            //     _ => Subject
            // };
        }
    }
}

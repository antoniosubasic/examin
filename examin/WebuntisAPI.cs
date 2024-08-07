using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace examin.WebuntisAPI
{
    internal struct SearchSchoolPayload
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("method")]
        public string Method { get; set; }

        [JsonPropertyName("params")]
        public SearchSchoolParams[] Params { get; set; }

        [JsonPropertyName("jsonrpc")]
        public string JsonRPC { get; set; }
    }

    internal struct SearchSchoolParams
    {
        [JsonPropertyName("search")]
        public string Search { get; set; }
    }

    internal class Session(Config.School school, string username, string password)
    {
        private Config.School _school { get; set; } = school;
        public string? Username { get; set; } = username;
        public string? Password { get; set; } = password;

        private string? _jSessionId { get; set; }
        private string? _schoolId { get; set; }
        private string _cookie => $"JSESSIONID={_jSessionId}; schoolname={_schoolId}";

        public bool LoggedIn { get; private set; }

        public static async Task<IEnumerable<Config.School>> SearchSchool(string searchQuery)
        {
            using var client = new HttpClient();

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new("https://mobile.webuntis.com/ms/schoolquery2"),
                Headers = { { "Accept", "application/json" } },
                Content = new StringContent(
                    JsonSerializer.Serialize(new SearchSchoolPayload
                    {
                        Id = $"wu_schulsuche-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                        Method = "searchSchool",
                        Params = [new SearchSchoolParams { Search = searchQuery }],
                        JsonRPC = "2.0"
                    }),
                    Encoding.UTF8,
                    "application/json"
                )
            };

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var jsonDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var root = jsonDocument.RootElement;

                if (root.TryGetProperty("error", out var error)) { throw new HttpRequestException(error.GetProperty("message").GetString()); }
                else if (root.TryGetProperty("result", out var result))
                {
                    if (result.TryGetProperty("schools", out var schools)) { return JsonSerializer.Deserialize<IEnumerable<Config.School>>(schools.GetRawText()) ?? throw new JsonException("failed to deserialize schools"); }
                    else { throw new JsonException("failed to get property 'schools'"); }
                }
                else { throw new JsonException("failed to get property 'results'"); }
            }
            else { throw new HttpRequestException(await response.Content.ReadAsStringAsync()); }
        }

        private void EnsureLoggedIn()
        {
            if (!LoggedIn) { throw new AuthenticationException("you need to login first"); }
        }

        public async Task TryLogin()
        {
            if (!LoggedIn)
            {
                using var client = new HttpClient();

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new($"https://{_school.ServerURL}/WebUntis/j_spring_security_check"),
                    Headers = { { "Accept", "application/json" } },
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "school", _school.LoginName },
                        { "j_username", Username ?? throw new ArgumentNullException("username can't be null") },
                        { "j_password", Password ?? throw new ArgumentNullException("password can't be null") },
                        { "token", "" }
                    })
                };

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseCookies = response.Headers.GetValues("Set-Cookie");

                    _jSessionId = responseCookies.First(cookie => cookie.StartsWith("JSESSIONID")).Split(';')[0].Split('=')[1];
                    _schoolId = responseCookies.First(cookie => cookie.StartsWith("schoolname")).Split(';')[0].Split('=')[1].Trim('"');

                    if (_jSessionId is null || _schoolId is null) { throw new HttpRequestException("JSESSIONID or SCHOOLID not found"); }
                    else { LoggedIn = true; }
                }
                else { throw new HttpRequestException(await response.Content.ReadAsStringAsync()); }
            }
        }

        public async Task<IEnumerable<Exam>> TryGetExams(DateOnly start, DateOnly end)
        {
            EnsureLoggedIn();

            using var client = new HttpClient();

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new($"https://{_school.ServerURL}/WebUntis/api/exams?startDate={start:yyyyMMdd}&endDate={end:yyyyMMdd}"),
                Headers =
                {
                    { "Accept", "application/json" },
                    { "Cookie", _cookie }
                }
            };

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var jsonDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var root = jsonDocument.RootElement;

                if (root.TryGetProperty("data", out var data))
                {
                    if (data.TryGetProperty("exams", out var exams))
                    {
                        System.Windows.Clipboard.SetText(exams.GetRawText());
                        return JsonSerializer.Deserialize<IEnumerable<Exam>>(exams.GetRawText(), new JsonSerializerOptions { Converters = { new DateOnlyConverter(), new TimeOnlyConverter() } }) ?? throw new JsonException("failed to deserialize exams");
                    }
                    else { throw new JsonException("failed to get property 'exams'"); }
                }
                else { throw new JsonException("failed to get property 'data'"); }
            }
            else { throw new HttpRequestException(await response.Content.ReadAsStringAsync()); }
        }
    }

    internal class DateOnlyConverter : JsonConverter<DateOnly>
    {
        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var date = reader.GetInt32();
            return new DateOnly(date / 10_000, date / 100 % 100, date % 100);
        }

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    internal class TimeOnlyConverter : JsonConverter<TimeOnly>
    {
        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var time = reader.GetInt32();
            return new TimeOnly(time / 100, time % 100);
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}

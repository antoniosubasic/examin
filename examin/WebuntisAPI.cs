using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace examin.WebuntisAPI
{
    internal class Session(string schoolUrl, string schoolName, string username, string password)
    {
        private string _schoolUrl { get; } = (schoolUrl.StartsWith("https://") ? schoolUrl : $"https://{schoolUrl.TrimStart("http://".ToCharArray())}").TrimEnd('/');
        private string _schoolName { get; } = schoolName.ToLower().Replace('-', ' ');
        private string _username { get; } = username;
        private string _password { get; } = password;
        private string _cookie => $"JSESSIONID={_jSessionId}; schoolname={_schoolId}";

        private string? _jSessionId { get; set; }
        private string? _schoolId { get; set; }

        private bool _loggedIn { get; set; }

        internal Session(Config config) : this(config.SchoolURL, config.Schoolname, config.Username, config.Password) { }


        private void EnsureLoggedIn()
        {
            if (!_loggedIn) { throw new InvalidOperationException("You need to login first!"); }
        }

        internal async Task TryLogin()
        {
            if (!_loggedIn)
            {
                using var client = new HttpClient();

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new($"{_schoolUrl}/WebUntis/j_spring_security_check"),
                    Headers = { { "Accept", "application/json" } },
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "school", _schoolName },
                        { "j_username", _username },
                        { "j_password", _password },
                        { "token", "" }
                    })
                };

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseCookies = response.Headers.GetValues("Set-Cookie");

                    _jSessionId = responseCookies.First(cookie => cookie.StartsWith("JSESSIONID")).Split(';')[0].Split('=')[1];
                    _schoolId = responseCookies.First(cookie => cookie.StartsWith("schoolname")).Split(';')[0].Split('=')[1].Trim('"');

                    if (_jSessionId is null || _schoolId is null) { throw new Exception("Login failed"); }
                    else { _loggedIn = true; }
                }
                else { throw new Exception("Login failed"); }
            }
        }

        internal async Task<IEnumerable<Exam>> TryGetExams(DateOnly start, DateOnly end)
        {
            EnsureLoggedIn();

            using var client = new HttpClient();

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new($"{_schoolUrl}/WebUntis/api/exams?startDate={start:yyyyMMdd}&endDate={end:yyyyMMdd}"),
                Headers =
                {
                    { "Accept", "application/json" },
                    { "Cookie", _cookie }
                }
            };

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<IEnumerable<Exam>>(
                        content[content.IndexOf('[')..(content.LastIndexOf(']') + 1)],
                        new JsonSerializerOptions { Converters = { new DateOnlyConverter(), new TimeOnlyConverter() } }
                    ) ?? throw new Exception("Failed to get exams");
            }
            else { throw new Exception("Failed to get exams"); }
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

using backend.Models;
using System.Text.Json;
using System.Text;
using System.Security.Authentication;
using backend.Converters;

namespace backend.Services;

public class WebuntisService(IHttpClientFactory httpClientFactory, ILogger<WebuntisService> logger)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<WebuntisService> _logger = logger;
    private readonly Dictionary<string, UserSession> _sessions = [];

    public async Task<IEnumerable<School>> SearchSchoolsAsync(string searchQuery)
    {
        try
        {
            var payload = new
            {
                id = $"wu_schulsuche-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                method = "searchSchool",
                @params = new[] { new { search = searchQuery } },
                jsonrpc = "2.0"
            };

            using var httpClient = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new("https://mobile.webuntis.com/ms/schoolquery2"),
                Headers = { { "Accept", "application/json" } },
                Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                )
            };

            var response = await httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var jsonDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var root = jsonDocument.RootElement;

                if (root.TryGetProperty("error", out var error))
                {
                    throw new HttpRequestException(error.GetProperty("message").GetString());
                }
                else if (root.TryGetProperty("result", out var result))
                {
                    if (result.TryGetProperty("schools", out var schools))
                    {
                        return JsonSerializer.Deserialize<IEnumerable<School>>(schools.GetRawText())
                            ?? throw new JsonException("failed to deserialize schools");
                    }
                    else
                    {
                        throw new JsonException("failed to get property 'schools'");
                    }
                }
                else
                {
                    throw new JsonException("failed to get property 'result'");
                }
            }
            else
            {
                throw new HttpRequestException(await response.Content.ReadAsStringAsync());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "error searching for schools with query: {SearchQuery}", searchQuery);
            throw;
        }
    }

    public async Task<LoginResponse> LoginAsync(string schoolLoginName, string username, string password)
    {
        try
        {
            var schools = await SearchSchoolsAsync(schoolLoginName);
            var school = schools.FirstOrDefault(s => s.LoginName == schoolLoginName);

            if (school == null)
            {
                return new LoginResponse { Success = false, Message = "school not found" };
            }

            using var httpClient = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new($"https://{school.ServerURL}/WebUntis/j_spring_security_check"),
                Headers = { { "Accept", "application/json" } },
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "school", school.LoginName },
                    { "j_username", username },
                    { "j_password", password },
                    { "token", "" }
                })
            };

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!content.Contains("Invalid user name and/or password"))
            {
                if (response.IsSuccessStatusCode)
                {
                    var responseCookies = response.Headers.GetValues("Set-Cookie");
                    var jSessionId = responseCookies.First(cookie => cookie.StartsWith("JSESSIONID")).Split(';')[0].Split('=')[1];
                    var schoolId = responseCookies.First(cookie => cookie.StartsWith("schoolname")).Split(';')[0].Split('=')[1].Trim('"');

                    if (string.IsNullOrEmpty(jSessionId) || string.IsNullOrEmpty(schoolId))
                    {
                        return new LoginResponse { Success = false, Message = "JSESSIONID or SCHOOLID not found" };
                    }

                    var sessionId = Guid.NewGuid().ToString();
                    _sessions[sessionId] = new UserSession
                    {
                        School = school,
                        Username = username,
                        JSessionId = jSessionId,
                        SchoolId = schoolId,
                        LoginTime = DateTime.UtcNow
                    };

                    _logger.LogInformation(
                        "successfully created session: {SessionId} for user: {Username} at school: {School}",
                        sessionId, username, school.LoginName
                    );

                    return new LoginResponse
                    {
                        Success = true,
                        Message = "login successful",
                        SessionId = sessionId
                    };
                }
                else
                {
                    return new LoginResponse { Success = false, Message = content };
                }
            }
            else
            {
                return new LoginResponse { Success = false, Message = "invalid username or password" };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "error during login for user: {Username} at school: {School}", username, schoolLoginName);
            return new LoginResponse { Success = false, Message = "login failed due to server error" };
        }
    }

    public async Task<IEnumerable<Exam>> GetExamsAsync(string sessionId, DateOnly startDate, DateOnly endDate)
    {
        _logger.LogInformation("attempting to get exams for session: {SessionId}", sessionId);
        _logger.LogInformation("current active sessions: {SessionCount}", _sessions.Count);

        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            _logger.LogWarning(
                "session not found: {SessionId}. Available sessions: {AvailableSessions}",
                sessionId, string.Join(", ", _sessions.Keys)
            );
            throw new AuthenticationException("Invalid session");
        }

        _logger.LogInformation(
            "found session for user: {Username} at school: {School}",
            session.Username, session.School.LoginName
        );

        try
        {
            var cookie = $"JSESSIONID={session.JSessionId}; schoolname={session.SchoolId}";

            using var httpClient = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new($"https://{session.School.ServerURL}/WebUntis/api/exams?startDate={startDate:yyyyMMdd}&endDate={endDate:yyyyMMdd}"),
                Headers =
                {
                    { "Accept", "application/json" },
                    { "Cookie", cookie }
                }
            };

            var response = await httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var jsonDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var root = jsonDocument.RootElement;

                if (root.TryGetProperty("data", out var data))
                {
                    if (data.TryGetProperty("exams", out var exams))
                    {
                        var options = new JsonSerializerOptions
                        {
                            Converters = { new DateOnlyConverter(), new TimeOnlyConverter() }
                        };

                        return JsonSerializer.Deserialize<IEnumerable<Exam>>(exams.GetRawText(), options)
                            ?? throw new JsonException("failed to deserialize exams");
                    }
                    else
                    {
                        throw new JsonException("failed to get property 'exams'");
                    }
                }
                else
                {
                    throw new JsonException("failed to get property 'data'");
                }
            }
            else
            {
                throw new HttpRequestException(await response.Content.ReadAsStringAsync());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "error getting exams for session: {SessionId}", sessionId);
            throw;
        }
    }

    public void Logout(string sessionId)
    {
        _sessions.Remove(sessionId);
    }
}

public class UserSession
{
    public School School { get; set; } = new();
    public string Username { get; set; } = "";
    public string JSessionId { get; set; } = "";
    public string SchoolId { get; set; } = "";
    public DateTime LoginTime { get; set; }
}

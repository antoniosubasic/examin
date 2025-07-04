namespace backend.Models;

public class LoginRequest
{
    public string School { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class ExamRequest
{
    public string SessionId { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}

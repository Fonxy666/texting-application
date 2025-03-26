namespace UserService.Model.Responses.Auth;

public record AuthResult(
    bool Success,
    string Id,
    string Email
)
{
    public readonly Dictionary<string, string> ErrorMessages = new();
}
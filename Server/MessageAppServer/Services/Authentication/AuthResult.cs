namespace Server.Services.Authentication;

public record AuthResult(
    bool Success,
    string Id
)
{
    public readonly Dictionary<string, string> ErrorMessages = new();
}
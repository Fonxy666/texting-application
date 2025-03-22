namespace AuthenticationService.Model;

public class RefreshToken
{
    public required string Token { get; init; }
    public DateTime Created { get; } = DateTime.UtcNow;
    public DateTime Expires { get; init; }
}
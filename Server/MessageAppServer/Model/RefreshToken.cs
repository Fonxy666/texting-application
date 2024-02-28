namespace Server.Model;

public class RefreshToken
{
    public required string? Token { get; init; }
    public DateTime Created { get; set; } = DateTime.Now;
    public DateTime Expires { get; init; }
}
using Microsoft.AspNetCore.Identity;

namespace Server.Model;

public class ApplicationUser(string? imageUrl) : IdentityUser<Guid>
{
    public string? ImageUrl { get; private set; } = imageUrl;
    public string? RefreshToken { get; private set; } = string.Empty;
    public DateTime? RefreshTokenCreated { get; private set; }
    public DateTime? RefreshTokenExpires { get; private set; }

    public ApplicationUser() : this("-")
    {
    }

    public void SetRefreshToken(string? token)
    {
        RefreshToken = token;
    }

    public void SetRefreshTokenCreated(DateTime? time)
    {
        RefreshTokenCreated = time;
    }
    
    public void SetRefreshTokenExpires(DateTime? time)
    {
        RefreshTokenExpires = time;
    }
}
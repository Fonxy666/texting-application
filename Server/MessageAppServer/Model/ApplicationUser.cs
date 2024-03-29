using Microsoft.AspNetCore.Identity;

namespace Server.Model;

public class ApplicationUser(string imageUrl) : IdentityUser
{
    public string ImageUrl { get; private set; } = imageUrl;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime? RefreshTokenCreated { get; set; }
    public DateTime? RefreshTokenExpires { get; set; }
}
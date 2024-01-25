using Microsoft.AspNetCore.Identity;

namespace Server.Model;

public class ApplicationUser(string imageUrl) : IdentityUser
{
    public string ImageUrl { get; private set; } = imageUrl;
}
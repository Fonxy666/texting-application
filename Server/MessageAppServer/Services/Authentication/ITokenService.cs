using Microsoft.AspNetCore.Identity;

namespace Server.Services.Authentication;

public interface ITokenService
{
    public string CreateToken(IdentityUser user, string? role, bool isTest = false);
}
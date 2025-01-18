using Microsoft.AspNetCore.Identity;
using AuthenticationServer.Model;

namespace AuthenticationServer.Services.Authentication;

public interface ITokenService
{
    public string CreateJwtToken(IdentityUser<Guid> user, string? role, bool rememberMe);
    public RefreshToken CreateRefreshToken();
}
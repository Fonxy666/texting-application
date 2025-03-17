using Microsoft.AspNetCore.Identity;
using AuthenticationService.Model;

namespace AuthenticationService.Services.Authentication;

public interface ITokenService
{
    public string CreateJwtToken(IdentityUser<Guid> user, string? role, bool rememberMe);
    public RefreshToken CreateRefreshToken();
}
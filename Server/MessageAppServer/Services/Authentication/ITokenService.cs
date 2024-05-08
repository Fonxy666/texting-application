using Microsoft.AspNetCore.Identity;
using Server.Model;

namespace Server.Services.Authentication;

public interface ITokenService
{
    public string CreateJwtToken(IdentityUser<Guid> user, string? role, bool rememberMe);
    public RefreshToken CreateRefreshToken();
}
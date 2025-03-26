using Microsoft.AspNetCore.Identity;
using UserService.Model;

namespace UserService.Services.Authentication;

public interface ITokenService
{
    public string CreateJwtToken(IdentityUser<Guid> user, string? role, bool rememberMe);
    public RefreshToken CreateRefreshToken();
}
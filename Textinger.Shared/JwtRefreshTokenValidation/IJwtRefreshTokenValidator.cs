using Microsoft.AspNetCore.Http;

namespace Textinger.Shared.JwtRefreshTokenValidation;

public interface IJwtRefreshTokenValidator
{
    bool ExamineCookies(HttpContext context);
    bool TokenExpired(HttpContext context);
    string? GetRoleFromToken(string token);
}
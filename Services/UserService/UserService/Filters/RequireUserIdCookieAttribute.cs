using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using UserService.Models.Responses;

namespace UserService.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireUserIdCookieAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var userIdClaim = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            context.Result = new UnauthorizedObjectResult(new FailedResponseWithMessage("Unauthorized User."));
            return;
        }

        context.HttpContext.Items["UserId"] = userIdClaim;
    }
}

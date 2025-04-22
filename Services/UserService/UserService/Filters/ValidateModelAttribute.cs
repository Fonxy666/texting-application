using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using UserService.Models.Responses;

namespace UserService.Filters;

public class ValidateModelAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ActionArguments.Values.Any(arg => arg is not null && !IsSimpleType(arg.GetType())))
        {
            return;
        }

        if (!context.ModelState.IsValid)
        {
            context.Result = new BadRequestObjectResult(new FailedResponseWithMessage("Invalid model state."));
        }
    }

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal);
    }
}

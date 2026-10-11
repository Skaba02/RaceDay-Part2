using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Api.Helpers;

namespace RaceDay.Api.Filters;

/// <summary>
/// Blocks the request unless the session shows a logged-in user with the given role.
/// Returns 401 if nobody is logged in, or 403 if the logged-in user has the wrong role.
/// </summary>
public class RequireRoleAttribute : ActionFilterAttribute
{
    private readonly string _role;

    public RequireRoleAttribute(string role)
    {
        _role = role;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var userId = context.HttpContext.Session.GetInt32(SessionKeys.UserId);
        if (userId == null)
        {
            context.Result = new UnauthorizedObjectResult(
                new { message = "You must be logged in to access this endpoint." });
            return;
        }

        var role = context.HttpContext.Session.GetString(SessionKeys.Role);
        if (!string.Equals(role, _role, StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new ObjectResult(
                new { message = $"This endpoint is only available to the {_role} role." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}

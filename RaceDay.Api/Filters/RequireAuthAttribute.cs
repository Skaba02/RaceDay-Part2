using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Api.Helpers;

namespace RaceDay.Api.Filters;

/// <summary>
/// Blocks the request unless the session shows a logged-in user.
/// Use on any endpoint that just needs "someone is logged in", regardless of role.
/// </summary>
public class RequireAuthAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var userId = context.HttpContext.Session.GetInt32(SessionKeys.UserId);
        if (userId == null)
        {
            context.Result = new UnauthorizedObjectResult(
                new { message = "You must be logged in to access this endpoint." });
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Helpers;

namespace RaceDay.Web.Filters;

/// <summary>Sends visitors who are not logged in to the login page.</summary>
public class RequireLoginAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;

        if (session.GetInt32(SessionKeys.UserId) == null)
        {
            var request = context.HttpContext.Request;
            var returnUrl = request.Method == HttpMethods.Get ? request.Path + request.QueryString : null;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
        }
    }
}

/// <summary>Only lets the given role in. Other roles see the "access denied" page.</summary>
public class RequireRoleAttribute : ActionFilterAttribute
{
    private readonly string _role;

    public RequireRoleAttribute(string role)
    {
        _role = role;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;

        if (session.GetInt32(SessionKeys.UserId) == null)
        {
            var request = context.HttpContext.Request;
            var returnUrl = request.Method == HttpMethods.Get ? request.Path + request.QueryString : null;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (session.GetString(SessionKeys.Role) != _role)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}

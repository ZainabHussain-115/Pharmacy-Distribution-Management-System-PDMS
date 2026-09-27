using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PDMS.Filters
{
    // ── BASE: Requires login ──────────────────────────────────────────
    public class RequireLoginAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            if (session.GetString("UserID") == null)
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }
            base.OnActionExecuting(context);
        }
    }

    // ── ADMIN ONLY: Redirects Staff to Access Denied ──────────────────
    public class AdminOnlyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;

            if (session.GetString("UserID") == null)
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            if (session.GetString("Role") != "Admin")
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}

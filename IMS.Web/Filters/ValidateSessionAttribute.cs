using System.Web.Mvc;

namespace IMS.Web.Filters
{
    public class ValidateSessionAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var allowAnonymous = filterContext.ActionDescriptor.IsDefined(typeof(AllowAnonymousAttribute), true) ||
            filterContext.ActionDescriptor.ControllerDescriptor.IsDefined(
            typeof(AllowAnonymousAttribute), true);
            if (!allowAnonymous)
            {
                // Check if session is null or expired
                if (filterContext.HttpContext.Session["UserName"] == null)
                {
                    // Prevent a redirection loop by checking if the current request is already for the login page
                    var controller = filterContext.RouteData.Values["controller"].ToString();
                    var action = filterContext.RouteData.Values["action"].ToString();
                    if (controller != "Authentication" || action != "RedirectToLogin")
                    {
                        // Redirect to the login page if session is null and user is not already on the login page
                        filterContext.Result = new RedirectToRouteResult(
                        new System.Web.Routing.RouteValueDictionary(new { controller = "Login", action = "SessionAlert" })
                        );
                    }
                }
                base.OnActionExecuting(filterContext);
            }
        }
    }
}
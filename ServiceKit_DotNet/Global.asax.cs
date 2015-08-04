using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace ServiceKit_DotNet
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RegisterRoutes(RouteTable.Routes);
        }

        // Defining Routes
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("favicon.ico");
            // Gets the details of the customer
            routes.MapRoute("HelpQd", "help/getDetails", new { controller = "Home", action = "HelpDequeue" });
            // Deletes the queueId when a window is closed or user clicks cancel button 
            routes.MapRoute("HelpQdelete", "help/deleteq", new { controller = "Home", action = "DeleteQueueId" });
            // Adds the sessionId to the cloudQueue
            routes.MapRoute("HelpQ", "help/queue", new { controller = "Home", action = "HelpQueue" });
            // Create a help Session 
            routes.MapRoute("HelpS", "help/session", new { controller = "Home", action = "HelpSession" });
            // Layout for the customer representatives page
            routes.MapRoute("Rep", "rep", new {controller = "Home" , action = "Rep"});
            // Layout or the Customer page
            routes.MapRoute("Index", "Index", new { controller = "Home", action = "Index" });
            routes.MapRoute("Default", "{*path}", new { controller = "Home", action = "Index" });
        }
    }
}

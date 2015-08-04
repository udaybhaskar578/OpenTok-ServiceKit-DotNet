using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using OpenTokSDK;

namespace ServiceKit_DotNet
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
           
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
    // Configure all the variables accordingly
    public class Settings
    {
        public static string StorageAccountConnection = ConfigurationManager.AppSettings["CloudStorage"];
        public static CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(StorageAccountConnection);
        // Replace these values with the values from your TokBox Dashboard
        const  string Secret = " ";
        const int ApiKey = 00000000;
        public static OpenTok Tk = new OpenTok(ApiKey, Secret);
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace SOMOID
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            config.Formatters.XmlFormatter.SupportedMediaTypes.Add(new System.Net.Http.Headers.MediaTypeHeaderValue("application/xml"));
            config.Formatters.XmlFormatter.UseXmlSerializer = true;
            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/somiod"
            );

           
        }
    }
}

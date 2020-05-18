
namespace RM.Razor {

    using Microsoft.AspNetCore.Mvc.Razor;
    using System;
    using System.Collections.Generic;

    public class MultiTenantRazorViewEngineOptions : RazorViewEngineOptions {

        // A pair of strings to match the HttpContextItemsKey value to the view library
        // ie  in app.settings you would set it like this (Im basing mine on hostnames)
        // "MobileConfigInfo": {
        //   "Values": {          
        //          "localhost" : "RM.Razor.ViewLibrary1",
        //          "somesite.com" : "RM.Razor.ViewLibrary2"
        //   }
        // }
        // The HttpContext.Items[HttpContextItemsKey] is set in middleware. The provided example uses the hostname to determine
        // which library to use but custom middleware could set this value based on any given criteria (think A/B testing or Localization etc)

        public List<ViewLibraryInfo> ViewLibraries { get;set;}


        // Add your configuration here. The Dictionary key is the value of HttpContext.Items[HttpContextItemsKey]
        // which is set in the middleware. The string array is a list of Assembly Names that should be 
        // searched (in order) before falling back to the Default Library
        public Dictionary<string, string[]> ViewLibraryConfig { get; set; }


        // The default library that is used as a fallback for any missing views in the chosen library
        // If this is left blank then it will default to the assembly that is running the MVC application as the default
        // Only change this if you want your default views to be in a separate Razor Class Library. Either way you should 
        // ensure that the default library has all the views required in it to fallback to.
        public ViewLibraryInfo DefaultViewLibrary { get; set; }

        // This is the key used in the HttpContext.Items collection to choose the ViewLibrary to use for any given request
        public string HttpContextItemsKey { get; set; } = "ViewLibrary";

        // This is the sliding Cache expiration for the Views MemoryCache.
        // Items will expire after 20 mins of being in the cache and not requested.       
        public TimeSpan CacheExpirationDuration { get; set; } = TimeSpan.FromMinutes(20);
    }

}

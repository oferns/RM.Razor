using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RM.Razor.Mvc.Middleware;
using RM.Mvc.Models;
using Microsoft.Net.Http.Headers;

namespace RM.Razor.Mvc.Controllers {
    public class HomeController : Controller {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger) {
            _logger = logger;
        }

        public IActionResult Index() {
            return View();
        }

        public IActionResult Privacy() {
            return View();
        }

#if DEBUG
        public IActionResult ChangeConfig(string configName) {

            ControllerContext.HttpContext.Response.Cookies.Append(CookieBasedViewLibrarySelectorMiddleware.CookieKey, configName);


            if (!ControllerContext.HttpContext.Request.Headers.TryGetValue(HeaderNames.Referer, out var referrer)) {
                referrer = "/";
            }

            return Redirect(referrer);        
        }
#endif
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

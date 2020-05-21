using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RM.Mvc.Middleware;
using RM.Mvc.Components;

namespace RM.Mvc.Controllers {
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
        public IActionResult ChangeHost(string hostName) {

            ControllerContext.HttpContext.Response.Cookies.Append(CookieBasedViewLibrarySelectorMiddleware.CookieKey, hostName);
            
            var referer = "/";

            if (Url.IsLocalUrl(ControllerContext.HttpContext.Request.Headers["Referer"])){
                referer = ControllerContext.HttpContext.Request.Headers["Referer"];
            }
            return Redirect(referer);        
        }
#endif
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

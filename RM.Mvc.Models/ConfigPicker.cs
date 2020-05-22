namespace RM.Mvc.Models {

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using RM.Razor;
    using System;

    [ViewComponent(Name = "ConfigPicker")]
    public class ConfigPicker : ViewComponent {

        private readonly RazorMultiViewEngineOptions options;
        public ConfigPicker(IOptions<RazorMultiViewEngineOptions> optionsAccessor) {
            this.options = optionsAccessor?.Value ?? throw new ArgumentNullException(nameof(optionsAccessor));
        }

        public IViewComponentResult Invoke() {
            return View(this.options);
        }
    }
}

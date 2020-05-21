using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using RM.Razor;

namespace RM.Mvc.Components {
    [ViewComponent(Name ="ViewLibraryPicker")]
    public class ViewLibraryPickerViewComponent : ViewComponent {


        public ViewLibraryPickerViewComponent(IOptions<MultiTenantRazorViewEngineOptions> optionsAccessor) { 
        
        
        }




    }
}

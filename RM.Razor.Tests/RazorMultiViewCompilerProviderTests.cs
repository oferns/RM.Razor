namespace RM.Razor.Tests {
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc.Razor;
    using Microsoft.AspNetCore.Mvc.Razor.Compilation;
    using Microsoft.Extensions.Primitives;
    using Moq;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Xunit;

    public class RazorMultiViewCompilerProviderTests {

        // Should get the compiler for the Default Library when HttpContext Items key is not present

        // Should get the correct compiler for the Default Library when HttpContext Items key is present and valid in config

        // Should get the compiler for the Default Library when HttpContext Items key is present but not valid in config




        private IHttpContextAccessor GetWithItems(Dictionary<object, object> items) {
            var mockAccessor = new Mock<IHttpContextAccessor>();
            mockAccessor.Setup(m => m.HttpContext).Returns(new DefaultHttpContext() { Items = items });
            return mockAccessor.Object;        
        }

    }
}

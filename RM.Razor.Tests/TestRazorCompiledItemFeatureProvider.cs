
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Razor.Hosting;
namespace RM.Razor.Tests {
    public class TestRazorCompiledItemFeatureProvider : IApplicationFeatureProvider<ViewsFeature> {
        private readonly IEnumerable<RazorCompiledItem> compiledItems;

        public TestRazorCompiledItemFeatureProvider(IEnumerable<RazorCompiledItem> compiledItems) {
            this.compiledItems = compiledItems;
        }

        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ViewsFeature feature) {

            foreach (var item in this.compiledItems) {
                var descriptor = new CompiledViewDescriptor(item);
                feature.ViewDescriptors.Add(descriptor);
            }
        }
    }
}

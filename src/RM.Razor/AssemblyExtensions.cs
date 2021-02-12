using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Razor.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RM.Razor {
    public static class AssemblyExtensions {

        public static IEnumerable<Assembly> EnsureRazorAssemblies(this Assembly assembly) {

            var relatedAssemblyAttributes = assembly.GetCustomAttributes<RelatedAssemblyAttribute>();
            if (!relatedAssemblyAttributes.Any()) {
                yield break;
            }
            foreach (var relatedAssemblyAttribute in relatedAssemblyAttributes) {
                var relatedAssembly = AppDomain.CurrentDomain.EnsureAssembly(relatedAssemblyAttribute.AssemblyFileName);
                if (relatedAssembly.GetCustomAttributes<RazorCompiledItemAttribute>().Any()) {
                    yield return relatedAssembly;
                }
            }

        }
    }
}

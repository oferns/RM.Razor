using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RM.Razor {
    public static class AssemblyExtensions {

        public static IEnumerable<Assembly> GetRazorAssemblies(this Assembly assembly) {

            var asses = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var relatedAssembly in asses.Where(a => a.GetCustomAttributes<RelatedAssemblyAttribute>().Any())) {

                foreach (var relatedAttribute in relatedAssembly.GetCustomAttributes<RelatedAssemblyAttribute>()) {
                    var ass = asses.Where(a => a.GetName().Name.Equals(relatedAttribute.AssemblyFileName)).SingleOrDefault();
                    
                    if (ass is null) {
                        ass = Assembly.LoadFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relatedAttribute.AssemblyFileName + ".dll"));
                    }

                    if (ass is object)
                        yield return ass;
                }
            }
        }
    }
}

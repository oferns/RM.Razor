using System;
using System.IO;
using System.Reflection;

namespace RM.Razor {

    public static class AppDomainExtensions {

        public static Assembly EnsureAssembly(this AppDomain domain, string assemblyName) {

            var assemblies = domain.GetAssemblies();

            foreach (var assembly in assemblies) {
                if (assembly.GetName().Name.Equals(assemblyName, StringComparison.Ordinal)) { // Must match exactly
                    return assembly;
                }            
            }
                       
            return Assembly.LoadFile(Path.Combine(Assembly.GetExecutingAssembly().Location, assemblyName));                
        }
    }
}

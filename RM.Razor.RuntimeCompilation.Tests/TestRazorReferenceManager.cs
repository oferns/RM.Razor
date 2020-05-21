/// <summary>
/// src/Mvc/Mvc.Razor.RuntimeCompilation/test/TestInfrastructure/TestRazorReferenceManager.cs
/// </summary>
namespace RM.Razor.RuntimeCompilation.Tests {
    using System;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Mvc.ApplicationParts;
    using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
    using Microsoft.CodeAnalysis;
    using Microsoft.Extensions.Options;

    public class TestRazorReferenceManager : PublicRazorReferenceManager {
        public TestRazorReferenceManager()
            : base(
                new ApplicationPartManager(),
                Options.Create(new MvcRazorRuntimeCompilationOptions())) {
            CompilationReferences = Array.Empty<MetadataReference>();
        }

        public override IReadOnlyList<MetadataReference> CompilationReferences { get; }
    }
}
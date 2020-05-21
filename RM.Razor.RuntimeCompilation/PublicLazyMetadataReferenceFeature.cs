/// <summary>
/// A public version of src/Mvc/Mvc.Razor.RuntimeCompilation/src/LazyMetadataReferenceFeature.cs
/// </summary>

namespace RM.Razor.RuntimeCompilation {
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Razor.Language;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Razor;

    public class PublicLazyMetadataReferenceFeature : IMetadataReferenceFeature {
        private readonly PublicRazorReferenceManager referenceManager;

        public PublicLazyMetadataReferenceFeature(PublicRazorReferenceManager referenceManager) {
            this.referenceManager = referenceManager;
        }

        /// <remarks>
        /// Invoking <see cref="RazorReferenceManager.CompilationReferences"/> ensures that compilation
        /// references are lazily evaluated.
        /// </remarks>
        public IReadOnlyList<MetadataReference> References => this.referenceManager.CompilationReferences;

        public RazorEngine Engine { get; set; }
    }
}

using System;
using Xunit;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Moq;

namespace RM.Razor.RuntimeCompilation.Tests {
    public class PublicRazorReferenceManagerTests {
        private static readonly string ApplicationPartReferencePath = "some-path";

        [Fact]
        public void GetCompilationReferences_CombinesApplicationPartAndOptionMetadataReferences() {
            // Arrange
            var options = new MvcRazorRuntimeCompilationOptions();
            var additionalReferencePath = "additional-path";
            options.AdditionalReferencePaths.Add(additionalReferencePath);

            var applicationPartManager = GetApplicationPartManager();
            var referenceManager = new PublicRazorReferenceManager(
                applicationPartManager,
                Options.Create(options));

            var expected = new[] { ApplicationPartReferencePath, additionalReferencePath };

            // Act
            var references = referenceManager.GetReferencePaths();

            // Assert
            Assert.Equal(expected, references);
        }

        private static ApplicationPartManager GetApplicationPartManager() {
            var applicationPartManager = new ApplicationPartManager();
            var part = new Mock<ApplicationPart>();

            part.As<ICompilationReferencesProvider>()
                .Setup(p => p.GetReferencePaths())
                .Returns(new[] { ApplicationPartReferencePath });

            applicationPartManager.ApplicationParts.Add(part.Object);

            return applicationPartManager;
        }
    }
}

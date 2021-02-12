
namespace RM.Razor.Tests {

    using Xunit;
    public class PublicViewEnginePathTests {


        [Theory]
        [InlineData("Views/../Home/Index.cshtml", "/Home/Index.cshtml")]
        [InlineData("/Views/Home/../Shared/Partial.cshtml", "/Views/Shared/Partial.cshtml")]
        [InlineData("/Views/Shared/./Partial.cshtml", "/Views/Shared/Partial.cshtml")]
        [InlineData("//Views/Index.cshtml", "/Views/Index.cshtml")]
        public void ResolvePath_ResolvesPathTraversals(string input, string expected) {
            // Arrange & Act
            var result = PublicViewEnginePath.ResolvePath(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("../Index.cshtml")]
        [InlineData("Views/../../Index.cshtml")]
        [InlineData("Views/../Shared/../../Index.cshtml")]
        public void ResolvePath_DoesNotTraversePastApplicationRoot(string input) {
            // Arrange
            var result = PublicViewEnginePath.ResolvePath(input);

            // Assert
            Assert.Same(input, result);
        }

        [Theory]
        [InlineData("/Views/Index.cshtml")]
        [InlineData(@"/Views\Index.cshtml")]
        [InlineData("Index..cshtml")]
        [InlineData("/directory.with.periods/sub-dir/index.cshtml")]
        [InlineData("file.with.periods.cshtml")]
        public void ResolvePath_DoesNotModifyPathsWithoutTraversals(string input) {
            // Arrange & Act
            var result = PublicViewEnginePath.ResolvePath(input);

            // Assert
            Assert.Same(input, result);
        }
    }
}

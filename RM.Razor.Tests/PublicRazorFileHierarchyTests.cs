namespace RM.Razor.Tests {

    using Xunit;

    public class PublicRazorFileHierarchyTests {
        [Fact]
        public void GetViewStartPaths_ForFileAtRoot() {
            // Arrange
            var expected = new[] { "/_ViewStart.cshtml", };
            var path = "/Home.cshtml";

            // Act
            var actual = PublicRazorFileHierarchy.GetViewStartPaths(path);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetViewStartPaths_ForForFileInViewsDirectory() {
            // Arrange
            var expected = new[]
            {
                "/Views/Home/_ViewStart.cshtml",
                "/Views/_ViewStart.cshtml",
                "/_ViewStart.cshtml",
            };
            var path = "/Views/Home/Index.cshtml";

            // Act
            var actual = PublicRazorFileHierarchy.GetViewStartPaths(path);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetViewStartPaths_ForForFileInAreasDirectory() {
            // Arrange
            var expected = new[]
            {
                "/Areas/Views/MyArea/Home/_ViewStart.cshtml",
                "/Areas/Views/MyArea/_ViewStart.cshtml",
                "/Areas/Views/_ViewStart.cshtml",
                "/Areas/_ViewStart.cshtml",
                "/_ViewStart.cshtml",
            };
            var path = "/Areas/Views/MyArea/Home/Index.cshtml";

            // Act
            var actual = PublicRazorFileHierarchy.GetViewStartPaths(path);

            // Assert
            Assert.Equal(expected, actual);
        }
    }
}

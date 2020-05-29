namespace RM.Razor.Tests {
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc.ApplicationParts;
    using Microsoft.AspNetCore.Mvc.Razor;
    using Microsoft.AspNetCore.Mvc.Razor.Compilation;
    using Microsoft.AspNetCore.Razor.Hosting;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Primitives;
    using Moq;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Xunit;

    public class RazorMultiViewCompilerProviderTests {

        // Should get the compiler for the Default Library when HttpContext Items key is not present

        [Fact]
        public void ShouldGetDefaultCompilerWhenNoKeyPresent() {
            // Arrange
            var partsMan = GetApplicationPartManager(Array.Empty<RazorCompiledItem>());
            var options = new RazorMultiViewEngineOptions();
            var accessorWithItems = GetWithItems(new Dictionary<object, object>());

            // Act
            var provider = new RazorMultiViewCompilerProvider(partsMan, accessorWithItems, Options.Create(options));

            // Assert
            Assert.Single(provider.compilers);
            Assert.True(provider.compilers.ContainsKey("default"));
            Assert.Equal<IViewCompiler>(provider.GetCompiler(), provider.compilers["default"]);
        }

        [Fact]
        public void ShouldGetKeyedCompilerWhenConfigValid() {

            // Arrange
            var assembly1 = typeof(RazorMultiViewCompilerProviderTests).Assembly.GetName().Name; // Default assembly
            var assembly2 = typeof(RazorMultiViewEngineOptions).Assembly.GetName().Name; // Conifgured assembly

            var options = new RazorMultiViewEngineOptions { 
                DefaultViewLibrary = new ViewLibraryInfo { AssemblyName = assembly1 },
                ViewLibraryConfig = new Dictionary<string, string[]> { { "test", new[] { assembly2 } } }
            };

            var accessorWithItems = GetWithItems(new Dictionary<object, object> { { options.HttpContextItemsKey, "test" } });
            var partsMan = GetApplicationPartManager(Array.Empty<RazorCompiledItem>());

            // Act
            var provider = new RazorMultiViewCompilerProvider(partsMan, accessorWithItems, Options.Create(options));

            // Assert
            Assert.True(provider.compilers.ContainsKey("default"));
            Assert.True(provider.compilers.ContainsKey("test"));
            Assert.Equal<IViewCompiler>(provider.GetCompiler(), provider.compilers["test"]);

        }

        [Fact]
        public void ShouldGetDefaultCompilerWhenConfigValidButKeyNotRecognised() {

            // Arrange
            var assembly1 = typeof(RazorMultiViewCompilerProviderTests).Assembly.GetName().Name; // Default assembly
            var assembly2 = typeof(RazorMultiViewEngineOptions).Assembly.GetName().Name; // Conifgured assembly

            var options = new RazorMultiViewEngineOptions {
                DefaultViewLibrary = new ViewLibraryInfo { AssemblyName = assembly1 },
                ViewLibraryConfig = new Dictionary<string, string[]> { { "test", new[] { assembly2 } } }
            };

            var accessorWithItems = GetWithItems(new Dictionary<object, object> { { options.HttpContextItemsKey, "no-test" } });
            var partsMan = GetApplicationPartManager(Array.Empty<RazorCompiledItem>());

            // Act
            var provider = new RazorMultiViewCompilerProvider(partsMan, accessorWithItems, Options.Create(options));

            // Assert
            Assert.True(provider.compilers.ContainsKey("default"));
            Assert.True(provider.compilers.ContainsKey("test"));

            Assert.Equal<IViewCompiler>(provider.GetCompiler(), provider.compilers["default"]);

        }


        [Fact]
        public async Task ShouldIgnoreDuplicatePathsCaseInsensitive() {

            // Arrange
            var assembly1 = typeof(RazorMultiViewCompilerProviderTests).Assembly.GetName().Name; // Default assembly
            var assembly2 = typeof(RazorMultiViewEngineOptions).Assembly.GetName().Name; // Conifgured assembly

            var view1 = TestRazorCompiledItem.CreateForView(typeof(RazorMultiViewCompilerProviderTests), "test1");
            var view2 = TestRazorCompiledItem.CreateForView(typeof(RazorMultiViewCompilerProviderTests), "TEST1");


            var options = new RazorMultiViewEngineOptions {
                DefaultViewLibrary = new ViewLibraryInfo { AssemblyName = assembly1 },
                ViewLibraryConfig = new Dictionary<string, string[]> { { "test", new[] { assembly2 } } }
            };

            var accessorWithItems = GetWithItems(new Dictionary<object, object> { { options.HttpContextItemsKey, "test" } });
            var partsMan = GetApplicationPartManager(new[] { view1, view2 });

            // Act
            var provider = new RazorMultiViewCompilerProvider(partsMan, accessorWithItems, Options.Create(options));
            var compiledView1 = await provider.GetCompiler().CompileAsync("/test1");
            var compiledView2 = await provider.GetCompiler().CompileAsync("/TEST1");

            Assert.Equal(view1, compiledView1.Item);
            Assert.Equal(view1, compiledView2.Item);

        }




        [Fact]
        public async Task ShouldRespectConfigWhenReturningCompiledViewsWithTheSamePath() {

            // Arrange
            var assembly1 = typeof(RazorMultiViewCompilerProviderTests).Assembly.GetName().Name; // Default assembly
            var assembly2 = typeof(RazorMultiViewEngineOptions).Assembly.GetName().Name; // Conifgured assembly

            var view1 = TestRazorCompiledItem.CreateForView(typeof(RazorMultiViewCompilerProviderTests), "test1");
            var view2 = TestRazorCompiledItem.CreateForView(typeof(RazorMultiViewEngineOptions), "test1");

            var options = new RazorMultiViewEngineOptions {
                DefaultViewLibrary = new ViewLibraryInfo { AssemblyName = assembly1 },
                ViewLibraryConfig = new Dictionary<string, string[]> { { "test", new[] { assembly2 } } }
            };

            var accessorWithItems = GetWithItems(new Dictionary<object, object> { { options.HttpContextItemsKey, "test" } });
            var partsMan = GetApplicationPartManager(new[] { view1, view2 });

            // Act
            var provider = new RazorMultiViewCompilerProvider(partsMan, accessorWithItems, Options.Create(options));
            var compiledView = await provider.GetCompiler().CompileAsync("/test1");
            Assert.Equal(view2, compiledView.Item);
        }

        [Fact]
        public async Task ShouldRevertToDefaultViewsWhenPathDoesNotExistInSpecifiedLibrary() {
            // Arrange
            var assembly1 = typeof(RazorMultiViewCompilerProviderTests).Assembly.GetName().Name; // Default assembly
            var assembly2 = typeof(RazorMultiViewEngineOptions).Assembly.GetName().Name; // Conifgured assembly

            var view1 = TestRazorCompiledItem.CreateForView(typeof(RazorMultiViewCompilerProviderTests), "test1");
            var view2 = TestRazorCompiledItem.CreateForView(typeof(RazorMultiViewEngineOptions), "test2");
            

            var options = new RazorMultiViewEngineOptions {
                DefaultViewLibrary = new ViewLibraryInfo { AssemblyName = assembly1 },
                ViewLibraryConfig = new Dictionary<string, string[]> { { "test", new[] { assembly2 } } }
            };

            var accessorWithItems = GetWithItems(new Dictionary<object, object> { { options.HttpContextItemsKey, "test" } });
            var partsMan = GetApplicationPartManager(new[] { view1, view2 });

            // Act
            var provider = new RazorMultiViewCompilerProvider(partsMan, accessorWithItems, Options.Create(options));
            var compiledView1 = await provider.GetCompiler().CompileAsync("/test1");
            var compiledView2 = await provider.GetCompiler().CompileAsync("/test2");

            Assert.Equal(view1, compiledView1.Item);
            Assert.Equal(view2, compiledView2.Item);
        }

        private static ApplicationPartManager GetApplicationPartManager(IEnumerable<RazorCompiledItem> compiledItems) {

            var applicationPartManager = new ApplicationPartManager();
            var featureProvider = new TestRazorCompiledItemFeatureProvider(compiledItems);
            applicationPartManager.FeatureProviders.Add(featureProvider);
            return applicationPartManager;
        }

        private IHttpContextAccessor GetWithItems(Dictionary<object, object> items) {
            var mockAccessor = new Mock<IHttpContextAccessor>();
            mockAccessor.Setup(m => m.HttpContext).Returns(new DefaultHttpContext() { Items = items });
            return mockAccessor.Object;
        }
    }
}

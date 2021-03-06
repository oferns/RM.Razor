﻿namespace RM.Razor {

    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Razor;
    using Microsoft.AspNetCore.Mvc.ViewEngines;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Primitives;
    using System;    
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text.Encodings.Web;

    public class RazorMultiViewEngine : IRazorViewEngine {

        public static readonly string ViewExtension = ".cshtml";
        private const string AreaKey = "area";
        private const string ControllerKey = "controller";
        private const string PageKey = "page";

        private readonly IRazorPageFactoryProvider pageFactory;
        private readonly IRazorPageActivator pageActivator;
        private readonly HtmlEncoder htmlEncoder;
        private readonly RazorMultiViewEngineOptions options;
        private readonly DiagnosticListener diagnosticListener;


        public RazorMultiViewEngine(IHttpContextAccessor contextAccessor,
                                            IRazorPageFactoryProvider pageFactory,
                                            IRazorPageActivator pageActivator,
                                            HtmlEncoder htmlEncoder,
                                            IOptions<RazorMultiViewEngineOptions> optionsAccessor,
                                            DiagnosticListener diagnosticListener) {


            this.pageFactory = pageFactory ?? throw new ArgumentNullException(nameof(pageFactory));
            this.pageActivator = pageActivator ?? throw new ArgumentNullException(nameof(pageActivator));
            this.htmlEncoder = htmlEncoder ?? throw new ArgumentNullException(nameof(htmlEncoder));
            this.options = optionsAccessor?.Value ?? throw new ArgumentNullException(nameof(optionsAccessor));
            this.diagnosticListener = diagnosticListener ?? throw new ArgumentNullException(nameof(diagnosticListener));

            if (contextAccessor is null) {
                throw new ArgumentNullException(nameof(contextAccessor));
            }

            this.ViewLookupCache = new MultiTenantMemoryCache(contextAccessor, options.HttpContextItemsKey);

        }

        protected internal IMemoryCache ViewLookupCache { get; }

        /// <summary>
        /// Gets the case-normalized route value for the specified route <paramref name="key"/>.
        /// </summary>
        /// <param name="context">The <see cref="ActionContext"/>.</param>
        /// <param name="key">The route key to lookup.</param>
        /// <returns>The value corresponding to the key.</returns>
        /// <remarks>
        /// The casing of a route value in <see cref="ActionContext.RouteData"/> is determined by the client.
        /// This making constructing paths for view locations in a case sensitive file system unreliable. Using the
        /// <see cref="Abstractions.ActionDescriptor.RouteValues"/> to get route values
        /// produces consistently cased results.
        /// </remarks>
        public static string GetNormalizedRouteValue(ActionContext context, string key) => RazorViewEngine.GetNormalizedRouteValue(context, key);

        public RazorPageResult FindPage(ActionContext context, string pageName) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            if (string.IsNullOrEmpty(pageName)) {
                throw new ArgumentException("Value cannot be null or empty.", nameof(pageName));
            }

            if (IsApplicationRelativePath(pageName) || IsRelativePath(pageName)) {
                // A path; not a name this method can handle.
                return new RazorPageResult(pageName, Enumerable.Empty<string>());
            }

            var cacheResult = LocatePageFromViewLocations(context, pageName, isMainPage: false);
            if (cacheResult.Success) {
                var razorPage = cacheResult.ViewEntry.PageFactory();
                return new RazorPageResult(pageName, razorPage);
            } else {
                return new RazorPageResult(pageName, cacheResult.SearchedLocations);
            }
        }

        public RazorPageResult GetPage(string executingFilePath, string pagePath) {
            if (string.IsNullOrEmpty(pagePath)) {
                throw new ArgumentException(nameof(pagePath));
            }

            if (!(IsApplicationRelativePath(pagePath) || IsRelativePath(pagePath))) {
                // Not a path this method can handle.
                return new RazorPageResult(pagePath, Enumerable.Empty<string>());
            }

            var cacheResult = LocatePageFromPath(executingFilePath, pagePath, isMainPage: false);
            if (cacheResult.Success) {
                var razorPage = cacheResult.ViewEntry.PageFactory();
                return new RazorPageResult(pagePath, razorPage);
            } else {
                return new RazorPageResult(pagePath, cacheResult.SearchedLocations);
            }
        }

        public ViewEngineResult FindView(ActionContext context, string viewName, bool isMainPage) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            if (string.IsNullOrEmpty(viewName)) {
                throw new ArgumentException("Value cannot be null or empty.", nameof(viewName));
            }

            if (IsApplicationRelativePath(viewName) || IsRelativePath(viewName)) {
                // A path; not a name this method can handle.
                return ViewEngineResult.NotFound(viewName, Enumerable.Empty<string>());
            }

            var cacheResult = LocatePageFromViewLocations(context, viewName, isMainPage);
            return CreateViewEngineResult(cacheResult, viewName);
        }

        public ViewEngineResult GetView(string executingFilePath, string viewPath, bool isMainPage) {
            if (string.IsNullOrEmpty(viewPath)) {
                throw new ArgumentException(nameof(viewPath));
            }

            if (!(IsApplicationRelativePath(viewPath) || IsRelativePath(viewPath))) {
                // Not a path this method can handle.
                return ViewEngineResult.NotFound(viewPath, Array.Empty<string>());
            }

            var cacheResult = LocatePageFromPath(executingFilePath, viewPath, isMainPage);
            return CreateViewEngineResult(cacheResult, viewPath);
        }

        public string GetAbsolutePath(string executingFilePath, string pagePath) {
            if (string.IsNullOrEmpty(pagePath)) {
                // Path is not valid; no change required.
                return pagePath;
            }

            if (IsApplicationRelativePath(pagePath)) {
                // An absolute path already; no change required.
                return pagePath;
            }

            if (!IsRelativePath(pagePath)) {
                // A page name; no change required.
                return pagePath;
            }

            if (string.IsNullOrEmpty(executingFilePath)) {
                // Given a relative path i.e. not yet application-relative (starting with "~/" or "/"), interpret
                // path relative to currently-executing view, if any.
                // Not yet executing a view. Start in app root.
                var absolutePath = "/" + pagePath;
                return PublicViewEnginePath.ResolvePath(absolutePath);
            }

            return PublicViewEnginePath.CombinePath(executingFilePath, pagePath);
        }

        // internal for tests < public coz wtf not?
        internal IEnumerable<string> GetViewLocationFormats(ViewLocationExpanderContext context) {
            if (!string.IsNullOrEmpty(context.AreaName) &&
                !string.IsNullOrEmpty(context.ControllerName)) {
                return options.AreaViewLocationFormats;
            } else if (!string.IsNullOrEmpty(context.ControllerName)) {
                return options.ViewLocationFormats;
            } else if (!string.IsNullOrEmpty(context.AreaName) &&
                  !string.IsNullOrEmpty(context.PageName)) {
                return options.AreaPageViewLocationFormats;
            } else if (!string.IsNullOrEmpty(context.PageName)) {
                return options.PageViewLocationFormats;
            } else {
                // If we don't match one of these conditions, we'll just treat it like regular controller/action
                // and use those search paths. This is what we did in 1.0.0 without giving much thought to it.
                return options.ViewLocationFormats;
            }
        }

        private PublicViewLocationCacheResult LocatePageFromPath(string executingFilePath, string pagePath, bool isMainPage) {
            var applicationRelativePath = GetAbsolutePath(executingFilePath, pagePath);
            var cacheKey = new PublicViewLocationCacheKey(applicationRelativePath, isMainPage);
            if (!ViewLookupCache.TryGetValue(cacheKey, out PublicViewLocationCacheResult cacheResult)) {
                var expirationTokens = new HashSet<IChangeToken>();
                cacheResult = CreateCacheResult(expirationTokens, applicationRelativePath, isMainPage);

                var cacheEntryOptions = new MemoryCacheEntryOptions();
                cacheEntryOptions.SetSlidingExpiration(this.options.CacheExpirationDuration);
                foreach (var expirationToken in expirationTokens) {
                    cacheEntryOptions.AddExpirationToken(expirationToken);
                }

                // No views were found at the specified location. Create a not found result.
                if (cacheResult == null) {
                    cacheResult = new PublicViewLocationCacheResult(new[] { applicationRelativePath });
                }

                cacheResult = ViewLookupCache.Set(
                    cacheKey,
                    cacheResult,
                    cacheEntryOptions);
            }

            return cacheResult;
        }

        private PublicViewLocationCacheResult LocatePageFromViewLocations(ActionContext actionContext, string pageName, bool isMainPage) {
            var controllerName = GetNormalizedRouteValue(actionContext, ControllerKey);
            var areaName = GetNormalizedRouteValue(actionContext, AreaKey);
            string razorPageName = null;
            if (actionContext.ActionDescriptor.RouteValues.ContainsKey(PageKey)) {
                // Only calculate the Razor Page name if "page" is registered in RouteValues.
                razorPageName = GetNormalizedRouteValue(actionContext, PageKey);
            }

            var expanderContext = new ViewLocationExpanderContext(
                actionContext,
                pageName,
                controllerName,
                areaName,
                razorPageName,
                isMainPage);
            Dictionary<string, string> expanderValues = null;

            var expanders = options.ViewLocationExpanders;
            // Read interface .Count once rather than per iteration
            var expandersCount = expanders.Count;
            if (expandersCount > 0) {
                expanderValues = new Dictionary<string, string>(StringComparer.Ordinal);
                expanderContext.Values = expanderValues;

                // Perf: Avoid allocations
                for (var i = 0; i < expandersCount; i++) {
                    expanders[i].PopulateValues(expanderContext);
                }
            }

            var cacheKey = new PublicViewLocationCacheKey(
                expanderContext.ViewName,
                expanderContext.ControllerName,
                expanderContext.AreaName,
                expanderContext.PageName,
                expanderContext.IsMainPage,
                expanderValues);

            if (!ViewLookupCache.TryGetValue(cacheKey, out PublicViewLocationCacheResult cacheResult)) {
                cacheResult = OnCacheMiss(expanderContext, cacheKey);
            }

            return cacheResult;
        }

        private PublicViewLocationCacheResult OnCacheMiss(ViewLocationExpanderContext expanderContext, PublicViewLocationCacheKey cacheKey) {
            var viewLocations = GetViewLocationFormats(expanderContext);

            var expanders = options.ViewLocationExpanders;
            // Read interface .Count once rather than per iteration
            var expandersCount = expanders.Count;
            for (var i = 0; i < expandersCount; i++) {
                viewLocations = expanders[i].ExpandViewLocations(expanderContext, viewLocations);
            }

            PublicViewLocationCacheResult cacheResult = null;
            var searchedLocations = new List<string>();
            var expirationTokens = new HashSet<IChangeToken>();
            foreach (var location in viewLocations) {
                var path = string.Format(
                    CultureInfo.InvariantCulture,
                    location,
                    expanderContext.ViewName,
                    expanderContext.ControllerName,
                    expanderContext.AreaName);

                path = PublicViewEnginePath.ResolvePath(path);

                cacheResult = CreateCacheResult(expirationTokens, path, expanderContext.IsMainPage);
                if (cacheResult != null) {
                    break;
                }

                searchedLocations.Add(path);
            }

            // No views were found at the specified location. Create a not found result.
            if (cacheResult == null) {
                cacheResult = new PublicViewLocationCacheResult(searchedLocations);
            }

            var cacheEntryOptions = new MemoryCacheEntryOptions();
            cacheEntryOptions.SetSlidingExpiration(this.options.CacheExpirationDuration);
            foreach (var expirationToken in expirationTokens) {
                cacheEntryOptions.AddExpirationToken(expirationToken);
            }

            return ViewLookupCache.Set(cacheKey, cacheResult, cacheEntryOptions);
        }

        internal PublicViewLocationCacheResult CreateCacheResult(HashSet<IChangeToken> expirationTokens, string relativePath, bool isMainPage) {
            var factoryResult = pageFactory.CreateFactory(relativePath);
            var viewDescriptor = factoryResult.ViewDescriptor;
            if (viewDescriptor?.ExpirationTokens != null) {
                var viewExpirationTokens = viewDescriptor.ExpirationTokens;
                // Read interface .Count once rather than per iteration
                var viewExpirationTokensCount = viewExpirationTokens.Count;
                for (var i = 0; i < viewExpirationTokensCount; i++) {
                    expirationTokens.Add(viewExpirationTokens[i]);
                }
            }

            if (factoryResult.Success) {
                // Only need to lookup _ViewStarts for the main page.
                var viewStartPages = isMainPage ?
                    GetViewStartPages(viewDescriptor.RelativePath, expirationTokens) :
                    Array.Empty<PublicViewLocationCacheItem>();


                return new PublicViewLocationCacheResult(
                    new PublicViewLocationCacheItem(factoryResult.RazorPageFactory, relativePath),
                    viewStartPages);
            }

            return null;
        }

        private IReadOnlyList<PublicViewLocationCacheItem> GetViewStartPages(string path, HashSet<IChangeToken> expirationTokens) {

            var viewStartPages = new List<PublicViewLocationCacheItem>();

            foreach (var filePath in PublicRazorFileHierarchy.GetViewStartPaths(path)) {
                var result = pageFactory.CreateFactory(filePath);
                var viewDescriptor = result.ViewDescriptor;
                if (viewDescriptor?.ExpirationTokens != null) {
                    for (var i = 0; i < viewDescriptor.ExpirationTokens.Count; i++) {
                        expirationTokens.Add(viewDescriptor.ExpirationTokens[i]);
                    }
                }

                if (result.Success) {
                    // Populate the viewStartPages list so that _ViewStarts appear in the order the need to be
                    // executed (closest last, furthest first). This is the reverse order in which
                    // ViewHierarchyUtility.GetViewStartLocations returns _ViewStarts.
                    viewStartPages.Insert(0, new PublicViewLocationCacheItem(result.RazorPageFactory, filePath));
                }
            }

            return viewStartPages;
        }

        internal virtual ViewEngineResult CreateViewEngineResult(PublicViewLocationCacheResult result, string viewName) {
            if (!result.Success) {
                return ViewEngineResult.NotFound(viewName, result.SearchedLocations);
            }

            var page = result.ViewEntry.PageFactory();            
            var cnt = result.ViewStartEntries.Count;

            var viewStarts = new IRazorPage[result.ViewStartEntries.Count];

            for (var i = 0; i < cnt; i++) {
                var viewStartItem = result.ViewStartEntries[i];
                viewStarts[i] = viewStartItem.PageFactory();
            }

            var view = new RazorView(this, pageActivator, viewStarts, page, htmlEncoder, diagnosticListener);
            return ViewEngineResult.Found(viewName, view);
        }

        private static bool IsApplicationRelativePath(string name) {
            if (name is null) {
                throw new ArgumentNullException(nameof(name));
            }

            return name[0] == '~' || name[0] == '/';
        }

        private static bool IsRelativePath(string name) {
            if (name is null) {
                throw new ArgumentNullException(nameof(name));
            }

            // Though ./ViewName looks like a relative path, framework searches for that view using view locations.
            return name.EndsWith(ViewExtension, StringComparison.OrdinalIgnoreCase);
        }

        // Private class that handles multiple clients
        private class MultiTenantMemoryCache : IMemoryCache {
            private readonly IHttpContextAccessor contextAccessor;
            private readonly string httpContextItemsKey;
            private readonly IDictionary<string, IMemoryCache> hostCaches = new Dictionary<string, IMemoryCache> { { "default", new MemoryCache(new MemoryCacheOptions()) } };
            private readonly object hostCachesLock = new object();

            internal MultiTenantMemoryCache(IHttpContextAccessor contextAccessor, string httpContextItemsKey) {
                this.contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
                this.httpContextItemsKey = httpContextItemsKey;
            }

            public ICacheEntry CreateEntry(object key) {
                return GetCurrentTenantCache().CreateEntry(key);
            }

            public void Remove(object key) {
                GetCurrentTenantCache().Remove(key);
            }

            public bool TryGetValue(object key, out object value) {
                return GetCurrentTenantCache().TryGetValue(key, out value);
            }

            private IMemoryCache GetCurrentTenantCache() {

                if (contextAccessor.HttpContext is object && contextAccessor.HttpContext.Items.TryGetValue(this.httpContextItemsKey, out var host) && !string.IsNullOrEmpty(host?.ToString())) {

                    var hostName = host.ToString();
                    if (hostCaches.ContainsKey(hostName)) {
                        return hostCaches[hostName];
                    }

                    lock (hostCachesLock) {
                        if (hostCaches.ContainsKey(hostName)) {
                            return hostCaches[hostName];
                        }
                        return (hostCaches[hostName] = new MemoryCache(new MemoryCacheOptions()));
                    }
                }

                return hostCaches["default"];
            }

            public void Dispose() {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing) {
                if (disposing) {
                    foreach (var cache in hostCaches) {
                        cache.Value.Dispose();
                    }
                }
            }
        }
    }
}

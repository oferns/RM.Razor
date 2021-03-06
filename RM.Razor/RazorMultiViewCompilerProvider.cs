﻿namespace RM.Razor {

    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc.ApplicationParts;
    using Microsoft.AspNetCore.Mvc.Razor.Compilation;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public class RazorMultiViewCompilerProvider : IViewCompilerProvider {

        private readonly IHttpContextAccessor contextAccessor;
        private readonly RazorMultiViewEngineOptions options;
        internal readonly IDictionary<string, IViewCompiler> compilers = new Dictionary<string, IViewCompiler>();


        public RazorMultiViewCompilerProvider(ApplicationPartManager applicationPartManager,
                                                IHttpContextAccessor contextAccessor,
                                                IOptions<RazorMultiViewEngineOptions> optionsAccessor) {

            if (applicationPartManager is null) {
                throw new ArgumentNullException(nameof(applicationPartManager));
            }

            this.contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            this.options = optionsAccessor?.Value ?? throw new ArgumentNullException(nameof(optionsAccessor));

            var feature = new ViewsFeature();
            applicationPartManager.PopulateFeature(feature);

            var defaultViews = new List<CompiledViewDescriptor>();

            foreach (var descriptor in feature.ViewDescriptors.Where(f => f.Item.Type.Assembly.GetName().Name.Equals(options.DefaultViewLibrary?.AssemblyName) ||
                                                                            f.Item.Type.Assembly.GetName().Name.Equals(options.DefaultViewLibrary?.AssemblyName + ".Views", StringComparison.Ordinal))) {
                
                if (!defaultViews.Exists(v => v.RelativePath.Equals(descriptor.RelativePath, StringComparison.OrdinalIgnoreCase))) {
                    defaultViews.Add(descriptor);
                }
            }

            compilers.Add("default", new RazorMultiViewCompiler(defaultViews));

            // A cache list of libraries and their compiled views 
            var libraryViewList = new Dictionary<string, List<CompiledViewDescriptor>>();
            if (options.ViewLibraryConfig is object) {
                foreach (var option in options.ViewLibraryConfig) {

                    if (compilers.ContainsKey(option.Key)) {
                        continue;
                    }

                    // A list of descriptors for this option                
                    var viewDescriptors = new List<CompiledViewDescriptor>();

                    // Loop the requested libraries 
                    // TODO: Find a better way of getting the related View assembly
                    foreach (var library in option.Value) {
                        if (!libraryViewList.TryGetValue(library, out var liblist)) {
                            liblist = feature.ViewDescriptors.Where(d => d.Item.Type.Assembly.GetName().Name.Equals($"{library}") || d.Item.Type.Assembly.GetName().Name.Equals($"{library}.Views")).ToList();
                        }

                        foreach (var descriptor in liblist) {
                            if (viewDescriptors.Exists(v => v.RelativePath.Equals(descriptor.RelativePath, StringComparison.OrdinalIgnoreCase))) {
                                continue;
                            }
                            viewDescriptors.Add(descriptor);
                        }
                    }

                    // Add any missing views from the default library
                    foreach (var descriptor in defaultViews) {
                        if (viewDescriptors.Exists(v => v.RelativePath.Equals(descriptor.RelativePath, StringComparison.OrdinalIgnoreCase))) {
                            continue;
                        }
                        viewDescriptors.Add(descriptor);
                    }
                    compilers.Add(option.Key, new RazorMultiViewCompiler(viewDescriptors));
                }
            }
        }

        public IViewCompiler GetCompiler() {
            if (contextAccessor.HttpContext.Items.TryGetValue(this.options.HttpContextItemsKey, out var keyValue) && !string.IsNullOrEmpty(keyValue?.ToString())) {
                var keyValueString = keyValue.ToString();
                if (compilers.ContainsKey(keyValueString)) {
                    return compilers[keyValueString];
                }
            }
            return compilers["default"];
        }
    }
}
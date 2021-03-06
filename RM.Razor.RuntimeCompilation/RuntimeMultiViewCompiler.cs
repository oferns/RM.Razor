﻿namespace RM.Razor.RuntimeCompilation {

    using Microsoft.AspNetCore.Mvc.Razor.Compilation;
    using Microsoft.AspNetCore.Razor.Hosting;
    using Microsoft.AspNetCore.Razor.Language;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Emit;
    using Microsoft.CodeAnalysis.Text;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Primitives;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    public class RuntimeMultiViewCompiler : IViewCompiler {

        private readonly Dictionary<string, CompiledViewDescriptor> precompiledViews = new Dictionary<string, CompiledViewDescriptor>(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, IEnumerable<CompiledViewDescriptor>> compiledViews = new ConcurrentDictionary<string, IEnumerable<CompiledViewDescriptor>>();

        private readonly IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
        private readonly object cacheLock = new object();

        private readonly IDictionary<string, string> normalizedPathCache = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        private readonly IDictionary<string, RazorProjectEngine> projectEngines;

        private readonly ILogger logger;
        private readonly PublicCSharpCompiler csharpCompiler;

        public RuntimeMultiViewCompiler(
            IDictionary<string, RazorProjectEngine> projectEngines,
            PublicCSharpCompiler csharpCompiler,
            IList<CompiledViewDescriptor> compiledViews,
            ILogger logger) {

            this.projectEngines = projectEngines;
            this.csharpCompiler = csharpCompiler;
            this.logger = logger;

            var libs = new List<string>();
            foreach (var compiledView in compiledViews) {

                var library = compiledView.Type?.Assembly.GetName().Name ?? "default";

                if (!this.precompiledViews.ContainsKey(compiledView.RelativePath)) {
                    this.precompiledViews.Add(compiledView.RelativePath, compiledView);
                }
            }
        }

        public Task<CompiledViewDescriptor> CompileAsync(string relativePath) {
            if (relativePath == null) {
                throw new ArgumentNullException(nameof(relativePath));
            }

            logger.LogInformation($"{relativePath} - RelPath Looking in the Compiler Live Memory Cache");

            // Attempt to lookup the cache entry using the passed in path. This will succeed if the path is already
            // normalized and a cache entry exists.
            if (cache.TryGetValue(relativePath, out Task<CompiledViewDescriptor> cachedResult)) {
                logger.LogInformation($"{relativePath} - RelPath Found in the Compiler Live Memory Cache");
                return cachedResult;
            }

            logger.LogInformation($"{relativePath} - RelPath Not found in the Compiler Live Memory Cache");

            var normalizedPath = GetNormalizedPath(relativePath);


            if (!(normalizedPath.Equals(relativePath)) && cache.TryGetValue(normalizedPath, out cachedResult)) {
                logger.LogInformation($"{relativePath} - NormPath Looking in the Compiler Live Memory Cache");
                return cachedResult;
            }

            logger.LogInformation($"{normalizedPath} NormPath Not found in the Compiler Live Memory Cache");

            // Entry does not exist. Attempt to create one.
            cachedResult = OnCacheMiss(normalizedPath);
            return cachedResult;
        }

        private IFileProvider GetProviderFromRazorProjectEngine(RazorProjectEngine engine) {
            return ((PublicFileProviderRazorProjectFileSystem)engine.FileSystem).FileProvider;
        }

        private Task<CompiledViewDescriptor> OnCacheMiss(string normalizedPath) {
            ViewCompilerWorkItem item;
            TaskCompletionSource<CompiledViewDescriptor> taskSource;
            MemoryCacheEntryOptions cacheEntryOptions;


            // Safe races cannot be allowed when compiling Razor pages. To ensure only one compilation request succeeds
            // per file, we'll lock the creation of a cache entry. Creating the cache entry should be very quick. The
            // actual work for compiling files happens outside the critical section.
            lock (cacheLock) {

                // Double-checked locking to handle a possible race.
                if (cache.TryGetValue(normalizedPath, out Task<CompiledViewDescriptor> result)) {
                    return result;
                }

                if (precompiledViews.TryGetValue(normalizedPath, out var precompiledView)) {
                    // item = CreatePrecompiledWorkItem(normalizedPath, precompiledView);
                    // precompiledViews.Remove(normalizedPath); // Once it is in a work item remove it.
                    item = CreateRuntimeCompilationWorkItem(normalizedPath, precompiledView);
                } else {
                    item = CreateRuntimeCompilationWorkItem(normalizedPath, null);
                }

                var tokens = new List<IChangeToken>();
                taskSource = new TaskCompletionSource<CompiledViewDescriptor>(creationOptions: TaskCreationOptions.RunContinuationsAsynchronously);
                tokens.AddRange(item.ExpirationTokens);

                if (!item.SupportsCompilation) {
                    taskSource.SetResult(item.Descriptor);
                }

                cacheEntryOptions = new MemoryCacheEntryOptions();

                foreach (var token in tokens) {
                    cacheEntryOptions.ExpirationTokens.Add(token);
                }
                cache.Set(normalizedPath, taskSource.Task, cacheEntryOptions);
            }


            // Now the lock has been released so we can do more expensive processing.
            if (item.SupportsCompilation) {
                Debug.Assert(taskSource is object);

                if (item.Descriptor?.Item is object) {

                    var itemAssemblyName = item.Descriptor.Item.Type.Assembly.GetName().Name;
                    // If we dont have an engine for the library
                    if (!projectEngines.TryGetValue(itemAssemblyName, out var engine)) {
                        engine = projectEngines.First().Value;
                    }

                    // If nothing changed serve that mofo
                    if (PublicChecksumValidator.IsItemValid(engine.FileSystem, item.Descriptor.Item)) {
                        Debug.Assert(item.Descriptor != null);

                        taskSource.SetResult(item.Descriptor);
                        return taskSource.Task;
                    }

                    // Otherwise try to recompile
                    try {
                        var descriptor = CompileAndEmit(normalizedPath, engine, itemAssemblyName);
                        descriptor.ExpirationTokens = cacheEntryOptions.ExpirationTokens;
                        taskSource.SetResult(descriptor);
                        return taskSource.Task;
                    } catch (Exception ex) {
                        logger.LogError(ex, "Razor blowup");
                        taskSource.SetException(ex);
                        return taskSource.Task;
                    }
                }

                // _logger.ViewCompilerInvalidingCompiledFile(item.NormalizedPath);

                var exceptions = new List<Exception>();


                foreach (var engine in projectEngines) {
                    var file = engine.Value.FileSystem.GetItem(normalizedPath, null);
                    if (file.Exists) {
                        if (item.OriginalDescriptor is object) {
                            if (PublicChecksumValidator.IsItemValid(engine.Value.FileSystem, item.OriginalDescriptor.Item)) {
                                taskSource.SetResult(item.OriginalDescriptor);
                                return taskSource.Task;
                            }
                        }

                        try {
                            var descriptor = CompileAndEmit(normalizedPath, engine.Value, engine.Key);
                            descriptor.ExpirationTokens = cacheEntryOptions.ExpirationTokens;
                            taskSource.SetResult(descriptor);
                            return taskSource.Task;
                        } catch (Exception ex) {
                            exceptions.Add(ex);
                            logger.LogError(ex, "Razor blowup");
                        }
                    }
                }
                if (exceptions.Count > 0) {
                    taskSource.SetException(exceptions.AsEnumerable());
                }
            }

            return taskSource.Task;
        }

        private ViewCompilerWorkItem CreatePrecompiledWorkItem(string normalizedPath, CompiledViewDescriptor precompiledView) {
            // We have a precompiled view - but we're not sure that we can use it yet.
            //
            // We need to determine first if we have enough information to 'recompile' this view. If that's the case
            // we'll create change tokens for all of the files.
            //
            // Then we'll attempt to validate if any of those files have different content than the original sources
            // based on checksums.
            if (precompiledView.Item == null || !PublicChecksumValidator.IsRecompilationSupported(precompiledView.Item)) {
                return new ViewCompilerWorkItem {
                    // If we don't have a checksum for the primary source file we can't recompile.
                    SupportsCompilation = false,
                    ExpirationTokens = Array.Empty<IChangeToken>(), // Never expire because we can't recompile.
                    Descriptor = precompiledView, // This will be used as-is.
                };
            }


            var item = new ViewCompilerWorkItem {
                SupportsCompilation = true,
                Descriptor = precompiledView, // This might be used, if the checksums match.
                // Used to validate and recompile
                NormalizedPath = normalizedPath,
                ExpirationTokens = GetExpirationTokens(precompiledView)
            };

            // We also need to create a new descriptor, because the original one doesn't have expiration tokens on
            // it. These will be used by the view location cache, which is like an L1 cache for views (this class is
            // the L2 cache).
            item.Descriptor = new CompiledViewDescriptor() {
                ExpirationTokens = item.ExpirationTokens,
                Item = precompiledView.Item,
                RelativePath = precompiledView.RelativePath,
            };

            return item;
        }

        private ViewCompilerWorkItem CreateRuntimeCompilationWorkItem(string normalizedPath, CompiledViewDescriptor originalDescriptor) {

            if (originalDescriptor is object) {
                if (originalDescriptor.Item == null || !PublicChecksumValidator.IsRecompilationSupported(originalDescriptor.Item)) {
                    return new ViewCompilerWorkItem {
                        // If we don't have a checksum for the primary source file we can't recompile.
                        SupportsCompilation = false,
                        ExpirationTokens = Array.Empty<IChangeToken>(), // Never expire because we can't recompile.
                        Descriptor = originalDescriptor, // This will be used as-is.
                    };
                }

                var assembly = originalDescriptor.Item.Type?.Assembly.GetName().Name;
                if (!string.IsNullOrEmpty(assembly) && this.projectEngines.TryGetValue(assembly, out var origEngine)) {
                    var projectItem = origEngine.FileSystem.GetItem(normalizedPath, fileKind: null);
                    if (!projectItem.Exists) {
                        originalDescriptor = null; //force recompilation
                    }
                }
            }

            var allTokens = new List<IChangeToken>();
            var exists = false;

            foreach (var engine in this.projectEngines) {
                var fileProvider = GetProviderFromRazorProjectEngine(engine.Value);
                IList<IChangeToken> expirationTokens = new List<IChangeToken> {
                    fileProvider.Watch(normalizedPath),
                };

                var projectItem = engine.Value.FileSystem.GetItem(normalizedPath, fileKind: null);
                if (projectItem.Exists) {
                    exists = true;
                    var importFeature = engine.Value.ProjectFeatures.OfType<IImportProjectFeature>().ToArray();
                    foreach (var feature in importFeature) {
                        foreach (var file in feature.GetImports(projectItem)) {
                            if (file.FilePath != null) {
                                expirationTokens.Add(GetProviderFromRazorProjectEngine(engine.Value).Watch(file.FilePath));
                            }
                        }
                    }
                }

                allTokens.AddRange(expirationTokens);
                // _logger.ViewCompilerFoundFileToCompile(normalizedPath);
            }

            if (originalDescriptor is object) {
                originalDescriptor.ExpirationTokens = allTokens;
            }

            return new ViewCompilerWorkItem() {
                SupportsCompilation = exists,
                NormalizedPath = normalizedPath,
                ExpirationTokens = allTokens,
                Descriptor = exists ? default : new CompiledViewDescriptor() {
                    RelativePath = normalizedPath,
                    ExpirationTokens = allTokens

                },
                OriginalDescriptor = originalDescriptor
            };
        }

        private IList<IChangeToken> GetExpirationTokens(CompiledViewDescriptor precompiledView) {
            var checksums = precompiledView.Item.GetChecksumMetadata();
            var expirationTokens = new List<IChangeToken>(checksums.Count);

            foreach (var engine in projectEngines) {

                var provider = GetProviderFromRazorProjectEngine(engine.Value);
                for (var i = 0; i < checksums.Count; i++) {
                    // We rely on Razor to provide the right set of checksums. Trust the compiler, it has to do a good job,
                    // so it probably will.

                    expirationTokens.Add(provider.Watch(checksums[i].Identifier));
                }
            }
            return expirationTokens;
        }

        protected virtual CompiledViewDescriptor CompileAndEmit(string relativePath, RazorProjectEngine engine, string assemblyName) {
            var projectItem = engine.FileSystem.GetItem(relativePath, fileKind: null);
            var codeDocument = engine.Process(projectItem);
            var cSharpDocument = codeDocument.GetCSharpDocument();

            if (cSharpDocument.Diagnostics.Count > 0) {
                throw PublicCompilationFailedExceptionFactory.Create(
                    codeDocument,
                    cSharpDocument.Diagnostics);
            }

            var assembly = CompileAndEmit(codeDocument, cSharpDocument.GeneratedCode, engine, assemblyName);

            // Anything we compile from source will use Razor 2.1 and so should have the new metadata.
            var loader = new RazorCompiledItemLoader();
            var item = loader.LoadItems(assembly).SingleOrDefault();
            return new CompiledViewDescriptor(item);
        }

        internal Assembly CompileAndEmit(RazorCodeDocument codeDocument, string generatedCode, RazorProjectEngine engine, string originalAssemblyName) {

            var startTimestamp = logger.IsEnabled(LogLevel.Debug) ? Stopwatch.GetTimestamp() : 0;

            var assemblyName = originalAssemblyName + "." + Path.GetRandomFileName().Substring(1, 3);
            var compilation = CreateCompilation(generatedCode, assemblyName);

            var emitOptions = csharpCompiler.EmitOptions;
            var emitPdbFile = csharpCompiler.EmitPdb && emitOptions.DebugInformationFormat != DebugInformationFormat.Embedded;

            using (var assemblyStream = new MemoryStream())
            using (var pdbStream = emitPdbFile ? new MemoryStream() : null) {
                var result = compilation.Emit(
                    assemblyStream,
                    pdbStream,
                    options: emitOptions);

                if (!result.Success) {
                    throw PublicCompilationFailedExceptionFactory.Create(
                        codeDocument,
                        generatedCode,
                        assemblyName,
                        result.Diagnostics);
                }

                assemblyStream.Seek(0, SeekOrigin.Begin);
                pdbStream?.Seek(0, SeekOrigin.Begin);

                var assembly = Assembly.Load(assemblyStream.ToArray(), pdbStream?.ToArray());
                //_logger.GeneratedCodeToAssemblyCompilationEnd(codeDocument.Source.FilePath, startTimestamp);

                return assembly;
            }
        }

        private CSharpCompilation CreateCompilation(string compilationContent, string assemblyName) {
            var sourceText = SourceText.From(compilationContent, Encoding.UTF8);
            var syntaxTree = csharpCompiler.CreateSyntaxTree(sourceText).WithFilePath(assemblyName);
            return csharpCompiler
                .CreateCompilation(assemblyName)
                .AddSyntaxTrees(syntaxTree);
        }

        private string GetNormalizedPath(string relativePath) {
            Debug.Assert(relativePath != null);
            logger.LogInformation($"Normalizing {relativePath}");
            if (relativePath.Length == 0) {
                return relativePath;
            }

            if (!normalizedPathCache.TryGetValue(relativePath, out var normalizedPath)) {
                normalizedPath = NormalizePath(relativePath);
                normalizedPathCache[relativePath] = normalizedPath;
            }

            logger.LogInformation($"Normalization result: Was {relativePath} Now {normalizedPath}");

            return normalizedPath;
        }

        public static string NormalizePath(string path) {
            var addLeadingSlash = path[0] != '\\' && path[0] != '/';
            var transformSlashes = path.IndexOf('\\') != -1;

            if (!addLeadingSlash && !transformSlashes) {
                return path;
            }

            var length = path.Length;
            if (addLeadingSlash) {
                length++;
            }

            return string.Create(length, (path, addLeadingSlash), (span, tuple) => {
                var (pathValue, addLeadingSlashValue) = tuple;
                var spanIndex = 0;

                if (addLeadingSlashValue) {
                    span[spanIndex++] = '/';
                }

                foreach (var ch in pathValue) {
                    span[spanIndex++] = ch == '\\' ? '/' : ch;
                }
            });
        }

        private class ViewCompilerWorkItem {
            public bool SupportsCompilation { get; set; }

            public string NormalizedPath { get; set; }

            public IList<IChangeToken> ExpirationTokens { get; set; }

            public CompiledViewDescriptor Descriptor { get; set; }

            public CompiledViewDescriptor OriginalDescriptor { get; set; }

        }
    }
}
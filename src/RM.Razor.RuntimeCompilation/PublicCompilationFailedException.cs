namespace RM.Razor.RuntimeCompilation {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.AspNetCore.Diagnostics;

    public class PublicCompilationFailedException : Exception, ICompilationException {
        public PublicCompilationFailedException(
                IEnumerable<CompilationFailure> compilationFailures)
            : base(FormatMessage(compilationFailures)) {
            if (compilationFailures == null) {
                throw new ArgumentNullException(nameof(compilationFailures));
            }

            CompilationFailures = compilationFailures;
        }

        public IEnumerable<CompilationFailure> CompilationFailures { get; }

        private static string FormatMessage(IEnumerable<CompilationFailure> compilationFailures) {
            return 
                string.Join(
                    Environment.NewLine,
                    compilationFailures.SelectMany(f => f.Messages).Select(message => message.FormattedMessage));
        }
    }
}
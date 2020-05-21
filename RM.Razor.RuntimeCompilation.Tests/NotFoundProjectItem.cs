namespace RM.Razor.RuntimeCompilation.Tests {

    using Microsoft.AspNetCore.Razor.Language;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class NotFoundProjectItem : RazorProjectItem {
        public NotFoundProjectItem(string basePath, string path) {
            BasePath = basePath;
            FilePath = path;
        }

        public override string BasePath { get; }

        public override string FilePath { get; }

        public override bool Exists => false;

        public override string PhysicalPath => throw new NotSupportedException();

        public override Stream Read() => throw new NotSupportedException();
    }
}
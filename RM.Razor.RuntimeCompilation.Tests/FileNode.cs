namespace RM.Razor.RuntimeCompilation.Tests {

    using Microsoft.AspNetCore.Razor.Language;
    using System.Diagnostics;

    [DebuggerDisplay("{Path}")]
    public class FileNode {
        public FileNode(string path, RazorProjectItem projectItem) {
            Path = path;
            ProjectItem = projectItem;
        }

        public DirectoryNode Directory { get; set; }

        public string Path { get; }

        public RazorProjectItem ProjectItem { get; set; }
    }
}

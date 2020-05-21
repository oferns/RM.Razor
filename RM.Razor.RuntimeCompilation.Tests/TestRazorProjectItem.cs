﻿
namespace RM.Razor.RuntimeCompilation.Tests {
    using Microsoft.AspNetCore.Razor.Language;
    using System.IO;
    using System.Text;

    public class TestRazorProjectItem : RazorProjectItem {
        public TestRazorProjectItem(
            string filePath,
            string content = "Default content",
            string physicalPath = null,
            string relativePhysicalPath = null,
            string basePath = "/") {
            FilePath = filePath;
            PhysicalPath = physicalPath;
            RelativePhysicalPath = relativePhysicalPath;
            BasePath = basePath;
            Content = content;
        }

        public override string BasePath { get; }

        public override string FilePath { get; }

        public override string PhysicalPath { get; }

        public override string RelativePhysicalPath { get; }

        public override bool Exists => true;

        public string Content { get; set; }

        public override Stream Read() {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(Content));

            return stream;
        }
    }
}

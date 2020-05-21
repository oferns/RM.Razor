/// <summary>
/// src/Mvc/shared/Mvc.Views.TestCommon/TestDirectoryFileInfo.cs
/// </summary>
namespace RM.Razor.RuntimeCompilation.Tests {

    using Microsoft.Extensions.FileProviders;
    using System;
    using System.IO;

    public class TestDirectoryFileInfo : IFileInfo {
        public bool IsDirectory => true;

        public long Length { get; set; }

        public string Name { get; set; }

        public string PhysicalPath { get; set; }

        public bool Exists => true;

        public DateTimeOffset LastModified => throw new NotImplementedException();

        public Stream CreateReadStream() {
            throw new NotSupportedException();
        }
    }
}
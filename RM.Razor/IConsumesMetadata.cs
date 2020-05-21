namespace RM.Razor {

    using System.Collections.Generic;

    public interface IConsumesMetadata {
        IReadOnlyList<string> ContentTypes { get; }
    }
}
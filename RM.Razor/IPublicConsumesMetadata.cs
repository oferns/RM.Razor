namespace RM.Razor {

    using System.Collections.Generic;

    interface IPublicConsumesMetadata {
        IReadOnlyList<string> ContentTypes { get; }
    }
}
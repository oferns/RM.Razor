namespace RM.Razor {

    using System;
    using System.Collections.Generic;

    public class PublicConsumesMetadata : IPublicConsumesMetadata {
        
        public PublicConsumesMetadata(string[] contentTypes) {
            if (contentTypes == null) {
                throw new ArgumentNullException(nameof(contentTypes));
            }

            ContentTypes = contentTypes;
        }

        public IReadOnlyList<string> ContentTypes { get; }
    }
}

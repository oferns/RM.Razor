
namespace RM.Razor {
    using Microsoft.AspNetCore.Mvc.Razor;
    using System;

    /// <summary>
    /// An item in <see cref="PublicViewLocationCacheResult"/>.
    /// </summary>
    public readonly struct PublicViewLocationCacheItem {
        /// <summary>
        /// Initializes a new instance of <see cref="PublicViewLocationCacheItem"/>.
        /// </summary>
        /// <param name="razorPageFactory">The <see cref="IRazorPage"/> factory.</param>
        /// <param name="location">The application relative path of the <see cref="IRazorPage"/>.</param>
        public PublicViewLocationCacheItem(Func<IRazorPage> razorPageFactory, string location) {
            PageFactory = razorPageFactory;
            Location = location;
        }

        /// <summary>
        /// Gets the application relative path of the <see cref="IRazorPage"/>
        /// </summary>
        public string Location { get; }

        /// <summary>
        /// Gets the <see cref="IRazorPage"/> factory.
        /// </summary>
        public Func<IRazorPage> PageFactory { get; }
    }
}

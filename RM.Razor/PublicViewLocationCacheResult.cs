using System;
using System.Collections.Generic;
using System.Text;

namespace RM.Razor {
    /// <summary>
    /// Result of view location cache lookup.
    /// </summary>
    public class PublicViewLocationCacheResult {
        /// <summary>
        /// Initializes a new instance of <see cref="PublicViewLocationCacheResult"/>
        /// for a view that was successfully found at the specified location.
        /// </summary>
        /// <param name="view">The <see cref="PublicViewLocationCacheItem"/> for the found view.</param>
        /// <param name="viewStarts"><see cref="PublicViewLocationCacheItem"/>s for applicable _ViewStarts.</param>
        public PublicViewLocationCacheResult(
            PublicViewLocationCacheItem view,
            IReadOnlyList<PublicViewLocationCacheItem> viewStarts) {
            ViewEntry = view;
            ViewStartEntries = viewStarts ?? throw new ArgumentNullException(nameof(viewStarts));
            Success = true;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="PublicViewLocationCacheResult"/> for a
        /// failed view lookup.
        /// </summary>
        /// <param name="searchedLocations">Locations that were searched.</param>
        public PublicViewLocationCacheResult(IEnumerable<string> searchedLocations) {
            SearchedLocations = searchedLocations ?? throw new ArgumentNullException(nameof(searchedLocations));
        }

        /// <summary>
        /// <see cref="PublicViewLocationCacheItem"/> for the located view.
        /// </summary>
        /// <remarks><c>null</c> if <see cref="Success"/> is <c>false</c>.</remarks>
        public PublicViewLocationCacheItem ViewEntry { get; }

        /// <summary>
        /// <see cref="PublicViewLocationCacheItem"/>s for applicable _ViewStarts.
        /// </summary>
        /// <remarks><c>null</c> if <see cref="Success"/> is <c>false</c>.</remarks>
        public IReadOnlyList<PublicViewLocationCacheItem> ViewStartEntries { get; }

        /// <summary>
        /// The sequence of locations that were searched.
        /// </summary>
        /// <remarks>
        /// When <see cref="Success"/> is <c>true</c> this includes all paths that were search prior to finding
        /// a view at <see cref="ViewEntry"/>. When <see cref="Success"/> is <c>false</c>, this includes
        /// all search paths.
        /// </remarks>
        public IEnumerable<string> SearchedLocations { get; }

        /// <summary>
        /// Gets a value that indicates whether the view was successfully found.
        /// </summary>
        public bool Success { get; }
    }
}


namespace RM.Razor {

    using Microsoft.AspNetCore.Mvc.Filters;
    using System;
    using System.Collections.Generic;

    public class PublicFilterDescriptorOrderComparer : IComparer<FilterDescriptor> {
        public static PublicFilterDescriptorOrderComparer Comparer { get; } = new PublicFilterDescriptorOrderComparer();

        public int Compare(FilterDescriptor x, FilterDescriptor y) {
            if (x == null) {
                throw new ArgumentNullException(nameof(x));
            }

            if (y == null) {
                throw new ArgumentNullException(nameof(y));
            }

            if (x.Order == y.Order) {
                return x.Scope.CompareTo(y.Scope);
            } else {
                return x.Order.CompareTo(y.Order);
            }
        }
    }
}

using Ryujinx.UI.App.Common;
using System;

namespace Ryujinx.Ava.UI.ViewModels
{
    /// <summary>
    /// Implements a custom comparer which is used for sorting titles by favorite on a UI.
    /// Returns a sorted list of favorites in alphabetical order, followed by all non-favorites sorted alphabetical.
    /// </summary>
    public struct AppListFavoriteComparable : IComparable
    {
        /// <summary>
        /// The application data being compared.
        /// </summary>
        private readonly ApplicationData app;

        /// <summary>
        /// Constructs a new <see cref="AppListFavoriteComparable"/> with the specified application data.
        /// </summary>
        /// <param name="app">The app data being compared.</param>
        public AppListFavoriteComparable(ApplicationData app)
        {
            this.app = app;
        }

        /// <inheritdoc/>
        public int CompareTo(object o)
        {
            if (o is AppListFavoriteComparable other)
            {
                if (app.Favorite == other.app.Favorite)
                {
                    return app.Name.CompareTo(other.app.Name);
                }

                return app.Favorite ? -1 : 1;
            }

            throw new InvalidCastException($"Cannot cast {o.GetType()} to {nameof(ApplicationData)}");
        }
    }
}

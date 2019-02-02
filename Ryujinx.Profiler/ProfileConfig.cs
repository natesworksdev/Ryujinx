using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.Profiler
{
    public struct ProfileConfig
    {
        public string Category;
        public string SessionGroup;
        public string SessionItem;

        public int Level;

        // Private cached variables
        private string _cachedTag;
        private string _cachedSession;
        private string _cachedSearch;

        // Public helpers to get config in more user friendly format,
        // Cached because they never change and are called often
        public string Search
        {
            get
            {
                if (_cachedSearch == null)
                {
                    _cachedSearch = $"{Category}.{SessionGroup}.{SessionItem}";
                }

                return _cachedSearch;
            }
        }

        public string Tag
        {
            get
            {
                if (_cachedTag == null)
                    _cachedTag = $"{Category}{(Session == "" ? "" : $" ({Session})")}";
                return _cachedTag;
            }
        }

        public string Session
        {
            get
            {
                if (_cachedSession == null)
                {
                    if (SessionGroup != null && SessionItem != null)
                    {
                        _cachedSession = $"{SessionGroup}: {SessionItem}";
                    }
                    else if (SessionGroup != null)
                    {
                        _cachedSession = $"{SessionGroup}";
                    }
                    else if (SessionItem != null)
                    {
                        _cachedSession = $"---: {SessionItem}";
                    }
                    else
                    {
                        _cachedSession = "";
                    }
                }

                return _cachedSession;
            }
        }
    }

    /// <summary>
    /// Predefined configs to make profiling easier,
    /// nested so you can reference as Profiles.Category.Group.Item where item and group may be optional
    /// </summary>
    public static class Profiles
    {
        public static class CPU
        {
            public static ProfileConfig TranslateTier0 = new ProfileConfig()
            {
                Category = "CPU",
                SessionGroup = "TranslateTier0"
            };

            public static ProfileConfig TranslateTier1 = new ProfileConfig()
            {
                Category = "CPU",
                SessionGroup = "TranslateTier1"
            };
        }

        public static ProfileConfig ServiceCall = new ProfileConfig()
        {
            Category = "ServiceCall",
        };
    }
}

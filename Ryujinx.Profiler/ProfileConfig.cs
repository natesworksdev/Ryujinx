using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.Profiler
{
    public struct ProfileConfig
    {
        public string Category;
        public string SessionGroup, SessionItem;

        private string CachedTag, CachedSession;

        public string Search => $"{Category}.{SessionGroup}.{SessionItem}";

        public string Tag
        {
            get
            {
                if (CachedTag == null)
                    CachedTag = $"{Category}{(Session == "" ? "" : $" ({Session})")}";
                return CachedTag;
            }
        }

        public string Session
        {
            get
            {
                if (CachedSession == null)
                {
                    if (SessionGroup != null && SessionItem != null)
                    {
                        CachedSession = $"{SessionGroup}: {SessionItem}";
                    }
                    else if (SessionGroup != null)
                    {
                        CachedSession = $"{SessionGroup}";
                    }
                    else if (SessionItem != null)
                    {
                        CachedSession = $"---: {SessionItem}";
                    }
                    else
                    {
                        CachedSession = "";
                    }
                }

                return CachedSession;
            }
        }
    }

    public static class Profiles
    {
        public static class CPU
        {
            public static ProfileConfig Test = new ProfileConfig()
            {
                Category = "CPU",
                SessionGroup = "Test"
            };
        }

        public static ProfileConfig ServiceCall = new ProfileConfig()
        {
            Category = "ServiceCall",
        };
    }
}

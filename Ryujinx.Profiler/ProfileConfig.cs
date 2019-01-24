using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.Profiler
{
    public struct ProfileConfig
    {
        public string Category;
        public string SessionGroup, SessionItem;

        private string cachedTag, cachedSession;

        public string Tag
        {
            get
            {
                if (cachedTag == null)
                    cachedTag = $"{Category}{(Session == "" ? "" : $" ({Session})")}";
                return cachedTag;
            }
        }

        public string Session
        {
            get
            {
                if (cachedSession == null)
                {
                    if (SessionGroup != null && SessionItem != null)
                    {
                        cachedSession = $"{SessionGroup}: {SessionItem}";
                    }
                    else if (SessionGroup != null)
                    {
                        cachedSession = $"{SessionGroup}";
                    }
                    else if (SessionItem != null)
                    {
                        cachedSession = $"---: {SessionItem}";
                    }
                    else
                    {
                        cachedSession = "";
                    }
                }

                return cachedSession;
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

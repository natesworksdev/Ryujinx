using System;

namespace Ryujinx.HLE.HOS.Services
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ServiceAttribute : Attribute
    {
        public readonly string Name;
        public readonly bool   UsePermission;
        public readonly int    Permission;

        public ServiceAttribute(string name, bool usePermission = false, int permission = 0)
        {
            Name          = name;
            UsePermission = usePermission;
            Permission    = permission;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.HLE.HOS.Applets
{
    static class AppletManager
    {
        private static Dictionary<AppletId, Type> _appletMapping;

        static AppletManager()
        {
            _appletMapping = new Dictionary<AppletId, Type> {
                { AppletId.PlayerSelect, typeof(PlayerSelect) }
            };
        }

        public static IApplet Create(AppletId applet, Horizon system)
        {
            if(_appletMapping.TryGetValue(applet, out Type appletClass))
            {
                return (IApplet)Activator.CreateInstance(appletClass, system);
            }

            return new StubApplet();
        }
    }
}

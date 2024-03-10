using Ryujinx.Horizon.Applets.Browser;
using Ryujinx.Horizon.Applets.Controller;
using Ryujinx.Horizon.Applets.PlayerSelect;
using Ryujinx.Horizon.Applets.SoftwareKeyboard;
using Ryujinx.Horizon.Sdk.Am;
using System;
using System.Collections.Generic;
using ErrorApplet = Ryujinx.Horizon.Applets.Error.ErrorApplet;

namespace Ryujinx.Horizon.Applets
{
    static class AppletManager
    {
        private static readonly Dictionary<AppletId, Type> _appletMapping;

        static AppletManager()
        {
            _appletMapping = new Dictionary<AppletId, Type>
            {
                { AppletId.Error,            typeof(ErrorApplet)            },
                { AppletId.PlayerSelect,     typeof(PlayerSelectApplet)     },
                { AppletId.Controller,       typeof(ControllerApplet)       },
                { AppletId.Swkbd,            typeof(SoftwareKeyboardApplet) },
                { AppletId.Web,              typeof(BrowserApplet)          },
                { AppletId.Shop,             typeof(BrowserApplet)          },
                { AppletId.OfflineWeb,       typeof(BrowserApplet)          },
            };
        }

        public static IApplet Create(AppletId applet)
        {
            if (_appletMapping.TryGetValue(applet, out Type appletClass))
            {
                return (IApplet)Activator.CreateInstance(appletClass);
            }

            throw new NotImplementedException($"{applet} applet is not implemented.");
        }
    }
}

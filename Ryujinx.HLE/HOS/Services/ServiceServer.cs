using Ryujinx.HLE.HOS.Services.Am;
using Ryujinx.HLE.HOS.Services.Hid;
using Ryujinx.HLE.HOS.Services.Sdb.Pl;
using Ryujinx.HLE.HOS.Services.Time;
using Ryujinx.HLE.HOS.Services.Vi;
using System;
using System.Linq;
using System.Reflection;

namespace Ryujinx.HLE.HOS.Services
{
    class ServiceServer
    {
        private readonly Switch _device;

        public AmServer AmServer { get; private set; }
        public HidServerBase HidServer { get; private set; }
        public SharedFontManager SharedFontManager { get; private set; }
        public TimeManager TimeManager { get; private set; }
        public ViServer ViServer { get; private set; }

        public ServiceServer(Switch device)
        {
            _device = device;
        }

        public void DiscoverAll()
        {
            var services = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.GetCustomAttributes(typeof(ServiceAttribute), true).Length != 0);

            ServerBase commonServer = new ServerBase(_device, "CommonServer");
            AmServer = new AmServer(_device);
            HidServer = new HidServerBase(_device);
            SharedFontManager = new SharedFontManager(_device);
            TimeManager = new TimeManager(_device);
            ViServer = new ViServer(_device);

            ServerBase PickServer(string name)
            {
                if (name.StartsWith("appletAE") || name.StartsWith("appletOE"))
                {
                    return AmServer;
                }
                else if (name.StartsWith("hid") || name.StartsWith("irs"))
                {
                    return HidServer;
                }
                else if (name.StartsWith("pl"))
                {
                    return SharedFontManager;
                }
                else if (name.StartsWith("time"))
                {
                    return TimeManager;
                }
                else if (name.StartsWith("vi"))
                {
                    return ViServer;
                }
                else
                {
                    return commonServer;
                }
            }

            foreach (Type type in services)
            {
                foreach (var serviceAttribute in type.GetCustomAttributes<ServiceAttribute>())
                {
                    PickServer(serviceAttribute.Name).Register(serviceAttribute.Name, type, serviceAttribute.Parameter);
                }
            }

            // Signal all servers that we are done enqueuing services for registration, they can now continue.
            commonServer.SignalInitDone();
            AmServer.SignalInitDone();
            HidServer.SignalInitDone();
            SharedFontManager.SignalInitDone();
            TimeManager.SignalInitDone();
            ViServer.SignalInitDone();
        }
    }
}
using Ryujinx.HLE.HOS.Services.Hid;
using Ryujinx.HLE.HOS.Services.Sdb.Pl;
using Ryujinx.HLE.HOS.Services.Time;
using System;
using System.Linq;
using System.Reflection;

namespace Ryujinx.HLE.HOS.Services
{
    class ServiceServer
    {
        private readonly Switch _device;

        public HidServerBase HidServer { get; private set; }
        public SharedFontManager SharedFontManager { get; private set; }
        public TimeManager TimeManager { get; private set; }

        public ServiceServer(Switch device)
        {
            _device = device;
        }

        public void DiscoverAll()
        {
            var services = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.GetCustomAttributes(typeof(ServiceAttribute), true).Length != 0);

            ServerBase commonServer = new ServerBase(_device.System.KernelContext, "CommonServer");
            HidServer = new HidServerBase(_device.System.KernelContext);
            SharedFontManager = new SharedFontManager(_device);
            TimeManager = new TimeManager(_device);

            ServerBase PickServer(string name)
            {
                if (name.StartsWith("hid") || name.StartsWith("irs"))
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
            HidServer.SignalInitDone();
            SharedFontManager.SignalInitDone();
            TimeManager.SignalInitDone();
        }
    }
}
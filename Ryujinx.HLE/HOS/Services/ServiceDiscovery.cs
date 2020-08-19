using System;
using System.Linq;
using System.Reflection;

namespace Ryujinx.HLE.HOS.Services
{
    static class ServiceDiscovery
    {
        public static void DiscoverAll(Switch device)
        {
            var services = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.GetCustomAttributes(typeof(ServiceAttribute), true).Length != 0);

            ServerBase commonServer = new ServerBase(device.System.KernelContext, "CommonServer");

            foreach (Type type in services)
            {
                foreach (var serviceAttribute in type.GetCustomAttributes<ServiceAttribute>())
                {
                    commonServer.Register(serviceAttribute.Name, type, serviceAttribute.Parameter);
                }
            }

            // Signal all servers that we are done enqueuing services for registration, they can now continue.
            commonServer.SignalInitDone();
        }
    }
}
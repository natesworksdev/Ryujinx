using Ryujinx.HLE.HOS.Services.Acc;
using Ryujinx.HLE.HOS.Services.Am;
using Ryujinx.HLE.HOS.Services.Apm;
using Ryujinx.HLE.HOS.Services.Aud;
using Ryujinx.HLE.HOS.Services.Bsd;
using Ryujinx.HLE.HOS.Services.Caps;
using Ryujinx.HLE.HOS.Services.FspSrv;
using Ryujinx.HLE.HOS.Services.Hid;
using Ryujinx.HLE.HOS.Services.Irs;
using Ryujinx.HLE.HOS.Services.Ldr;
using Ryujinx.HLE.HOS.Services.Lm;
using Ryujinx.HLE.HOS.Services.Mm;
using Ryujinx.HLE.HOS.Services.Nfp;
using Ryujinx.HLE.HOS.Services.Ns;
using Ryujinx.HLE.HOS.Services.Nv;
using Ryujinx.HLE.HOS.Services.Pctl;
using Ryujinx.HLE.HOS.Services.Pl;
using Ryujinx.HLE.HOS.Services.Prepo;
using Ryujinx.HLE.HOS.Services.Psm;
using Ryujinx.HLE.HOS.Services.Set;
using Ryujinx.HLE.HOS.Services.Sfdnsres;
using Ryujinx.HLE.HOS.Services.Sm;
using Ryujinx.HLE.HOS.Services.Spl;
using Ryujinx.HLE.HOS.Services.Ssl;
using Ryujinx.HLE.HOS.Services.Vi;
using System;

namespace Ryujinx.HLE.HOS.Services
{
    static class ServiceFactory
    {
        public static IpcService MakeService(Horizon system, string name)
        {
            switch (name)
            {
                case "acc:u0":
                    return new AccountService();

                case "acc:u1":
                    return new AccountService();

                case "aoc:u":
                    return new AddOnContentManager();

                case "apm":
                    return new Manager();

                case "apm:p":
                    return new Manager();

                case "appletAE":
                    return new AllSystemAppletProxiesService();

                case "appletOE":
                    return new ApplicationProxyService();

                case "audout:u":
                    return new AudioOutManager();

                case "audren:u":
                    return new AudioRendererManager();

                case "bcat:a":
                    return new Bcat.ServiceCreator();

                case "bcat:m":
                    return new Bcat.ServiceCreator();

                case "bcat:u":
                    return new Bcat.ServiceCreator();

                case "bcat:s":
                    return new Bcat.ServiceCreator();

                case "bsd:s":
                    return new Client(true);

                case "bsd:u":
                    return new Client(false);

                case "caps:a":
                    return new AlbumAccessorService();

                case "caps:ss":
                    return new ScreenshotService();

                case "csrng":
                    return new RandomInterface();

                case "friend:a":
                    return new Friend.ServiceCreator();

                case "friend:u":
                    return new Friend.ServiceCreator();

                case "fsp-srv":
                    return new FileSystemProxy();

                case "hid":
                    return new HidServer(system);

                case "irs":
                    return new IrSensorServer();

                case "ldr:ro":
                    return new RoInterface();

                case "lm":
                    return new LogService();

                case "mm:u":
                    return new Request();

                case "nfp:user":
                    return new UserManager();

                case "nifm:u":
                    return new Nifm.StaticService();

                case "ns:ec":
                    return new ServiceGetterInterface();

                case "ns:su":
                    return new SystemUpdateInterface();

                case "ns:vm":
                    return new VulnerabilityManagerInterface();

                case "nvdrv":
                    return new NvDrvServices(system);

                case "nvdrv:a":
                    return new NvDrvServices(system);

                case "pctl:s":
                    return new ParentalControlServiceFactory();

                case "pctl:r":
                    return new ParentalControlServiceFactory();

                case "pctl:a":
                    return new ParentalControlServiceFactory();

                case "pctl":
                    return new ParentalControlServiceFactory();

                case "pl:u":
                    return new SharedFontManager();

                case "prepo:a":
                    return new PrepoService();

                case "prepo:u":
                    return new PrepoService();

                case "psm":
                    return new PsmServer();

                case "set":
                    return new SettingsServer();

                case "set:sys":
                    return new SystemSettingsServer();

                case "sfdnsres":
                    return new Resolver();

                case "sm:":
                    return new UserInterface();

                case "ssl":
                    return new SslService();

                case "time:a":
                    return new Time.StaticService();

                case "time:s":
                    return new Time.StaticService();

                case "time:u":
                    return new Time.StaticService();

                case "vi:m":
                    return new ManagerRootService();

                case "vi:s":
                    return new SystemRootService();

                case "vi:u":
                    return new ApplicationRootService();
            }

            throw new NotImplementedException(name);
        }
    }
}

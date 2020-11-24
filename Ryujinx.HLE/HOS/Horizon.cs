using LibHac;
using LibHac.Bcat;
using LibHac.Fs;
using LibHac.FsSystem;
using Ryujinx.Common;
using Ryujinx.Configuration;
using Ryujinx.HLE.FileSystem.Content;
using Ryujinx.HLE.HOS.Services;
using Ryujinx.HLE.HOS.Services.Am.AppletAE.AllSystemAppletProxiesService.SystemAppletProxy;
using Ryujinx.HLE.HOS.Services.Arp;
using Ryujinx.HLE.HOS.Services.Bluetooth.BluetoothDriver;
using Ryujinx.HLE.HOS.Services.Mii;
using Ryujinx.HLE.HOS.Services.Nv;
using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostCtrl;
using Ryujinx.HLE.HOS.Services.Sm;
using Ryujinx.HLE.HOS.Services.SurfaceFlinger;
using Ryujinx.HLE.HOS.SystemState;
using Ryujinx.HLE.Loaders.Executables;
using Ryujinx.Horizon.Kernel;
using System;
using System.IO;

namespace Ryujinx.HLE.HOS
{
    public class Horizon : IDisposable
    {
        internal KernelContext KernelContext { get; }

        internal ServiceServer ServiceServer { get; }

        internal Switch Device { get; }

        internal SurfaceFlinger SurfaceFlinger { get; }

        public SystemStateMgr State { get; }

        internal AppletStateMgr AppletState { get; }

        internal BluetoothEventManager BluetoothEventManager { get; }

        internal ContentManager ContentManager { get; }

        public Keyset KeySet => Device.FileSystem.KeySet;

        private bool _isDisposed;

        public bool EnablePtc { get; set; }

        public IntegrityCheckLevel FsIntegrityCheckLevel { get; set; }

        public int GlobalAccessLogMode { get; set; }

        internal NvHostSyncpt HostSyncpoint { get; }

        internal LibHac.Horizon LibHacHorizonServer { get; private set; }
        internal HorizonClient LibHacHorizonClient { get; private set; }

        public Horizon(Switch device, ContentManager contentManager)
        {
            KernelContext = new KernelContext(device.Memory);

            ServiceServer = new ServiceServer(device);

            Device = device;

            State = new SystemStateMgr();

            AppletState = new AppletStateMgr(this);

            BluetoothEventManager = new BluetoothEventManager();

            ContentManager = contentManager;

            DatabaseImpl.Instance.InitializeDatabase(device);

            HostSyncpoint = new NvHostSyncpt(device);

            SurfaceFlinger = new SurfaceFlinger(device);

            ConfigurationState.Instance.System.EnableDockedMode.Event += OnDockedModeChange;

            InitLibHacHorizon();
        }

        public void InitializeServices()
        {
            KernelStatic.SetKernelContext(KernelContext);

            var smServer = new ServerBase(Device, "sm")
            {
                IsSm = true
            };

            // TODO: Find a way to wait for SM initialization here...

            ServiceServer.DiscoverAll();
        }

        public void LoadKip(string kipPath)
        {
            using IStorage kipFile = new LocalStorage(kipPath, FileAccess.Read);

            ProgramLoader.LoadKip(Device, new KipExecutable(kipFile));
        }

        private void InitLibHacHorizon()
        {
            LibHac.Horizon horizon = new LibHac.Horizon(null, Device.FileSystem.FsServer);

            horizon.CreateHorizonClient(out HorizonClient ryujinxClient).ThrowIfFailure();
            horizon.CreateHorizonClient(out HorizonClient bcatClient).ThrowIfFailure();

            ryujinxClient.Sm.RegisterService(new LibHacIReader(this), "arp:r").ThrowIfFailure();
            new BcatServer(bcatClient);

            LibHacHorizonServer = horizon;
            LibHacHorizonClient = ryujinxClient;
        }

        private void OnDockedModeChange(object sender, ReactiveEventArgs<bool> e)
        {
            if (e.NewValue != State.DockedMode)
            {
                State.DockedMode = e.NewValue;

                AppletState.EnqueueMessage(MessageInfo.OperationModeChanged);
                AppletState.EnqueueMessage(MessageInfo.PerformanceModeChanged);
                SignalDisplayResolutionChange();
            }
        }

        public void SignalDisplayResolutionChange()
        {
            ServiceServer.AmServer.SignalDisplayResolutionChanged();
        }

        public void SignalVsync()
        {
            ServiceServer.ViServer?.SignalVsync();
        }

        public void EnableMultiCoreScheduling()
        {
            KernelContext.EnableMultiCoreScheduling();
        }

        public void DisableMultiCoreScheduling()
        {
            KernelContext.DisableMultiCoreScheduling();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                ConfigurationState.Instance.System.EnableDockedMode.Event -= OnDockedModeChange;

                _isDisposed = true;

                SurfaceFlinger.Dispose();

                KernelStatic.TerminateAllProcesses(KernelContext);

                // Destroy nvservices channels as KThread could be waiting on some user events.
                // This is safe as KThread that are likely to call ioctls are going to be terminated by the post handler hook on the SVC facade.
                INvDrvServices.Destroy();

                KernelContext.Dispose();
            }
        }
    }
}

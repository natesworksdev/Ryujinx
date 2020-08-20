using LibHac;
using LibHac.Bcat;
using LibHac.Fs;
using LibHac.FsSystem;
using Ryujinx.Common;
using Ryujinx.Configuration;
using Ryujinx.HLE.FileSystem.Content;
using Ryujinx.HLE.HOS.Kernel;
using Ryujinx.HLE.HOS.Kernel.Process;
using Ryujinx.HLE.HOS.Kernel.Threading;
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
using System;
using System.IO;
using System.Threading;

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

        internal KEvent VsyncEvent { get; }

        internal KEvent DisplayResolutionChangeEvent { get; }

        public Keyset KeySet => Device.FileSystem.KeySet;

#pragma warning disable CS0649
        private bool _hasStarted;
#pragma warning restore CS0649
        private bool _isDisposed;

        public bool EnablePtc { get; set; }

        public IntegrityCheckLevel FsIntegrityCheckLevel { get; set; }

        public int GlobalAccessLogMode { get; set; }

        internal NvHostSyncpt HostSyncpoint { get; }

        internal LibHac.Horizon LibHacHorizonServer { get; private set; }
        internal HorizonClient LibHacHorizonClient { get; private set; }

        public Horizon(Switch device, ContentManager contentManager)
        {
            KernelContext = new KernelContext(device, device.Memory);

            ServiceServer = new ServiceServer(device);

            Device = device;

            State = new SystemStateMgr();

            AppletState = new AppletStateMgr(this);

            AppletState.SetFocus(true);

            BluetoothEventManager = new BluetoothEventManager();

            VsyncEvent = new KEvent(KernelContext);

            DisplayResolutionChangeEvent = new KEvent(KernelContext);

            ContentManager = contentManager;

            DatabaseImpl.Instance.InitializeDatabase(device);

            HostSyncpoint = new NvHostSyncpt(device);

            SurfaceFlinger = new SurfaceFlinger(device);

            ConfigurationState.Instance.System.EnableDockedMode.Event += OnDockedModeChange;

            InitLibHacHorizon();
        }

        public void InitializeServices()
        {
            IUserInterface sm = new IUserInterface(KernelContext);

            // Wait until SM server thread is done with initialization,
            // only then doing connections to SM is safe.
            sm.Server.InitDone.WaitOne();
            sm.Server.InitDone.Dispose();

            ServiceServer.DiscoverAll();
        }

        public void LoadKip(string kipPath)
        {
            using IStorage kipFile = new LocalStorage(kipPath, FileAccess.Read);

            ProgramLoader.LoadKip(KernelContext, new KipExecutable(kipFile));
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
            DisplayResolutionChangeEvent.ReadableEvent.Signal();
        }

        public void SignalVsync()
        {
            VsyncEvent.ReadableEvent.Signal();
        }

        public void EnableMultiCoreScheduling()
        {
            if (!_hasStarted)
            {
                KernelContext.Scheduler.MultiCoreScheduling = true;
            }
        }

        public void DisableMultiCoreScheduling()
        {
            if (!_hasStarted)
            {
                KernelContext.Scheduler.MultiCoreScheduling = false;
            }
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

                KProcess terminationProcess = new KProcess(KernelContext);
                KThread terminationThread = new KThread(KernelContext);

                terminationThread.Initialize(0, 0, 0, 3, 0, terminationProcess, ThreadType.Kernel, () =>
                {
                    // Force all threads to exit.
                    lock (KernelContext.Processes)
                    {
                        foreach (KProcess process in KernelContext.Processes.Values)
                        {
                            process.Terminate();
                        }
                    }

                    // Exit ourself now!
                    KernelContext.Scheduler.ExitThread(terminationThread);
                    KernelContext.Scheduler.GetCurrentThread().Exit();
                    KernelContext.Scheduler.RemoveThread(terminationThread);
                });

                terminationThread.Start();

                // Wait until the thread is actually started.
                while (terminationThread.HostThread.ThreadState == ThreadState.Unstarted)
                {
                    Thread.Sleep(10);
                }

                // Wait until the termination thread is done terminating all the other threads.
                terminationThread.HostThread.Join();

                // Destroy nvservices channels as KThread could be waiting on some user events.
                // This is safe as KThread that are likely to call ioctls are going to be terminated by the post handler hook on the SVC facade.
                INvDrvServices.Destroy();

                KernelContext.Dispose();
            }
        }
    }
}

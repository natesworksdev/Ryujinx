using Ryujinx.Audio.Backends.CompatLayer;
using Ryujinx.Audio.Integration;
using Ryujinx.Common.Configuration;
using Ryujinx.Graphics.Gpu;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS;
using Ryujinx.HLE.HOS.Services.Apm;
using Ryujinx.HLE.HOS.Services.Hid;
using Ryujinx.HLE.Loaders.Processes;
using Ryujinx.HLE.Ui;
using Ryujinx.Memory;
using System;

namespace Ryujinx.HLE
{
    public class Switch : IDisposable
    {
        public HLEConfiguration Configuration { get; }
        public IHardwareDeviceDriver AudioDeviceDriver { get; }
        public MemoryBlock Memory { get; }
        public GpuContext Gpu { get; }
        public VirtualFileSystem FileSystem { get; }
        public HOS.Horizon System { get; }
        public ProcessLoader Processes { get; }
        public PerformanceStatistics Statistics { get; }
        public Hid Hid { get; }
        public TamperMachine TamperMachine { get; }
        public IHostUiHandler UiHandler { get; }
        public SpeedState SpeedState { get; private set; }

        public bool EnableDeviceVsync { get; set; } = true;

        public decimal NormalEmulationSpeed { get; set; }

        public decimal FastForwardEmulationSpeed { get; set; }

        public decimal TurboEmulationSpeed { get; set; }

        public int TargetFps { get; private set; } = 60;

        public Action TargetFpsChanged { get; set; }

        public bool IsFrameAvailable => Gpu.Window.IsFrameAvailable;

        public Switch(HLEConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration.GpuRenderer);
            ArgumentNullException.ThrowIfNull(configuration.AudioDeviceDriver);
            ArgumentNullException.ThrowIfNull(configuration.UserChannelPersistence);

            Configuration = configuration;
            FileSystem = Configuration.VirtualFileSystem;
            UiHandler = Configuration.HostUiHandler;

            MemoryAllocationFlags memoryAllocationFlags = configuration.MemoryManagerMode == MemoryManagerMode.SoftwarePageTable
                ? MemoryAllocationFlags.Reserve
                : MemoryAllocationFlags.Reserve | MemoryAllocationFlags.Mirrorable;

#pragma warning disable IDE0055 // Disable formatting
            AudioDeviceDriver = new CompatLayerHardwareDeviceDriver(Configuration.AudioDeviceDriver);
            Memory            = new MemoryBlock(Configuration.MemoryConfiguration.ToDramSize(), memoryAllocationFlags);
            Gpu               = new GpuContext(Configuration.GpuRenderer);
            System            = new HOS.Horizon(this);
            Statistics        = new PerformanceStatistics();
            Hid               = new Hid(this, System.HidStorage);
            Processes         = new ProcessLoader(this);
            TamperMachine     = new TamperMachine();

            System.State.SetLanguage(Configuration.SystemLanguage);
            System.State.SetRegion(Configuration.Region);

            EnableDeviceVsync                       = Configuration.EnableVsync;
            System.State.DockedMode                 = Configuration.EnableDockedMode;
            System.PerformanceState.PerformanceMode = System.State.DockedMode ? PerformanceMode.Boost : PerformanceMode.Default;
            System.EnablePtc                        = Configuration.EnablePtc;
            System.FsIntegrityCheckLevel            = Configuration.FsIntegrityCheckLevel;
            System.GlobalAccessLogMode              = Configuration.FsGlobalAccessLogMode;
            
            NormalEmulationSpeed                    = Configuration.NormalEmulationSpeed;
            FastForwardEmulationSpeed               = Configuration.FastForwardEmulationSpeed;
            TurboEmulationSpeed                     = Configuration.TurboEmulationSpeed;
            SetSpeedState(SpeedState.Normal);
#pragma warning restore IDE0055
        }

        public bool LoadCart(string exeFsDir, string romFsFile = null)
        {
            return Processes.LoadUnpackedNca(exeFsDir, romFsFile);
        }

        public bool LoadXci(string xciFile)
        {
            return Processes.LoadXci(xciFile);
        }

        public bool LoadNca(string ncaFile)
        {
            return Processes.LoadNca(ncaFile);
        }

        public bool LoadNsp(string nspFile)
        {
            return Processes.LoadNsp(nspFile);
        }

        public bool LoadProgram(string fileName)
        {
            return Processes.LoadNxo(fileName);
        }

        public bool WaitFifo()
        {
            return Gpu.GPFifo.WaitForCommands();
        }

        public void ProcessFrame()
        {
            Gpu.ProcessShaderCacheQueue();
            Gpu.Renderer.PreFrame();
            Gpu.GPFifo.DispatchCalls();
        }

        public bool ConsumeFrameAvailable()
        {
            return Gpu.Window.ConsumeFrameAvailable();
        }

        public void PresentFrame(Action swapBuffersCallback)
        {
            Gpu.Window.Present(swapBuffersCallback);
        }

        public void SetVolume(float volume)
        {
            System.SetVolume(Math.Clamp(volume, 0, 1));
        }

        public float GetVolume()
        {
            return System.GetVolume();
        }

        public void EnableCheats()
        {
            ModLoader.EnableCheats(Processes.ActiveApplication.ProgramId, TamperMachine);
        }

        public void SetSpeedState(SpeedState newState)
        {
            SpeedState = newState;
            TargetFps = (int)(NormalEmulationSpeed * 60);

            if (SpeedState.HasFlag(SpeedState.FastForward))
            {
                TargetFps = (int)(FastForwardEmulationSpeed * 60);
            }

            if (SpeedState.HasFlag(SpeedState.Turbo))
            {
                TargetFps = (int)(TurboEmulationSpeed * 60);
            }

            // If configuration set to -1, we interpret as unlimited/vsync off.
            if (TargetFps < 0)
            {
                TargetFps = -1;
            }

            TargetFpsChanged?.Invoke();
        }

        public string GetSpeedStateStatus()
        {
            string status = "Normal";

            if (SpeedState.HasFlag(SpeedState.FastForward))
            {
                status = "Fast Forward";
            }

            if (SpeedState.HasFlag(SpeedState.Turbo))
            {
                status = "Turbo";
            }

            return status;
        }

        public void ToggleFastForward()
        {
            if (this.SpeedState.HasFlag(SpeedState.FastForward))
            {
                this.SpeedState &= ~SpeedState.FastForward;
            }
            else
            {
                this.SpeedState |= SpeedState.FastForward;
            }

            this.SetSpeedState(this.SpeedState);
        }

        public void ToggleTurbo()
        {
            if (this.SpeedState.HasFlag(SpeedState.Turbo))
            {
                this.SpeedState &= ~SpeedState.Turbo;
            }
            else
            {
                this.SpeedState |= SpeedState.Turbo;
            }

            this.SetSpeedState(this.SpeedState);
        }

        public bool IsAudioMuted()
        {
            return System.GetVolume() == 0;
        }

        public void DisposeGpu()
        {
            Gpu.Dispose();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                System.Dispose();
                AudioDeviceDriver.Dispose();
                FileSystem.Dispose();
                Memory.Dispose();
            }
        }
    }
}

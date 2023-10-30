using Ryujinx.Common.Logging;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace Ryujinx.Common.SystemInterop
{
    /// <summary>
    /// Handle Windows Multimedia timer resolution.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public partial class WindowsMultimediaTimerResolution : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct TimeCaps
        {
            public uint wPeriodMin;
            public uint wPeriodMax;
        }

        [LibraryImport("winmm.dll", EntryPoint = "timeGetDevCaps", SetLastError = true)]
        private static partial uint TimeGetDevCaps(ref TimeCaps timeCaps, uint sizeTimeCaps);

        [LibraryImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static partial uint TimeBeginPeriod(uint uMilliseconds);

        [LibraryImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        private static partial uint TimeEndPeriod(uint uMilliseconds);

        [DllImport("ntdll.dll", SetLastError = true)]
        static extern int NtSetTimerResolution(int DesiredResolution, bool SetResolution, out int CurrentResolution);

        [DllImport("ntdll.dll", SetLastError = true)]
        static extern int NtQueryTimerResolution(out int MaximumResolution, out int MinimumResolution, out int CurrentResolution);


        private uint _targetResolutionInMilliseconds;
        private bool _isActive;

        /// <summary>
        /// Create a new <see cref="WindowsMultimediaTimerResolution"/> and activate the given resolution.
        /// </summary>
        /// <param name="targetResolutionInMilliseconds"></param>
        public WindowsMultimediaTimerResolution(uint targetResolutionInMilliseconds)
        {
            _targetResolutionInMilliseconds = targetResolutionInMilliseconds;

            EnsureResolutionSupport();

            Activate();

            AcitvateHiRes();            
            MeasureSleepTime();
        }

        private void EnsureResolutionSupport()
        {
            TimeCaps timeCaps = default;

            uint result = TimeGetDevCaps(ref timeCaps, (uint)Unsafe.SizeOf<TimeCaps>());

            if (result != 0)
            {
                Logger.Notice.Print(LogClass.Application, $"timeGetDevCaps failed with result: {result}");
            }
            else
            {
                uint supportedTargetResolutionInMilliseconds = Math.Min(Math.Max(timeCaps.wPeriodMin, _targetResolutionInMilliseconds), timeCaps.wPeriodMax);

                if (supportedTargetResolutionInMilliseconds != _targetResolutionInMilliseconds)
                {
                    Logger.Notice.Print(LogClass.Application, $"Target resolution isn't supported by OS, using closest resolution: {supportedTargetResolutionInMilliseconds}ms");

                    _targetResolutionInMilliseconds = supportedTargetResolutionInMilliseconds;
                }
            }
        }

        private void AcitvateHiRes()
        {
            int min, max, curr;
            NtQueryTimerResolution(out max, out min, out curr);

            if (min > 0)
            {
                NtSetTimerResolution(min, true, out curr);
            }

        }

        [Conditional("DEBUG")]
        private void MeasureSleepTime()
        {
            const int c_iter = 128;

            var start = PerformanceCounter.ElapsedTicks;

            for (int i = 0; i < c_iter; i++)
            {
                Thread.Sleep(1);
            }

            var end = PerformanceCounter.ElapsedTicks;

            var sleepMs = ((end - start) / 10000.0) / c_iter;

            Logger.Notice.Print(LogClass.Application, $"Avg. sleep duration {sleepMs}ms");
        }

        private void Activate()
        {
            uint result = TimeBeginPeriod(_targetResolutionInMilliseconds);

            if (result != 0)
            {
                Logger.Notice.Print(LogClass.Application, $"timeBeginPeriod failed with result: {result}");
            }
            else
            {
                _isActive = true;
            }
        }

        private void Disable()
        {
            if (_isActive)
            {
                uint result = TimeEndPeriod(_targetResolutionInMilliseconds);

                if (result != 0)
                {
                    Logger.Notice.Print(LogClass.Application, $"timeEndPeriod failed with result: {result}");
                }
                else
                {
                    _isActive = false;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Disable();
            }
        }
    }
}

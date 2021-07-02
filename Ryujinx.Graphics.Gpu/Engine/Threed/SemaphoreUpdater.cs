using Ryujinx.Common;
using Ryujinx.Graphics.Device;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Gpu.State;

namespace Ryujinx.Graphics.Gpu.Engine.Threed
{
    class SemaphoreUpdater
    {
        private const int NsToTicksFractionNumerator = 384;
        private const int NsToTicksFractionDenominator = 625;

        private readonly GpuContext _context;
        private readonly GpuChannel _channel;
        private readonly DeviceState<ThreedClassState> _state;

        public SemaphoreUpdater(GpuContext context, GpuChannel channel, DeviceState<ThreedClassState> state)
        {
            _context = context;
            _channel = channel;
            _state = state;
        }

        /// <summary>
        /// Resets the value of an internal GPU counter back to zero.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        public void ResetCounter(int argument)
        {
            ResetCounterType type = (ResetCounterType)argument;

            switch (type)
            {
                case ResetCounterType.SamplesPassed:
                    _context.Renderer.ResetCounter(CounterType.SamplesPassed);
                    break;
                case ResetCounterType.PrimitivesGenerated:
                    _context.Renderer.ResetCounter(CounterType.PrimitivesGenerated);
                    break;
                case ResetCounterType.TransformFeedbackPrimitivesWritten:
                    _context.Renderer.ResetCounter(CounterType.TransformFeedbackPrimitivesWritten);
                    break;
            }
        }

        /// <summary>
        /// Writes a GPU counter to guest memory.
        /// </summary>
        /// <param name="argument">Method call argument</param>
        public void Report(int argument)
        {
            SemaphoreOperation op = (SemaphoreOperation)(argument & 3);
            ReportCounterType type = (ReportCounterType)((argument >> 23) & 0x1f);

            switch (op)
            {
                case SemaphoreOperation.Release: ReleaseSemaphore(); break;
                case SemaphoreOperation.Counter: ReportCounter(type); break;
            }
        }

        /// <summary>
        /// Writes (or Releases) a GPU semaphore value to guest memory.
        /// </summary>
        private void ReleaseSemaphore()
        {
            _channel.MemoryManager.Write(_state.State.SemaphoreAddress.Pack(), _state.State.SemaphorePayload);

            _context.AdvanceSequence();
        }

        /// <summary>
        /// Packed GPU counter data (including GPU timestamp) in memory.
        /// </summary>
        private struct CounterData
        {
            public ulong Counter;
            public ulong Timestamp;
        }

        /// <summary>
        /// Writes a GPU counter to guest memory.
        /// This also writes the current timestamp value.
        /// </summary>
        /// <param name="type">Counter to be written to memory</param>
        private void ReportCounter(ReportCounterType type)
        {
            ulong gpuVa = _state.State.SemaphoreAddress.Pack();

            ulong ticks = ConvertNanosecondsToTicks((ulong)PerformanceCounter.ElapsedNanoseconds);

            if (GraphicsConfig.FastGpuTime)
            {
                // Divide by some amount to report time as if operations were performed faster than they really are.
                // This can prevent some games from switching to a lower resolution because rendering is too slow.
                ticks /= 256;
            }

            ICounterEvent counter = null;

            void resultHandler(object evt, ulong result)
            {
                CounterData counterData = new CounterData
                {
                    Counter = result,
                    Timestamp = ticks
                };

                if (counter?.Invalid != true)
                {
                    _channel.MemoryManager.Write(gpuVa, counterData);
                }
            }

            switch (type)
            {
                case ReportCounterType.Zero:
                    resultHandler(null, 0);
                    break;
                case ReportCounterType.SamplesPassed:
                    counter = _context.Renderer.ReportCounter(CounterType.SamplesPassed, resultHandler);
                    break;
                case ReportCounterType.PrimitivesGenerated:
                    counter = _context.Renderer.ReportCounter(CounterType.PrimitivesGenerated, resultHandler);
                    break;
                case ReportCounterType.TransformFeedbackPrimitivesWritten:
                    counter = _context.Renderer.ReportCounter(CounterType.TransformFeedbackPrimitivesWritten, resultHandler);
                    break;
            }

            _channel.MemoryManager.CounterCache.AddOrUpdate(gpuVa, counter);
        }

        /// <summary>
        /// Converts a nanoseconds timestamp value to Maxwell time ticks.
        /// </summary>
        /// <remarks>
        /// The frequency is 614400000 Hz.
        /// </remarks>
        /// <param name="nanoseconds">Timestamp in nanoseconds</param>
        /// <returns>Maxwell ticks</returns>
        private static ulong ConvertNanosecondsToTicks(ulong nanoseconds)
        {
            // We need to divide first to avoid overflows.
            // We fix up the result later by calculating the difference and adding
            // that to the result.
            ulong divided = nanoseconds / NsToTicksFractionDenominator;

            ulong rounded = divided * NsToTicksFractionDenominator;

            ulong errorBias = (nanoseconds - rounded) * NsToTicksFractionNumerator / NsToTicksFractionDenominator;

            return divided * NsToTicksFractionNumerator + errorBias;
        }
    }
}

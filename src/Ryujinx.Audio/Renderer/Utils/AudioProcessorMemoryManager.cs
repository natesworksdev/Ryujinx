using System.Runtime.CompilerServices;
using CpuAddress = System.UInt64;
using DspAddress = System.UInt64;

namespace Ryujinx.Audio.Renderer.Utils
{
    /// <summary>
    /// The <see cref="Dsp.AudioProcessor"/> memory management
    /// </summary>
    /// <remarks>This is stub for the most part but is kept to permit LLE if wanted.</remarks>
    static class AudioProcessorMemoryManager
    {
        /// <summary>
        /// Map the given <see cref="CpuAddress"/> to the <see cref="Dsp.AudioProcessor"/> address space.
        /// </summary>
        /// <param name="processHandle">The process owning the CPU memory.</param>
        /// <param name="cpuAddress">The <see cref="CpuAddress"/> to map.</param>
        /// <param name="size">The size of the CPU memory region to map.</param>
        /// <returns>The address on the <see cref="Dsp.AudioProcessor"/> address space.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable IDE0060
        public static DspAddress Map(uint processHandle, CpuAddress cpuAddress, ulong size)
        {
            return cpuAddress;
        }
#pragma warning restore IDE0060

        /// <summary>
        /// Unmap the given <see cref="CpuAddress"/> from the <see cref="Dsp.AudioProcessor"/> address space.
        /// </summary>
        /// <param name="processHandle">The process owning the CPU memory.</param>
        /// <param name="cpuAddress">The <see cref="CpuAddress"/> to unmap.</param>
        /// <param name="size">The size of the CPU memory region to unmap.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable IDE0060
        public static void Unmap(uint processHandle, CpuAddress cpuAddress, ulong size)
        {
        }
#pragma warning restore IDE0060

        /// <summary>
        /// Invalidate the <see cref="Dsp.AudioProcessor"/> data cache at the given address.
        /// </summary>
        /// <param name="address">The base DSP address to invalidate</param>
        /// <param name="size">The size of the DSP memory region to invalidate.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable IDE0060
        public static void InvalidateDspCache(DspAddress address, ulong size)
        {
        }
#pragma warning restore IDE0060

        /// <summary>
        /// Invalidate the CPU data cache at the given address.
        /// </summary>
        /// <param name="address">The base <see cref="CpuAddress"/> to invalidate</param>
        /// <param name="size">The size of the CPU memory region to invalidate.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable IDE0060
        public static void InvalidateDataCache(CpuAddress address, ulong size)
        {
        }
#pragma warning restore IDE0060
    }
}
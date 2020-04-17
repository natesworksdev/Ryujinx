using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Common.Utilities {
    [DebuggerDisplay("{ToString()}")]
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public struct Buffer16 {
#pragma warning disable IDE0044 // Add readonly modifier
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private ulong _dummy0;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private ulong _dummy1;
#pragma warning restore IDE0044 // Add readonly modifier

        public byte this[int i] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => ReadOnlyBytes[i];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Bytes[i] = value;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly ReadOnlySpan<byte> ReadOnlyBytes => SpanUtils.AsReadOnlyByteSpan(in this);
        public Span<byte> Bytes => SpanUtils.AsByteSpan(ref this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Span<byte> (in Buffer16 value) {
            // Prevent a defensive copy by changing the read-only in reference to a reference with Unsafe.AsRef()
            return SpanUtils.AsByteSpan(ref Unsafe.AsRef(in value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<byte> (in Buffer16 value) {
            return SpanUtils.AsReadOnlyByteSpan(in value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T As<T> () where T : unmanaged {
            if (Unsafe.SizeOf<T>() > Unsafe.SizeOf<Buffer16>()) {
                throw new ArgumentException();
            }

            return ref MemoryMarshal.GetReference(AsSpan<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan<T> () where T : unmanaged {
            return SpanUtils.AsSpan<Buffer16, T>(ref this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<T> AsReadOnlySpan<T> () where T : unmanaged {
            return SpanUtils.AsReadOnlySpan<Buffer16, T>(in this);
        }
    }
}
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Ryujinx.Ui.Common.Helper
{
    [SupportedOSPlatform("macos")]
    public static partial class NativeMacOS
    {
        private const string FoundationFramework = "/System/Library/Frameworks/Foundation.framework/Foundation";
        private const string AppKitFramework = "/System/Library/Frameworks/AppKit.framework/AppKit";

        [LibraryImport(FoundationFramework)]
        public static partial IntPtr CFStringCreateWithBytes(IntPtr allocator, IntPtr buffer, long bufferLength, CFStringEncoding encoding, [MarshalAs(UnmanagedType.Bool)]bool isExternalRepresentation);

        [LibraryImport(FoundationFramework)]
        public static partial void CFRelease(IntPtr handle);

        [LibraryImport(AppKitFramework)]
        public static partial IntPtr NSSelectorFromString(IntPtr cfstr);

        [LibraryImport(AppKitFramework, StringMarshalling = StringMarshalling.Utf16)]
        public static partial IntPtr objc_getClass(string name);

        [LibraryImport(FoundationFramework)]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector);

        [LibraryImport(FoundationFramework)]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, byte value);

        [LibraryImport(FoundationFramework)]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, IntPtr value);

        [LibraryImport(FoundationFramework)]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, NSRect point);

        [LibraryImport(FoundationFramework)]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, double value);

        [LibraryImport(FoundationFramework, EntryPoint = "objc_msgSend")]
        public static partial IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector);

        [LibraryImport(FoundationFramework, EntryPoint = "objc_msgSend")]
        public static partial IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, IntPtr param);

        public struct Selector
        {
            public readonly IntPtr NativePtr;

            public unsafe Selector(string name)
            {
                IntPtr cfstrSelector = CreateCFString(name);
                IntPtr selector = NSSelectorFromString(cfstrSelector);
                CFRelease(cfstrSelector);
                NativePtr = selector;
            }

            public static implicit operator Selector(string value) => new Selector(value);
        }

        public struct NSURL : IDisposable
        {
            public readonly IntPtr URLPtr;

            public unsafe NSURL(string path)
            {
                IntPtr cfstrPath = CreateCFString(path);
                IntPtr nsUrl = objc_getClass("NSURL");
                URLPtr = IntPtr_objc_msgSend(nsUrl, new Selector("fileURLWithPath:"), cfstrPath);
                CFRelease(cfstrPath);
            }

            public void Dispose()
            {
                CFRelease(URLPtr);
            }
        }

        public unsafe static IntPtr CreateCFString(string aString)
        {
            var bytes = Encoding.Unicode.GetBytes(aString);
            fixed (byte* b = bytes) {
                var cfStr = CFStringCreateWithBytes(IntPtr.Zero, (IntPtr)b, bytes.Length, CFStringEncoding.UTF16, false);
                return cfStr;
            }
        }

        public struct NSPoint
        {
            public double X;
            public double Y;

            public NSPoint(double x, double y)
            {
                X = x;
                Y = y;
            }
        }

        public struct NSRect
        {
            public NSPoint Pos;
            public NSPoint Size;

            public NSRect(double x, double y, double width, double height)
            {
                Pos = new NSPoint(x, y);
                Size = new NSPoint(width, height);
            }
        }

        public enum CFStringEncoding : uint
        {
            UTF16 = 0x0100,
            UTF16BE = 0x10000100,
            UTF16LE = 0x14000100,
            ASCII = 0x0600
        }
    }
}
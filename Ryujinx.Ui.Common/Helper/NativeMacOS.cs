using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Ryujinx.Ui.Common.Helper
{
    [SupportedOSPlatform("macos")]
    public static partial class NativeMacOS
    {
        private const string ObjCRuntime = "/usr/lib/libobjc.A.dylib";
        private const string CoreFoundationFramework = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
        private const string FoundationFramework = "/System/Library/Frameworks/Foundation.framework/Foundation";

        [LibraryImport(CoreFoundationFramework)]
        public static partial IntPtr CFStringCreateWithBytes(IntPtr allocator, IntPtr buffer, long bufferLength, CFStringEncoding encoding, [MarshalAs(UnmanagedType.Bool)]bool isExternalRepresentation);

        [LibraryImport(CoreFoundationFramework)]
        public static partial void CFRelease(IntPtr handle);

        [LibraryImport(FoundationFramework)]
        public static partial IntPtr NSSelectorFromString(IntPtr cfstr);

        [LibraryImport(ObjCRuntime, StringMarshalling = StringMarshalling.Utf8)]
        public static partial IntPtr objc_getClass(string name);

        [LibraryImport(ObjCRuntime)]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector);

        [LibraryImport(ObjCRuntime)]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, byte value);

        [LibraryImport(ObjCRuntime)]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, IntPtr value);

        [LibraryImport(ObjCRuntime)]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, NSRect point);

        [LibraryImport(ObjCRuntime)]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, double value);

        [LibraryImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
        public static partial IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector);

        [LibraryImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
        public static partial IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, IntPtr param);

        [LibraryImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool bool_objc_msgSend(IntPtr receiver, Selector selector, IntPtr param);

        public struct Selector
        {
            public readonly IntPtr NativePtr;

            public unsafe Selector(string name)
            {
                CFString cfstrSelector = new CFString(name);
                IntPtr selector = NSSelectorFromString(cfstrSelector.StrPtr);
                cfstrSelector.Dispose();
                NativePtr = selector;
            }

            public static implicit operator Selector(string value) => new Selector(value);
        }

        public struct NSURL : IDisposable
        {
            public readonly IntPtr URLPtr;

            public unsafe NSURL(string path)
            {
                CFString cfstrPath = new CFString(path);
                IntPtr nsUrl = objc_getClass("NSURL");
                URLPtr = IntPtr_objc_msgSend(nsUrl, new Selector("URLWithString:"), cfstrPath.StrPtr);
                cfstrPath.Dispose();
            }

            public void Dispose()
            {
                CFRelease(URLPtr);
            }
        }

        public struct CFString : IDisposable
        {
            public readonly IntPtr StrPtr;

            public unsafe CFString(string aString)
            {
                var bytes = Encoding.Unicode.GetBytes(aString);
                fixed (byte* b = bytes) {
                    StrPtr = CFStringCreateWithBytes(IntPtr.Zero, (IntPtr)b, bytes.Length, CFStringEncoding.UTF16, false);
                }
            }

            public void Dispose()
            {
                CFRelease(StrPtr);
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
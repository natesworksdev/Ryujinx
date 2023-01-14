using System;
using System.Runtime.Versioning;
using Avalonia;
using Ryujinx.Ui.Common.Helper;

namespace Ryujinx.Ava.UI.Helpers
{
    public delegate void UpdateBoundsCallbackDelegate(Rect rect);

    [SupportedOSPlatform("macos")]
    static class MetalHelper
    {
        public static IntPtr GetMetalLayer(out IntPtr nsView, out UpdateBoundsCallbackDelegate updateBounds)
        {
            // Create a new CAMetalLayer.
            IntPtr layerClass = NativeMacOS.objc_getClass("CAMetalLayer");
            IntPtr metalLayer = NativeMacOS.IntPtr_objc_msgSend(layerClass, "alloc");
            NativeMacOS.objc_msgSend(metalLayer, "init");

            // Create a child NSView to render into.
            IntPtr nsViewClass = NativeMacOS.objc_getClass("NSView");
            IntPtr child = NativeMacOS.IntPtr_objc_msgSend(nsViewClass, "alloc");
            NativeMacOS.objc_msgSend(child, "init", new NativeMacOS.NSRect(0, 0, 0, 0));

            // Make its renderer our metal layer.
            NativeMacOS.objc_msgSend(child, "setWantsLayer:", 1);
            NativeMacOS.objc_msgSend(child, "setLayer:", metalLayer);
            NativeMacOS.objc_msgSend(metalLayer, "setContentsScale:", Program.DesktopScaleFactor);

            // Ensure the scale factor is up to date.
            updateBounds = rect => {
                NativeMacOS.objc_msgSend(metalLayer, "setContentsScale:", Program.DesktopScaleFactor);
            };

            nsView = child;
            return metalLayer;
        }
    }
}
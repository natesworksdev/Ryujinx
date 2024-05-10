using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Silk.NET.OpenGL.Legacy;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Ryujinx.Graphics.OpenGL
{
    public static class Debugger
    {
        private static DebugProc _debugCallback;

        private static int _counter;

        public static void Initialize(GL gl, GraphicsDebugLevel logLevel)
        {
            // Disable everything
            gl.DebugMessageControl(DebugSource.DontCare, DebugType.DontCare, DebugSeverity.DontCare, 0, (uint[])null, false);

            if (logLevel == GraphicsDebugLevel.None)
            {
                gl.Disable(EnableCap.DebugOutputSynchronous);
                gl.DebugMessageCallback(null, IntPtr.Zero);

                return;
            }

            gl.Enable(EnableCap.DebugOutputSynchronous);

            if (logLevel == GraphicsDebugLevel.Error)
            {
                gl.DebugMessageControl(DebugSource.DontCare, DebugType.DebugTypeError, DebugSeverity.DontCare, 0, (uint[])null, true);
            }
            else if (logLevel == GraphicsDebugLevel.Slowdowns)
            {
                gl.DebugMessageControl(DebugSource.DontCare, DebugType.DebugTypeError, DebugSeverity.DontCare, 0, (uint[])null, true);
                gl.DebugMessageControl(DebugSource.DontCare, DebugType.DebugTypePerformance, DebugSeverity.DontCare, 0, (uint[])null, true);
            }
            else
            {
                gl.DebugMessageControl(DebugSource.DontCare, DebugType.DontCare, DebugSeverity.DontCare, 0, (uint[])null, true);
            }

            _counter = 0;
            _debugCallback = GLDebugHandler;

            gl.DebugMessageCallback(_debugCallback, IntPtr.Zero);

            Logger.Warning?.Print(LogClass.Gpu, "OpenGL Debugging is enabled. Performance will be negatively impacted.");
        }

        private static void GLDebugHandler(
            GLEnum source,
            GLEnum type,
            int id,
            GLEnum severity,
            int length,
            IntPtr message,
            IntPtr userParam)
        {
            string msg = Marshal.PtrToStringUTF8(message).Replace('\n', ' ');

            DebugType debugType = (DebugType)type;
            DebugSource debugSource = (DebugSource)source;

            switch (debugType)
            {
                case DebugType.DebugTypeError:
                    Logger.Error?.Print(LogClass.Gpu, $"{severity}: {msg}\nCallStack={Environment.StackTrace}", "GLERROR");
                    break;
                case DebugType.DebugTypePerformance:
                    Logger.Warning?.Print(LogClass.Gpu, $"{severity}: {msg}", "GLPERF");
                    break;
                case DebugType.DebugTypePushGroup:
                    Logger.Info?.Print(LogClass.Gpu, $"{{ ({id}) {severity}: {msg}", "GLINFO");
                    break;
                case DebugType.DebugTypePopGroup:
                    Logger.Info?.Print(LogClass.Gpu, $"}} ({id}) {severity}: {msg}", "GLINFO");
                    break;
                default:
                    if (debugSource == DebugSource.DebugSourceApplication)
                    {
                        Logger.Info?.Print(LogClass.Gpu, $"{type} {severity}: {msg}", "GLINFO");
                    }
                    else
                    {
                        Logger.Debug?.Print(LogClass.Gpu, $"{type} {severity}: {msg}", "GLDEBUG");
                    }
                    break;
            }
        }

        // Useful debug helpers
        public static void PushGroup(GL gl, string dbgMsg)
        {
            int counter = Interlocked.Increment(ref _counter);

            gl.PushDebugGroup(DebugSource.DebugSourceApplication, (uint)counter, (uint)dbgMsg.Length, dbgMsg);
        }

        public static void PopGroup(GL gl)
        {
            gl.PopDebugGroup();
        }

        public static void Print(GL gl, string dbgMsg, DebugType type = DebugType.DebugTypeMarker, DebugSeverity severity = DebugSeverity.DebugSeverityNotification, int id = 999999)
        {
            gl.DebugMessageInsert(DebugSource.DebugSourceApplication, type, (uint)id, severity, (uint)dbgMsg.Length, dbgMsg);
        }
    }
}

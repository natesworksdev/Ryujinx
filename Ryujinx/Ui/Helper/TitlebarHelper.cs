#pragma warning disable 0612

using Gdk;
using Gtk;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Ryujinx.Ui.Helper
{

    public class TitlebarHelper {
        
        #region Native Imports
        [DllImport("DwmApi")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, int[] attrValue, int attrSize);
        [DllImport("user32.dll")] static extern IntPtr SetWindowsHookEx(int hookType, HookProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll")] static extern int CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet=CharSet.Unicode)] static extern IntPtr GetModuleHandle([MarshalAs(UnmanagedType.LPWStr)] string lpModuleName);
        delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

        static IntPtr WindowCreationDetector;
        static List<IntPtr> AppWindows = new List<IntPtr>();

        #endregion

        static IntPtr OnShellCallback(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code == 1) // HSHELL_WINDOWCREATED
            {
                ApplyTitleBarColor(wParam);
                AppWindows.Add(wParam);
            }

            return CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
        }

        public static void Initialize()
        {
            if (OperatingSystem.IsWindows())
            {
                Process current = Process.GetCurrentProcess();
                //Listen For Window Created Events
                WindowCreationDetector = SetWindowsHookEx(10, OnShellCallback, GetModuleHandle(current.MainModule.ModuleName), (uint)current.Threads[0].Id);
            }
        }

        public static void ApplyTitleBarColor(IntPtr hwnd, int color = -1)
        {
            if (OperatingSystem.IsWindows()){

                if (color == -1){
                    color = GetThemeColor();
                }

                if (hwnd == IntPtr.Zero)
                {
                    foreach (IntPtr handle in AppWindows)
                    {
                        DwmSetWindowAttribute(handle, 35, new int[] { color }, 4);
                    }
                }
                else 
                {
                    DwmSetWindowAttribute(hwnd, 35, new int[] { color }, 4);
                }
                
            }
        }

        public static int GetThemeColor(){
            StyleContext emptyContext = new StyleContext();
            WidgetPath emptyPath = new WidgetPath();
            emptyPath.AppendType(Box.GType);
            emptyContext.AddClass("background");
            emptyContext.Path = emptyPath;
            emptyContext.Screen = Screen.Default;
            emptyContext.Save();

            RGBA color = emptyContext.GetBackgroundColor(StateFlags.Normal);

            //Convert To The BGR Int Format That Windows Understands
            string ch = $"{(int)(color.Red*255):X2}{(int)(color.Green*255):X2}{(int)(color.Blue*255):X2}";
            string R = ch.Substring(0, 2);
            string G = ch.Substring(2, 2);
            string B = ch.Substring(4, 2);
            int windowsColor = int.Parse(B + G + R, System.Globalization.NumberStyles.HexNumber);
            return windowsColor;
        }

        //Must Be Called When Theme Is Changed
        public static void ThemeChanged()
        {
            ApplyTitleBarColor(IntPtr.Zero);
        }
    }

}
using ImGuiNET;
using OpenTK;
using Ryujinx.Audio;
using Ryujinx.Audio.OpenAL;
using Ryujinx.Graphics.Gal;
using Ryujinx.Graphics.Gal.OpenGL;
using Ryujinx.HLE;
using Ryujinx.HLE.Input;
using OpenTK.Graphics;
using OpenTK.Input;
using System;
using System.IO;

namespace Ryujinx.UI
{
    class MainUI : WindowHelper
    {
        //toggles
        private bool ShowUI = true;
        private bool ShowFileDialog = false;
        private bool _isRunning = false;
        private bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                if (!value)
                {
                    ShowUI = true;
                }
            }
        }

        private string CurrentPath = Environment.CurrentDirectory;
        private string PackagePath = string.Empty;

        private const int TouchScreenWidth = 1280;
        private const int TouchScreenHeight = 720;

        private const float TouchScreenRatioX = (float)TouchScreenWidth / TouchScreenHeight;
        private const float TouchScreenRatioY = (float)TouchScreenHeight / TouchScreenWidth;

        FilePicker FileDialog;

        IGalRenderer Renderer;
        IAalOutput AudioOut;
        Switch Ns;

        public MainUI() : base("Test")
        {
            FileDialog = FilePicker.GetFilePicker("rom",null);

            Renderer = new OpenGLRenderer();

            AudioOut = new OpenALAudioOut();

            Ns = new Switch(Renderer, AudioOut);

            Config.Read(Ns.Log);

            Ns.Log.Updated += ConsoleLog.PrintLog;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            VSync = VSyncMode.On;

            Renderer.SetWindowSize(Width, Height);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            _deltaTime = (float)e.Time;
            if (ShowUI)
            {
                StartFrame();
                RenderUI();
                EndFrame();
            }
            else
            {
                Ns.Statistics.StartSystemFrame();

                Title = $"Ryujinx Screen - (Vsync: {VSync} - FPS: {Ns.Statistics.SystemFrameRate:0} - Guest FPS: " +
                    $"{Ns.Statistics.GameFrameRate:0})";

                Renderer.RunActions();
                Renderer.Render();

                SwapBuffers();

                Ns.Statistics.EndSystemFrame();

                Ns.Os.SignalVsync();
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (!ShowUI)
            {
                HidControllerButtons CurrentButton = 0;
                HidJoystickPosition LeftJoystick;
                HidJoystickPosition RightJoystick;

                int LeftJoystickDX = 0;
                int LeftJoystickDY = 0;
                int RightJoystickDX = 0;
                int RightJoystickDY = 0;

                //RightJoystick
                if (Keyboard[(Key)Config.FakeJoyCon.Left.StickUp]) LeftJoystickDY = short.MaxValue;
                if (Keyboard[(Key)Config.FakeJoyCon.Left.StickDown]) LeftJoystickDY = -short.MaxValue;
                if (Keyboard[(Key)Config.FakeJoyCon.Left.StickLeft]) LeftJoystickDX = -short.MaxValue;
                if (Keyboard[(Key)Config.FakeJoyCon.Left.StickRight]) LeftJoystickDX = short.MaxValue;

                //LeftButtons
                if (Keyboard[(Key)Config.FakeJoyCon.Left.StickButton]) CurrentButton |= HidControllerButtons.KEY_LSTICK;
                if (Keyboard[(Key)Config.FakeJoyCon.Left.DPadUp]) CurrentButton |= HidControllerButtons.KEY_DUP;
                if (Keyboard[(Key)Config.FakeJoyCon.Left.DPadDown]) CurrentButton |= HidControllerButtons.KEY_DDOWN;
                if (Keyboard[(Key)Config.FakeJoyCon.Left.DPadLeft]) CurrentButton |= HidControllerButtons.KEY_DLEFT;
                if (Keyboard[(Key)Config.FakeJoyCon.Left.DPadRight]) CurrentButton |= HidControllerButtons.KEY_DRIGHT;
                if (Keyboard[(Key)Config.FakeJoyCon.Left.ButtonMinus]) CurrentButton |= HidControllerButtons.KEY_MINUS;
                if (Keyboard[(Key)Config.FakeJoyCon.Left.ButtonL]) CurrentButton |= HidControllerButtons.KEY_L;
                if (Keyboard[(Key)Config.FakeJoyCon.Left.ButtonZL]) CurrentButton |= HidControllerButtons.KEY_ZL;

                //RightJoystick
                if (Keyboard[(Key)Config.FakeJoyCon.Right.StickUp]) RightJoystickDY = short.MaxValue;
                if (Keyboard[(Key)Config.FakeJoyCon.Right.StickDown]) RightJoystickDY = -short.MaxValue;
                if (Keyboard[(Key)Config.FakeJoyCon.Right.StickLeft]) RightJoystickDX = -short.MaxValue;
                if (Keyboard[(Key)Config.FakeJoyCon.Right.StickRight]) RightJoystickDX = short.MaxValue;

                //RightButtons
                if (Keyboard[(Key)Config.FakeJoyCon.Right.StickButton]) CurrentButton |= HidControllerButtons.KEY_RSTICK;
                if (Keyboard[(Key)Config.FakeJoyCon.Right.ButtonA]) CurrentButton |= HidControllerButtons.KEY_A;
                if (Keyboard[(Key)Config.FakeJoyCon.Right.ButtonB]) CurrentButton |= HidControllerButtons.KEY_B;
                if (Keyboard[(Key)Config.FakeJoyCon.Right.ButtonX]) CurrentButton |= HidControllerButtons.KEY_X;
                if (Keyboard[(Key)Config.FakeJoyCon.Right.ButtonY]) CurrentButton |= HidControllerButtons.KEY_Y;
                if (Keyboard[(Key)Config.FakeJoyCon.Right.ButtonPlus]) CurrentButton |= HidControllerButtons.KEY_PLUS;
                if (Keyboard[(Key)Config.FakeJoyCon.Right.ButtonR]) CurrentButton |= HidControllerButtons.KEY_R;
                if (Keyboard[(Key)Config.FakeJoyCon.Right.ButtonZR]) CurrentButton |= HidControllerButtons.KEY_ZR;

                LeftJoystick = new HidJoystickPosition
                {
                    DX = LeftJoystickDX,
                    DY = LeftJoystickDY
                };

                RightJoystick = new HidJoystickPosition
                {
                    DX = RightJoystickDX,
                    DY = RightJoystickDY
                };

                bool HasTouch = false;

                //Get screen touch position from left mouse click
                //OpenTK always captures mouse events, even if out of focus, so check if window is focused.
                if (Focused && Mouse?.GetState().LeftButton == ButtonState.Pressed)
                {
                    int ScrnWidth = Width;
                    int ScrnHeight = Height;

                    if (Width > Height * TouchScreenRatioX)
                    {
                        ScrnWidth = (int)(Height * TouchScreenRatioX);
                    }
                    else
                    {
                        ScrnHeight = (int)(Width * TouchScreenRatioY);
                    }

                    int StartX = (Width - ScrnWidth) >> 1;
                    int StartY = (Height - ScrnHeight) >> 1;

                    int EndX = StartX + ScrnWidth;
                    int EndY = StartY + ScrnHeight;

                    if (Mouse.X >= StartX &&
                        Mouse.Y >= StartY &&
                        Mouse.X < EndX &&
                        Mouse.Y < EndY)
                    {
                        int ScrnMouseX = Mouse.X - StartX;
                        int ScrnMouseY = Mouse.Y - StartY;

                        int MX = (int)(((float)ScrnMouseX / ScrnWidth) * TouchScreenWidth);
                        int MY = (int)(((float)ScrnMouseY / ScrnHeight) * TouchScreenHeight);

                        HidTouchPoint CurrentPoint = new HidTouchPoint
                        {
                            X = MX,
                            Y = MY,

                            //Placeholder values till more data is acquired
                            DiameterX = 10,
                            DiameterY = 10,
                            Angle = 90
                        };

                        HasTouch = true;

                        Ns.Hid.SetTouchPoints(CurrentPoint);
                    }
                }

                if (!HasTouch)
                {
                    Ns.Hid.SetTouchPoints();
                }

                Ns.Hid.SetJoyconButton(
                    HidControllerId.CONTROLLER_HANDHELD,
                    HidControllerLayouts.Handheld_Joined,
                    CurrentButton,
                    LeftJoystick,
                    RightJoystick);

                Ns.Hid.SetJoyconButton(
                    HidControllerId.CONTROLLER_HANDHELD,
                    HidControllerLayouts.Main,
                    CurrentButton,
                    LeftJoystick,
                    RightJoystick);
            }
        }

        private void RenderUI()
        {
            if (ShowUI)
            {
                ImGui.SetNextWindowPos(System.Numerics.Vector2.Zero, Condition.Always,
                    System.Numerics.Vector2.Zero);
                ImGui.SetNextWindowSize(new System.Numerics.Vector2(Width, Height),Condition.Always);
                if (ImGui.BeginWindow("MainWindow",ref ShowUI, WindowFlags.NoTitleBar
                    | WindowFlags.NoMove | WindowFlags.AlwaysAutoResize))
                {
                    if(ImGui.BeginChildFrame(0, new System.Numerics.Vector2(-1,-1), 
                        WindowFlags.AlwaysAutoResize))
                    {
                        ImGuiNative.igBeginGroup();
                        if(ImGui.Button("Load Package", new System.Numerics.Vector2(Values.ButtonWidth,
                            Values.ButtonHeight))){
                            ShowFileDialog = true;
                        }
                        ImGuiNative.igEndGroup();
                        ImGui.SameLine();

                        if(ImGui.BeginChildFrame(1, ImGui.GetContentRegionAvailable(),
                            WindowFlags.AlwaysAutoResize))
                        {
                            if (ShowFileDialog)
                            {
                                string output = CurrentPath;
                                if (FileDialog.Draw(ref output, false))
                                {
                                    if (!string.IsNullOrWhiteSpace(output))
                                    {
                                        PackagePath = output;
                                        ShowFileDialog = false;
                                        LoadPackage(PackagePath);
                                    }
                                }
                            }
                            ImGui.EndChildFrame();
                        }
                        ImGui.EndChildFrame();
                    }
                    ImGui.EndWindow();
                }                
            }
        }

        public void LoadPackage(string path)
        {
            if (Directory.Exists(path))
            {
                string[] RomFsFiles = Directory.GetFiles(path, "*.istorage");

                if (RomFsFiles.Length == 0)
                {
                    RomFsFiles = Directory.GetFiles(path, "*.romfs");
                }

                if (RomFsFiles.Length > 0)
                {
                    Console.WriteLine("Loading as cart with RomFS.");

                    Ns.LoadCart(path, RomFsFiles[0]);
                }
                else
                {
                    Console.WriteLine("Loading as cart WITHOUT RomFS.");

                    Ns.LoadCart(path);
                }
            }
            else if (File.Exists(path))
            {
                Console.WriteLine("Loading as homebrew.");

                Ns.LoadProgram(path);
            }
            IsRunning = true;
            ShowUI = false;
        }

    }
}

using System.Threading;

namespace Ryujinx.Profiler.UI
{
    public class ProfileWindowManager
    {
        private ProfileWindow Window;
        private Thread ProfileThread;

        public ProfileWindowManager()
        {
            if (Profile.ProfilingEnabled())
            {
                ProfileThread = new Thread(() =>
                {
                    Window = new ProfileWindow();
                    Window.Run(60, 60);
                });
                ProfileThread.Start();
            }
        }

        public void ToggleVisible()
        {
            if (Profile.ProfilingEnabled())
            {
                Window.ToggleVisible();
            }
        }

        public void Close()
        {
            if (Window != null)
            {
                Window.Close();
                Window.Dispose();
            }

            ProfileThread.Join();

            Window = null;
        }
    }
}

using System.Threading;

namespace Ryujinx.Profiler.UI
{
    public class ProfileWindowManager
    {
        private ProfileWindow window;

        public ProfileWindowManager()
        {
            if (Profile.ProfilingEnabled())
            {
                Thread profileThread = new Thread(() =>
                {
                    window = new ProfileWindow();
                    window.Run(60, 60);
                });
                profileThread.Start();
            }
        }

        public void ToggleVisible()
        {
            if (Profile.ProfilingEnabled())
            {
                window.ToggleVisible();
            }
        }
    }
}

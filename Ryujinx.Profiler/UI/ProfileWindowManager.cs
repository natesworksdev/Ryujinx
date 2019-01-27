using System.Threading;

namespace Ryujinx.Profiler.UI
{
    public class ProfileWindowManager
    {
        private ProfileWindow _window;
        private Thread _profileThread;

        public ProfileWindowManager()
        {
            if (Profile.ProfilingEnabled())
            {
                _profileThread = new Thread(() =>
                {
                    _window = new ProfileWindow();
                    _window.Run(60, 60);
                });
                _profileThread.Start();
            }
        }

        public void ToggleVisible()
        {
            if (Profile.ProfilingEnabled())
            {
                _window.ToggleVisible();
            }
        }

        public void Close()
        {
            if (_window != null)
            {
                _window.Close();
                _window.Dispose();
            }

            _profileThread.Join();

            _window = null;
        }
    }
}

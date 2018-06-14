using System;

namespace Ryujinx.UI
{
    class Program
    {
        static void Main(string[] args)
        {
            MainUI mainUI = new MainUI();
            mainUI.Run(60.0, 60.0);

            Environment.Exit(0);
        }
    }
}

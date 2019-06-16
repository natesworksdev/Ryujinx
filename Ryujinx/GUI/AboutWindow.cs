using Gtk;
using GUI = Gtk.Builder.ObjectAttribute;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Ryujinx
{
    public class AboutWindow : Window
    {
        [GUI] Window AboutWin;
        [GUI] Image  RyujinxLogo;
        [GUI] Image  PatreonLogo;
        [GUI] Image  GitHubLogo;
        [GUI] Image  DiscordLogo;
        [GUI] Image  TwitterLogo;

        public AboutWindow() : this(new Builder("Ryujinx.GUI.AboutWindow.glade")) { }

        private AboutWindow(Builder builder) : base(builder.GetObject("AboutWin").Handle)
        {
            builder.Autoconnect(this);
            AboutWin.Icon      = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.GUI.assets.ryujinxIcon.png");
            RyujinxLogo.Pixbuf = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.GUI.assets.ryujinxIcon.png", 220, 220);
            PatreonLogo.Pixbuf = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.GUI.assets.PatreonLogo.png", 30 , 30 );
            GitHubLogo.Pixbuf  = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.GUI.assets.GitHubLogo.png" , 30 , 30 );
            DiscordLogo.Pixbuf = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.GUI.assets.DiscordLogo.png", 30 , 30 );
            TwitterLogo.Pixbuf = new Gdk.Pixbuf(Assembly.GetExecutingAssembly(), "Ryujinx.GUI.assets.TwitterLogo.png", 30 , 30 );
        }

        public void OpenUrl(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}"));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }

        //Events
        private void RyujinxButton_Pressed(object obj, ButtonPressEventArgs args)
        {
            OpenUrl("https://ryujinx.org");
        }

        private void PatreonButton_Pressed(object obj, ButtonPressEventArgs args)
        {
            OpenUrl("https://www.patreon.com/ryujinx");
        }

        private void GitHubButton_Pressed(object obj, ButtonPressEventArgs args)
        {
            OpenUrl("https://github.com/Ryujinx/Ryujinx");
        }

        private void DiscordButton_Pressed(object obj, ButtonPressEventArgs args)
        {
            OpenUrl("https://discordapp.com/invite/N2FmfVc");
        }

        private void TwitterButton_Pressed(object obj, ButtonPressEventArgs args)
        {
            OpenUrl("https://twitter.com/RyujinxEmu");
        }

        private void ContributersButton_Pressed(object obj, ButtonPressEventArgs args)
        {
            OpenUrl("https://github.com/Ryujinx/Ryujinx/graphs/contributors");
        }

        private void CloseToggle_Activated(object obj, EventArgs args)
        {
            Destroy();
        }
    }
}

using Ryujinx.HLE.HOS.Applets;

namespace Ryujinx.HLE
{
    public interface IHostUiHandler
    {
        /// <summary>
        /// Displays an Input Dialog box to the user and blocks until text is entered
        /// </summary>
        /// <returns>A string representing what's typed on the keyboard</returns>
        string DisplayInputDialog(SoftwareKeyboardUiArgs args);
    }
}
using System;
using System.Threading;
namespace Ryujinx.Common.HostUiBridge
{
    public interface IHostUiHandler
    {
        /// <summary>
        /// Displays an Input Dialog box to the user and blocks until text is entered.
        /// </summary>
        /// <param name="userText">Text that the user entered. Set to `null` on internal errors</param>
        /// <returns>True when OK is pressed, False otherwise. Also returns True on internal errors</returns>
        bool DisplayInputDialog(SoftwareKeyboardUiArgs args, out string userText);

        /// <summary>
        /// Displays a Message Dialog box to the user and blocks until it is closed.
        /// </summary>
        /// <returns>True when OK is pressed, False otherwise.</returns>
        bool DisplayMessageDialog(string title, string message);

        /// <summary>
        /// Displays a Message Dialog box specific to Controller Applet and blocks until it is closed.
        /// </summary>
        /// <returns>True when OK is pressed, False otherwise.</returns>
        bool DisplayMessageDialog(ControllerAppletUiArgs args);

        /// <summary>
        /// Tell the UI that we need to transition to another program.
        /// </summary>
        void ProgramChange();

        /// <summary>
        /// Displays a Message Dialog box specific to Error Applet and blocks until it is closed.
        /// </summary>
        /// <returns>False when OK is pressed, True when another button (Details) is pressed.</returns>
        bool DisplayErrorAppletDialog(string title, string message, string[] buttonsText);

        /// <summary>
        /// Tells the UI to start showing a progress report (such as a loading bar).
        /// </summary>
        /// <param name="name">The name of the status being reported.</param>
        /// <param name="queryFunction">The GUI polls this function every refreshRateMs for current progress.</param>
        /// <param name="stopEvent">This event is signaled by the caller after it has completed its task.</param>
        /// <param name="refreshRateMs">The polling rate for progress</param>
        void StartProgressReport(ProgressReportType type, Func<(int Value, int Total)> queryFunction, AutoResetEvent stopEvent, int refreshRateMs);
    }
}
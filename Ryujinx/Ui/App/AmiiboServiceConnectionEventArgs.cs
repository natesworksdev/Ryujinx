using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ryujinx.Ui.App
{
    public class AmiiboServiceConnectionEventArgs : EventArgs
    {
        private const string OfflineMessage = "Unable to connect to the Amiibo API server. The Amiibo service will run in offline mode until a connection is made.";
        private const string OnlineMessage  = "Successfully connected to the Amiibo API server.";

        private string _message;
        private bool   _online;

        public string Message  { get => _message; }
        public bool   IsOnline { get => _online; }

        public AmiiboServiceConnectionEventArgs(bool online)
        {
            this._online = online;
            this._message = online ? OnlineMessage : OfflineMessage;
        }
    }
}

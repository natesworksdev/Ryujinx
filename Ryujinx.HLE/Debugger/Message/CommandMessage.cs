namespace Ryujinx.HLE.Debugger
{
    struct CommandMessage : IMessage
    {
        public string Command;

        public CommandMessage(string cmd)
        {
            Command = cmd;
        }
    }
}

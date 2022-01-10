namespace Ryujinx.HLE.HOS.Services.Sockets.Bsd
{
    class PollEvent
    {
        public PollEventData Data;
        public IBsdSocket Socket { get; }

        public PollEvent(PollEventData data, IBsdSocket socket)
        {
            Data = data;
            Socket = socket;
        }
    }
}
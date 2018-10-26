namespace Ryujinx.HLE.HOS.Services.Nv
{
    internal class NvFd
    {
        public string Name { get; private set; }

        public NvFd(string name)
        {
            this.Name = name;
        }
    }
}
namespace Ryujinx.HLE.HOS.Services.Vi
{
    internal class Display
    {
        public string Name { get; private set; }

        public Display(string name)
        {
            this.Name = name;
        }
    }
}
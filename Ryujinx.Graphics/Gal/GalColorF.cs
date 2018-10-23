namespace Ryujinx.Graphics.Gal
{
    public struct GalColorF
    {
        public float Red   { get; private set; }
        public float Green { get; private set; }
        public float Blue  { get; private set; }
        public float Alpha { get; private set; }

        public GalColorF(
            float red,
            float green,
            float blue,
            float alpha)
        {
            this.Red   = red;
            this.Green = green;
            this.Blue  = blue;
            this.Alpha = alpha;
        }
    }
}
namespace Ryujinx.HLE.HOS.Tamper.Atmosphere
{
    internal class Parameter<T>
    {
        public T Value { get; set; }

        public Parameter(T value)
        {
            Value = value;
        }
    }
}

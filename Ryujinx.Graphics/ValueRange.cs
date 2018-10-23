namespace Ryujinx.Graphics
{
    struct ValueRange<T>
    {
        public long Start { get; private set; }
        public long End   { get; private set; }

        public T Value { get; set; }

        public ValueRange(long start, long end, T value = default(T))
        {
            this.Start = start;
            this.End   = end;
            this.Value = value;
        }
    }
}
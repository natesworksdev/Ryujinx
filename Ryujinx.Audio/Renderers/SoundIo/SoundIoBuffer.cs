namespace Ryujinx.Audio.SoundIo
{
    /// <summary>
    /// Represents the remaining bytes left buffered for a specific buffer tag
    /// </summary>
    internal class SoundIoBuffer
    {
        /// <summary>
        /// The buffer tag this <see cref="SoundIoBuffer"/> represents
        /// </summary>
        public long Tag { get; private set; }

        /// <summary>
        /// The remaining bytes still to be released
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// Constructs a new instance of a <see cref="SoundIoBuffer"/>
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="length"></param>
        public SoundIoBuffer(long tag, int length)
        {
            Tag = tag;
            Length = length;
        }
    }
}

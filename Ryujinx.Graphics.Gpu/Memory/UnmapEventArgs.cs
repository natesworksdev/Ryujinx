namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// Memory unmap or remap event arguments.
    /// </summary>
    public class UnmapEventArgs
    {
        /// <summary>
        /// Address of the region being unmapped.
        /// </summary>
        public ulong Address { get; }

        /// <summary>
        /// Size in bytes of the region being unmapped.
        /// </summary>
        public ulong Size { get; }

        /// <summary>
        /// Creates a new instance of the unmap event arguments.
        /// </summary>
        /// <param name="address">Address of the region being unmapped</param>
        /// <param name="size">Size in bytes of the region being unmapped</param>
        public UnmapEventArgs(ulong address, ulong size)
        {
            Address = address;
            Size = size;
        }
    }
}

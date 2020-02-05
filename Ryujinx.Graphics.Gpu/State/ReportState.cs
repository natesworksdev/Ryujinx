namespace Ryujinx.Graphics.Gpu.State
{
    /// <summary>
    /// GPU counter report state.
    /// </summary>
    struct ReportState
    {
        public GpuVa Address;
#pragma warning disable CS0649
        public int   Payload;
        public uint  Control;
#pragma warning restore CS0649
    }
}

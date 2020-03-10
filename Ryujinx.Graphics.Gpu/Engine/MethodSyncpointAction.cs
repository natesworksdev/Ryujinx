using Ryujinx.Graphics.Gpu.State;

namespace Ryujinx.Graphics.Gpu.Engine
{
    partial class Methods
    {
        /// <summary>
        /// Performs actions on a syncpoint.
        /// </summary>
        /// <param name="state">Current GPU state</param>
        /// <param name="argument">Method call argument</param>
        public void SyncpointAction(GpuState state, int argument)
        {
            uint syncpointId = (uint)(argument) & 0xFFFF;

            _context.Synchronization.IncrementSyncpoint(syncpointId);
        }
    }
}

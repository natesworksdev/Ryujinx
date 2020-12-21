using System;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 8)]
    struct NodeLatestUpdate
    {
        public NodeLatestUpdateFlags State;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public byte[] Reserved;
    }

    static class NodeLatestUpdateHelper
    {
        public static void CalculateLatestUpdate(this NodeLatestUpdate[] array, NodeInfo[] beforeNodes, NodeInfo[] afterNodes)
        {
            if (beforeNodes == null)
            {
                // If there is no initial state, do not flag anyone as connected. (they are assumed to be connected before we joined)
                return;
            }

            lock (array)
            {
                for (int i = 0; i < 8; i++)
                {
                    NodeInfo before = beforeNodes == null ? new NodeInfo() : beforeNodes[i];
                    NodeInfo after = afterNodes == null ? new NodeInfo() : afterNodes[i];

                    if (before.IsConnected == 0)
                    {
                        if (after.IsConnected != 0)
                        {
                            array[i].State |= NodeLatestUpdateFlags.Connect;
                        }
                    }
                    else
                    {
                        if (after.IsConnected == 0)
                        {
                            array[i].State |= NodeLatestUpdateFlags.Disconnect;
                        }
                    }
                }
            }
        }

        public static NodeLatestUpdate[] ConsumeLatestUpdate(this NodeLatestUpdate[] array, int number)
        {
            NodeLatestUpdate[] result = new NodeLatestUpdate[number];

            lock (array)
            {
                for (int i = 0; i < number; i++)
                {
                    result[i].Reserved = new byte[7];

                    if (i < 8)
                    {
                        result[i].State = array[i].State;
                        array[i].State = NodeLatestUpdateFlags.None;
                    }
                }
            }

            return result;
        }
    }
}

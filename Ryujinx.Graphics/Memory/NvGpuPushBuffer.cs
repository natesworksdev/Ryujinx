using System.Collections.Generic;
using System.IO;

namespace Ryujinx.Graphics.Memory
{
    public static class NvGpuPushBuffer
    {
        private enum SubmissionMode
        {
            Incrementing    = 1,
            NonIncrementing = 3,
            Immediate       = 4,
            IncrementOnce   = 5
        }

        public static NvGpuPbEntry[] Decode(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);

                List<NvGpuPbEntry> pushBuffer = new List<NvGpuPbEntry>();

                bool CanRead() => ms.Position + 4 <= ms.Length;

                while (CanRead())
                {
                    int packed = reader.ReadInt32();

                    int meth = (packed >> 0)  & 0x1fff;
                    int subC = (packed >> 13) & 7;
                    int args = (packed >> 16) & 0x1fff;
                    int mode = (packed >> 29) & 7;

                    switch ((SubmissionMode)mode)
                    {
                        case SubmissionMode.Incrementing:
                        {
                            for (int index = 0; index < args && CanRead(); index++, meth++)
                            {
                                pushBuffer.Add(new NvGpuPbEntry(meth, subC, reader.ReadInt32()));
                            }

                            break;
                        }

                        case SubmissionMode.NonIncrementing:
                        {
                            int[] arguments = new int[args];

                            for (int index = 0; index < arguments.Length; index++)
                            {
                                if (!CanRead())
                                {
                                    break;
                                }

                                arguments[index] = reader.ReadInt32();
                            }

                            pushBuffer.Add(new NvGpuPbEntry(meth, subC, arguments));

                            break;
                        }

                        case SubmissionMode.Immediate:
                        {
                            pushBuffer.Add(new NvGpuPbEntry(meth, subC, args));

                            break;
                        }

                        case SubmissionMode.IncrementOnce:
                        {
                            if (CanRead())
                            {
                                pushBuffer.Add(new NvGpuPbEntry(meth, subC, reader.ReadInt32()));
                            }

                            if (CanRead() && args > 1)
                            {
                                int[] arguments = new int[args - 1];

                                for (int index = 0; index < arguments.Length && CanRead(); index++)
                                {
                                    arguments[index] = reader.ReadInt32();
                                }

                                pushBuffer.Add(new NvGpuPbEntry(meth + 1, subC, arguments));
                            }

                            break;
                        }
                    }
                }

                return pushBuffer.ToArray();
            }
        }
    }
}
using Ryujinx.Common;
using System;
using System.Runtime.CompilerServices;

namespace Ryujinx.Horizon.Sdk.Sf
{
    static class RawDataOffsetCalculator
    {
        public static int[] Calculate(Type[] types)
        {
            int[] offsets = new int[types.Length + 1];

            if (types.Length != 0)
            {
                int argsCount = types.Length;

                int[] sizes = new int[argsCount];
                int[] aligns = new int[argsCount];
                int[] map = new int[argsCount];

                for (int i = 0; i < argsCount; i++)
                {
                    var argType = CommandSerialization.GetInnerType(types[i]);

                    sizes[i] = SizeOf(argType);
                    aligns[i] = !argType.IsPrimitive ? argType.StructLayoutAttribute.Pack : sizes[i];
                    map[i] = i;
                }

                for (int i = 1; i < argsCount; i++)
                {
                    for (int j = i; j > 0 && aligns[map[j - 1]] > aligns[map[j]]; j--)
                    {
                        var temp = map[j - 1];
                        map[j - 1] = map[j];
                        map[j] = temp;
                    }
                }

                int offset = 0;

                foreach (int i in map)
                {
                    offset = BitUtils.AlignUp(offset, aligns[i]);
                    offsets[i] = offset;
                    offset += sizes[i];
                }

                offsets[argsCount] = offset;
            }

            return offsets;
        }

        private static int SizeOf(Type type)
        {
            return (int)GenericMethod.Invoke(typeof(Unsafe).GetMethod(nameof(Unsafe.SizeOf)), new Type[] { type });
        }
    }
}

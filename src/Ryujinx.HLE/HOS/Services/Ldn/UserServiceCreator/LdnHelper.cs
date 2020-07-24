using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator
{
    static class LdnHelper
    {
        public static byte[] StructureToByteArray(object obj, int padding = 0)
        {
            int len = Marshal.SizeOf(obj);
            byte[] arr = new byte[len + padding];
            IntPtr ptr = Marshal.AllocHGlobal(len);

            Marshal.StructureToPtr(obj, ptr, true);
            Marshal.Copy(ptr, arr, 0, len);
            Marshal.FreeHGlobal(ptr);

            return arr;
        }

        public static byte[] StructureToByteArrayWithData(object obj, byte[] data)
        {
            byte[] result = StructureToByteArray(obj, data.Length);

            Array.Copy(data, 0, result, result.Length - data.Length, data.Length);

            return result;
        }

        public static T FromBytes<T>(byte[] arr)
        {
            T str = default(T);

            int size = Marshal.SizeOf(str);
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(arr, 0, ptr, size);

            str = (T)Marshal.PtrToStructure(ptr, str.GetType());
            Marshal.FreeHGlobal(ptr);

            return str;
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
    }
}

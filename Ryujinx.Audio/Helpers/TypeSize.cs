using System;
using System.Reflection.Emit;

namespace Ryujinx.Audio
{
    /// <summary>
    /// Helper to determine the byte size of a type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class TypeSize<T>
    {
        /// <summary>
        /// The byte size of type <see cref="T"/>
        /// </summary>
        public readonly static int Size;

        static TypeSize()
        {
            var dm = new DynamicMethod("", typeof(int), Type.EmptyTypes);
            var il = dm.GetILGenerator();

            il.Emit(OpCodes.Sizeof, typeof(T));
            il.Emit(OpCodes.Ret);

            Size = (int)dm.Invoke(null, null);
        }
    }
}

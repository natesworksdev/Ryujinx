using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Ryujinx.Common.Extensions
{
    public static class NumericExtensions
    {
        #region Private methods because the JIT is weird at optimizing

        #region Boolean Methods
        // As per https://github.com/dotnet/runtime/issues/35159
        // The JIT, for whatever reason, generates significantly better code
        // if you call a separate helper method with the *same* code.
        // This will hopefully be fixed at some point.
        private static class BoolMethods
        {
            [MethodImpl(MethodOptions.FastInline)]
            internal static unsafe bool AsBool([Range(0, 1)] byte value) => *(bool*)&value;
            [MethodImpl(MethodOptions.FastInline)]
            internal static unsafe bool AsBool([Range(0, 1)] int value) => *(bool*)&value;
            [MethodImpl(MethodOptions.FastInline)]
            internal static unsafe bool AsBool([Range(0, 1)] uint value) => *(bool*)&value;

            [MethodImpl(MethodOptions.FastInline)]
            internal static unsafe byte AsByte(bool value) => *(byte*)&value;
            [MethodImpl(MethodOptions.FastInline)]
            internal static unsafe int AsInt(bool value) => *(byte*)&value;
            [MethodImpl(MethodOptions.FastInline)]
            internal static unsafe uint AsUInt(bool value) => *(byte*)&value;
        }
        #endregion

        #endregion

        #region Boolean Conversions

        /// <summary>Converts a given value which is 0 or 1 into false or true, respectively.
        ///    Behavior is undefined if the value is not 0 or 1.
        /// </summary>
        /// <returns>false or true, given 0 or 1, respectively.</returns>
        [MethodImpl(MethodOptions.FastInline)]
        public static bool AsBool([Range(0, 1)] this byte value) => BoolMethods.AsBool(value);

        /// <summary>Converts a given value which is 0 or 1 into false or true, respectively.
        ///    Behavior is undefined if the value is not 0 or 1.
        /// </summary>
        /// <returns>false or true, given 0 or 1, respectively.</returns>
        [MethodImpl(MethodOptions.FastInline)]
        public static bool AsBool([Range(0, 1)] this int value) => BoolMethods.AsBool(value);

        /// <summary>Converts a given value which is 0 or 1 into false or true, respectively.
        ///    Behavior is undefined if the value is not 0 or 1.
        /// </summary>
        /// <returns>false or true, given 0 or 1, respectively.</returns>
        [MethodImpl(MethodOptions.FastInline)]
        public static bool AsBool([Range(0, 1)] this uint value) => BoolMethods.AsBool(value);

        /// <summary>Converts a given value which is false or true into 0 or 1, respectively.</summary>
        /// <returns>0 or 1, given false or true, respectively.</returns>
        [MethodImpl(MethodOptions.FastInline)]
        public static byte AsByte(this bool value) => BoolMethods.AsByte(value);

        /// <summary>Converts a given value which is false or true into 0 or 1, respectively.</summary>
        /// <returns>0 or 1, given false or true, respectively.</returns>
        [MethodImpl(MethodOptions.FastInline)]
        public static int AsInt(this bool value) => BoolMethods.AsInt(value);

        /// <summary>Converts a given value which is false or true into 0 or 1, respectively.</summary>
        /// <returns>0 or 1, given false or true, respectively.</returns>
        [MethodImpl(MethodOptions.FastInline)]
        public static uint AsUInt(this bool value) => BoolMethods.AsUInt(value);

        #endregion
    }
}

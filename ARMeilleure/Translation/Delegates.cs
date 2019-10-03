using ARMeilleure.Instructions;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ARMeilleure.Translation
{
    static class Delegates
    {
        public static bool TryGetDelegateFuncPtr(string key, out IntPtr funcPtr)
        {
            if (key == null)
            {
                throw new ArgumentNullException();
            }

            if (_nativeInterfaceDelegates.TryGetValue(key, out DelegateInfo dlgInfo))
            {
                funcPtr = dlgInfo.FuncPtr;

                return true;
            }
            else if (_softFallbackDelegates.TryGetValue(key, out dlgInfo))
            {
                funcPtr = dlgInfo.FuncPtr;

                return true;
            }
            else if (_mathDelegates.TryGetValue(key, out dlgInfo))
            {
                funcPtr = dlgInfo.FuncPtr;

                return true;
            }
            else if (_softFloatDelegates.TryGetValue(key, out dlgInfo))
            {
                funcPtr = dlgInfo.FuncPtr;

                return true;
            }
            else
            {
                funcPtr = default;

                return false;
            }
        }

        public static DelegateInfo GetMathDelegateInfo(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException();
            }

            if (!_mathDelegates.TryGetValue(key, out DelegateInfo dlgInfo))
            {
                throw new Exception();
            }

            return dlgInfo;
        }

        public static DelegateInfo GetNativeInterfaceDelegateInfo(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException();
            }

            if (!_nativeInterfaceDelegates.TryGetValue(key, out DelegateInfo dlgInfo))
            {
                throw new Exception();
            }

            return dlgInfo;
        }

        public static DelegateInfo GetSoftFallbackDelegateInfo(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException();
            }

            if (!_softFallbackDelegates.TryGetValue(key, out DelegateInfo dlgInfo))
            {
                throw new Exception();
            }

            return dlgInfo;
        }

        public static DelegateInfo GetSoftFloatDelegateInfo(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException();
            }

            if (!_softFloatDelegates.TryGetValue(key, out DelegateInfo dlgInfo))
            {
                throw new Exception();
            }

            return dlgInfo;
        }

        private static void SetMathDelegateInfo(Type type, string name, Type[] types)
        {
            Delegate dlg = DelegateHelpers.GetDelegate(type, name, types);

            if (!_mathDelegates.TryAdd(GetKey(dlg.Method), new DelegateInfo(dlg)))
            {
                throw new Exception();
            }
        }

        private static void SetNativeInterfaceDelegateInfo(string name)
        {
            Delegate dlg = DelegateHelpers.GetDelegate(typeof(NativeInterface), name);

            if (!_nativeInterfaceDelegates.TryAdd(GetKey(dlg.Method), new DelegateInfo(dlg)))
            {
                throw new Exception();
            }
        }

        private static void SetSoftFallbackDelegateInfo(string name)
        {
            Delegate dlg = DelegateHelpers.GetDelegate(typeof(SoftFallback), name);

            if (!_softFallbackDelegates.TryAdd(GetKey(dlg.Method), new DelegateInfo(dlg)))
            {
                throw new Exception();
            }
        }

        private static void SetSoftFloatDelegateInfo(Type type, string name)
        {
            Delegate dlg = DelegateHelpers.GetDelegate(type, name);

            if (!_softFloatDelegates.TryAdd(GetKey(dlg.Method), new DelegateInfo(dlg)))
            {
                throw new Exception();
            }
        }

        private static string GetKey(MethodInfo info)
        {
            return $"{info.DeclaringType.Name}.{info.Name}";
        }

        private static readonly Dictionary<string, DelegateInfo> _mathDelegates;
        private static readonly Dictionary<string, DelegateInfo> _nativeInterfaceDelegates;
        private static readonly Dictionary<string, DelegateInfo> _softFallbackDelegates;
        private static readonly Dictionary<string, DelegateInfo> _softFloatDelegates;

        static Delegates()
        {
            _mathDelegates            = new Dictionary<string, DelegateInfo>();
            _nativeInterfaceDelegates = new Dictionary<string, DelegateInfo>();
            _softFallbackDelegates    = new Dictionary<string, DelegateInfo>();
            _softFloatDelegates       = new Dictionary<string, DelegateInfo>();

            SetMathDelegateInfo(typeof(Math), nameof(Math.Abs),      new Type[] { typeof(double) });
            SetMathDelegateInfo(typeof(Math), nameof(Math.Ceiling),  new Type[] { typeof(double) });
            SetMathDelegateInfo(typeof(Math), nameof(Math.Floor),    new Type[] { typeof(double) });
            SetMathDelegateInfo(typeof(Math), nameof(Math.Round),    new Type[] { typeof(double), typeof(MidpointRounding) });
            SetMathDelegateInfo(typeof(Math), nameof(Math.Truncate), new Type[] { typeof(double) });

            SetMathDelegateInfo(typeof(MathF), nameof(MathF.Abs),      new Type[] { typeof(float) });
            SetMathDelegateInfo(typeof(MathF), nameof(MathF.Ceiling),  new Type[] { typeof(float) });
            SetMathDelegateInfo(typeof(MathF), nameof(MathF.Floor),    new Type[] { typeof(float) });
            SetMathDelegateInfo(typeof(MathF), nameof(MathF.Round),    new Type[] { typeof(float), typeof(MidpointRounding) });
            SetMathDelegateInfo(typeof(MathF), nameof(MathF.Truncate), new Type[] { typeof(float) });

            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.Break));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.CheckSynchronization));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.ClearExclusive));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.GetCntfrqEl0));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.GetCntpctEl0));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.GetCtrEl0));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.GetDczidEl0));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.GetFpcr));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.GetFpsr));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.GetTpidr));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.GetTpidrEl0));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.ReadByte));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.ReadByteExclusive));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.ReadUInt16));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.ReadUInt16Exclusive));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.ReadUInt32));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.ReadUInt32Exclusive));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.ReadUInt64));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.ReadUInt64Exclusive));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.ReadVector128));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.ReadVector128Exclusive));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.SetFpcr));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.SetFpsr));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.SetTpidrEl0));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.SupervisorCall));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.Undefined));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.WriteByte));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.WriteByteExclusive));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.WriteUInt16));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.WriteUInt16Exclusive));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.WriteUInt32));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.WriteUInt32Exclusive));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.WriteUInt64));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.WriteUInt64Exclusive));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.WriteVector128));
            SetNativeInterfaceDelegateInfo(nameof(NativeInterface.WriteVector128Exclusive));

            SetSoftFallbackDelegateInfo(nameof(SoftFallback.BinarySignedSatQAcc));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.BinarySignedSatQAdd));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.BinarySignedSatQSub));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.BinaryUnsignedSatQAcc));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.BinaryUnsignedSatQAdd));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.BinaryUnsignedSatQSub));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.CountLeadingSigns));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.CountLeadingZeros));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.CountSetBits8));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.Crc32b));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.Crc32cb));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.Crc32ch));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.Crc32cw));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.Crc32cx));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.Crc32h));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.Crc32w));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.Crc32x));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.Decrypt));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.Encrypt));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.FixedRotate));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.HashChoose));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.HashLower));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.HashMajority));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.HashParity));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.HashUpper));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.InverseMixColumns));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.MaxS64));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.MaxU64));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.MinS64));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.MinU64));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.MixColumns));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.ReverseBits32));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.ReverseBits64));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.ReverseBits8));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.ReverseBytes16_32));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.ReverseBytes16_64));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.ReverseBytes32_64));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.Round));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.RoundF));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.SatF32ToS32));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.SatF32ToS64));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.SatF32ToU32));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.SatF32ToU64));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.SatF64ToS32));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.SatF64ToS64));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.SatF64ToU32));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.SatF64ToU64));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.Sha1SchedulePart1));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.Sha1SchedulePart2));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.Sha256SchedulePart1));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.Sha256SchedulePart2));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.SignedShlReg));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.SignedShlRegSatQ));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.SignedShrImm64));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.SignedSrcSignedDstSatQ));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.SignedSrcUnsignedDstSatQ));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.Tbl1_V64));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.Tbl1_V128));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.Tbl2_V64));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.Tbl2_V128));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.Tbl3_V64));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.Tbl3_V128));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.Tbl4_V64));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.Tbl4_V128));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.UnarySignedSatQAbsOrNeg));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.UnsignedShlReg));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.UnsignedShlRegSatQ));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.UnsignedShrImm64));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.UnsignedSrcSignedDstSatQ));
            SetSoftFallbackDelegateInfo(nameof(SoftFallback.UnsignedSrcUnsignedDstSatQ));

            SetSoftFloatDelegateInfo(typeof(SoftFloat16_32), nameof(SoftFloat16_32.FPConvert));

            SetSoftFloatDelegateInfo(typeof(SoftFloat32), nameof(SoftFloat32.FPAdd));
            SetSoftFloatDelegateInfo(typeof(SoftFloat32), nameof(SoftFloat32.FPCompare));
            SetSoftFloatDelegateInfo(typeof(SoftFloat32), nameof(SoftFloat32.FPCompareEQ));
            SetSoftFloatDelegateInfo(typeof(SoftFloat32), nameof(SoftFloat32.FPCompareGE));
            SetSoftFloatDelegateInfo(typeof(SoftFloat32), nameof(SoftFloat32.FPCompareGT));
            SetSoftFloatDelegateInfo(typeof(SoftFloat32), nameof(SoftFloat32.FPCompareLE));
            SetSoftFloatDelegateInfo(typeof(SoftFloat32), nameof(SoftFloat32.FPCompareLT));
            SetSoftFloatDelegateInfo(typeof(SoftFloat32), nameof(SoftFloat32.FPDiv));
            SetSoftFloatDelegateInfo(typeof(SoftFloat32), nameof(SoftFloat32.FPMax));
            SetSoftFloatDelegateInfo(typeof(SoftFloat32), nameof(SoftFloat32.FPMaxNum));
            SetSoftFloatDelegateInfo(typeof(SoftFloat32), nameof(SoftFloat32.FPMin));
            SetSoftFloatDelegateInfo(typeof(SoftFloat32), nameof(SoftFloat32.FPMinNum));
            SetSoftFloatDelegateInfo(typeof(SoftFloat32), nameof(SoftFloat32.FPMul));
            SetSoftFloatDelegateInfo(typeof(SoftFloat32), nameof(SoftFloat32.FPMulAdd));
            SetSoftFloatDelegateInfo(typeof(SoftFloat32), nameof(SoftFloat32.FPMulSub));
            SetSoftFloatDelegateInfo(typeof(SoftFloat32), nameof(SoftFloat32.FPMulX));
            SetSoftFloatDelegateInfo(typeof(SoftFloat32), nameof(SoftFloat32.FPRecipEstimate));
            SetSoftFloatDelegateInfo(typeof(SoftFloat32), nameof(SoftFloat32.FPRecipStepFused));
            SetSoftFloatDelegateInfo(typeof(SoftFloat32), nameof(SoftFloat32.FPRecpX));
            SetSoftFloatDelegateInfo(typeof(SoftFloat32), nameof(SoftFloat32.FPRSqrtEstimate));
            SetSoftFloatDelegateInfo(typeof(SoftFloat32), nameof(SoftFloat32.FPRSqrtStepFused));
            SetSoftFloatDelegateInfo(typeof(SoftFloat32), nameof(SoftFloat32.FPSqrt));
            SetSoftFloatDelegateInfo(typeof(SoftFloat32), nameof(SoftFloat32.FPSub));

            SetSoftFloatDelegateInfo(typeof(SoftFloat32_16), nameof(SoftFloat32_16.FPConvert));

            SetSoftFloatDelegateInfo(typeof(SoftFloat64), nameof(SoftFloat64.FPAdd));
            SetSoftFloatDelegateInfo(typeof(SoftFloat64), nameof(SoftFloat64.FPCompare));
            SetSoftFloatDelegateInfo(typeof(SoftFloat64), nameof(SoftFloat64.FPCompareEQ));
            SetSoftFloatDelegateInfo(typeof(SoftFloat64), nameof(SoftFloat64.FPCompareGE));
            SetSoftFloatDelegateInfo(typeof(SoftFloat64), nameof(SoftFloat64.FPCompareGT));
            SetSoftFloatDelegateInfo(typeof(SoftFloat64), nameof(SoftFloat64.FPCompareLE));
            SetSoftFloatDelegateInfo(typeof(SoftFloat64), nameof(SoftFloat64.FPCompareLT));
            SetSoftFloatDelegateInfo(typeof(SoftFloat64), nameof(SoftFloat64.FPDiv));
            SetSoftFloatDelegateInfo(typeof(SoftFloat64), nameof(SoftFloat64.FPMax));
            SetSoftFloatDelegateInfo(typeof(SoftFloat64), nameof(SoftFloat64.FPMaxNum));
            SetSoftFloatDelegateInfo(typeof(SoftFloat64), nameof(SoftFloat64.FPMin));
            SetSoftFloatDelegateInfo(typeof(SoftFloat64), nameof(SoftFloat64.FPMinNum));
            SetSoftFloatDelegateInfo(typeof(SoftFloat64), nameof(SoftFloat64.FPMul));
            SetSoftFloatDelegateInfo(typeof(SoftFloat64), nameof(SoftFloat64.FPMulAdd));
            SetSoftFloatDelegateInfo(typeof(SoftFloat64), nameof(SoftFloat64.FPMulSub));
            SetSoftFloatDelegateInfo(typeof(SoftFloat64), nameof(SoftFloat64.FPMulX));
            SetSoftFloatDelegateInfo(typeof(SoftFloat64), nameof(SoftFloat64.FPRecipEstimate));
            SetSoftFloatDelegateInfo(typeof(SoftFloat64), nameof(SoftFloat64.FPRecipStepFused));
            SetSoftFloatDelegateInfo(typeof(SoftFloat64), nameof(SoftFloat64.FPRecpX));
            SetSoftFloatDelegateInfo(typeof(SoftFloat64), nameof(SoftFloat64.FPRSqrtEstimate));
            SetSoftFloatDelegateInfo(typeof(SoftFloat64), nameof(SoftFloat64.FPRSqrtStepFused));
            SetSoftFloatDelegateInfo(typeof(SoftFloat64), nameof(SoftFloat64.FPSqrt));
            SetSoftFloatDelegateInfo(typeof(SoftFloat64), nameof(SoftFloat64.FPSub));
        }
    }
}

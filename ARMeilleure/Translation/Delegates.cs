using ARMeilleure.Instructions;
using ARMeilleure.State;
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
                funcPtr = default(IntPtr);

                return false;
            }
        }

        public static DelegateInfo GetMathDelegateInfo(string key)
        {
            if (key != null && _mathDelegates.TryGetValue(key, out DelegateInfo dlgInfo))
            {
                return dlgInfo;
            }

            throw new Exception();
        }

        public static DelegateInfo GetNativeInterfaceDelegateInfo(string key)
        {
            if (key != null && _nativeInterfaceDelegates.TryGetValue(key, out DelegateInfo dlgInfo))
            {
                return dlgInfo;
            }

            throw new Exception();
        }

        public static DelegateInfo GetSoftFallbackDelegateInfo(string key)
        {
            if (key != null && _softFallbackDelegates.TryGetValue(key, out DelegateInfo dlgInfo))
            {
                return dlgInfo;
            }

            throw new Exception();
        }

        public static DelegateInfo GetSoftFloatDelegateInfo(string key)
        {
            if (key != null && _softFloatDelegates.TryGetValue(key, out DelegateInfo dlgInfo))
            {
                return dlgInfo;
            }

            throw new Exception();
        }

        private static void SetMathDelegateInfo(Delegate dlg)
        {
            if (dlg == null || !_mathDelegates.TryAdd(GetKey(dlg.Method), new DelegateInfo(dlg)))
            {
                throw new Exception();
            }
        }

        private static void SetNativeInterfaceDelegateInfo(Delegate dlg)
        {
            if (dlg == null || !_nativeInterfaceDelegates.TryAdd(GetKey(dlg.Method), new DelegateInfo(dlg)))
            {
                throw new Exception();
            }
        }

        private static void SetSoftFallbackDelegateInfo(Delegate dlg)
        {
            if (dlg == null || !_softFallbackDelegates.TryAdd(GetKey(dlg.Method), new DelegateInfo(dlg)))
            {
                throw new Exception();
            }
        }

        private static void SetSoftFloatDelegateInfo(Delegate dlg)
        {
            if (dlg == null || !_softFloatDelegates.TryAdd(GetKey(dlg.Method), new DelegateInfo(dlg)))
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

            SetMathDelegateInfo(new _F64_F64                 (Math.Floor));
            SetMathDelegateInfo(new _F64_F64                 (Math.Ceiling));
            SetMathDelegateInfo(new _F64_F64                 (Math.Abs));
            SetMathDelegateInfo(new _F64_F64                 (Math.Truncate));
            SetMathDelegateInfo(new _F64_F64_MidpointRounding(Math.Round));

            SetMathDelegateInfo(new _F32_F32                 (MathF.Floor));
            SetMathDelegateInfo(new _F32_F32                 (MathF.Ceiling));
            SetMathDelegateInfo(new _F32_F32                 (MathF.Abs));
            SetMathDelegateInfo(new _F32_F32                 (MathF.Truncate));
            SetMathDelegateInfo(new _F32_F32_MidpointRounding(MathF.Round));

            SetNativeInterfaceDelegateInfo(new _S32_U64_U8   (NativeInterface.WriteByteExclusive));
            SetNativeInterfaceDelegateInfo(new _S32_U64_U16  (NativeInterface.WriteUInt16Exclusive));
            SetNativeInterfaceDelegateInfo(new _S32_U64_U32  (NativeInterface.WriteUInt32Exclusive));
            SetNativeInterfaceDelegateInfo(new _S32_U64_U64  (NativeInterface.WriteUInt64Exclusive));
            SetNativeInterfaceDelegateInfo(new _S32_U64_V128 (NativeInterface.WriteVector128Exclusive));
            SetNativeInterfaceDelegateInfo(new _U16_U64      (NativeInterface.ReadUInt16Exclusive));
            SetNativeInterfaceDelegateInfo(new _U16_U64      (NativeInterface.ReadUInt16));
            SetNativeInterfaceDelegateInfo(new _U32_U64      (NativeInterface.ReadUInt32Exclusive));
            SetNativeInterfaceDelegateInfo(new _U32_U64      (NativeInterface.ReadUInt32));
            SetNativeInterfaceDelegateInfo(new _U64          (NativeInterface.GetCtrEl0));
            SetNativeInterfaceDelegateInfo(new _U64          (NativeInterface.GetDczidEl0));
            SetNativeInterfaceDelegateInfo(new _U64          (NativeInterface.GetFpcr));
            SetNativeInterfaceDelegateInfo(new _U64          (NativeInterface.GetFpsr));
            SetNativeInterfaceDelegateInfo(new _U64          (NativeInterface.GetTpidrEl0));
            SetNativeInterfaceDelegateInfo(new _U64          (NativeInterface.GetTpidr));
            SetNativeInterfaceDelegateInfo(new _U64          (NativeInterface.GetCntfrqEl0));
            SetNativeInterfaceDelegateInfo(new _U64          (NativeInterface.GetCntpctEl0));
            SetNativeInterfaceDelegateInfo(new _U64_U64      (NativeInterface.ReadUInt64Exclusive));
            SetNativeInterfaceDelegateInfo(new _U64_U64      (NativeInterface.ReadUInt64));
            SetNativeInterfaceDelegateInfo(new _U8_U64       (NativeInterface.ReadByteExclusive));
            SetNativeInterfaceDelegateInfo(new _U8_U64       (NativeInterface.ReadByte));
            SetNativeInterfaceDelegateInfo(new _V128_U64     (NativeInterface.ReadVector128Exclusive));
            SetNativeInterfaceDelegateInfo(new _V128_U64     (NativeInterface.ReadVector128));
            SetNativeInterfaceDelegateInfo(new _Void         (NativeInterface.ClearExclusive));
            SetNativeInterfaceDelegateInfo(new _Void         (NativeInterface.CheckSynchronization));
            SetNativeInterfaceDelegateInfo(new _Void_U64     (NativeInterface.SetFpcr));
            SetNativeInterfaceDelegateInfo(new _Void_U64     (NativeInterface.SetFpsr));
            SetNativeInterfaceDelegateInfo(new _Void_U64     (NativeInterface.SetTpidrEl0));
            SetNativeInterfaceDelegateInfo(new _Void_U64_S32 (NativeInterface.Break));
            SetNativeInterfaceDelegateInfo(new _Void_U64_S32 (NativeInterface.SupervisorCall));
            SetNativeInterfaceDelegateInfo(new _Void_U64_S32 (NativeInterface.Undefined));
            SetNativeInterfaceDelegateInfo(new _Void_U64_U16 (NativeInterface.WriteUInt16));
            SetNativeInterfaceDelegateInfo(new _Void_U64_U32 (NativeInterface.WriteUInt32));
            SetNativeInterfaceDelegateInfo(new _Void_U64_U64 (NativeInterface.WriteUInt64));
            SetNativeInterfaceDelegateInfo(new _Void_U64_U8  (NativeInterface.WriteByte));
            SetNativeInterfaceDelegateInfo(new _Void_U64_V128(NativeInterface.WriteVector128));

            SetSoftFallbackDelegateInfo(new _F64_F64                      (SoftFallback.Round));
            SetSoftFallbackDelegateInfo(new _F32_F32                      (SoftFallback.RoundF));
            SetSoftFallbackDelegateInfo(new _S32_F32                      (SoftFallback.SatF32ToS32));
            SetSoftFallbackDelegateInfo(new _S32_F64                      (SoftFallback.SatF64ToS32));
            SetSoftFallbackDelegateInfo(new _S64_F32                      (SoftFallback.SatF32ToS64));
            SetSoftFallbackDelegateInfo(new _S64_F64                      (SoftFallback.SatF64ToS64));
            SetSoftFallbackDelegateInfo(new _S64_S64                      (SoftFallback.UnarySignedSatQAbsOrNeg));
            SetSoftFallbackDelegateInfo(new _S64_S64_S32                  (SoftFallback.SignedSrcSignedDstSatQ));
            SetSoftFallbackDelegateInfo(new _S64_S64_S64                  (SoftFallback.MaxS64));
            SetSoftFallbackDelegateInfo(new _S64_S64_S64                  (SoftFallback.MinS64));
            SetSoftFallbackDelegateInfo(new _S64_S64_S64                  (SoftFallback.BinarySignedSatQAdd));
            SetSoftFallbackDelegateInfo(new _S64_S64_S64                  (SoftFallback.BinarySignedSatQSub));
            SetSoftFallbackDelegateInfo(new _S64_S64_S64_Bool_S32         (SoftFallback.SignedShlRegSatQ));
            SetSoftFallbackDelegateInfo(new _S64_S64_S64_Bool_S32         (SoftFallback.SignedShlReg));
            SetSoftFallbackDelegateInfo(new _S64_S64_S64_S32              (SoftFallback.SignedShrImm64));
            SetSoftFallbackDelegateInfo(new _S64_U64_S32                  (SoftFallback.UnsignedSrcSignedDstSatQ));
            SetSoftFallbackDelegateInfo(new _S64_U64_S64                  (SoftFallback.BinarySignedSatQAcc));
            SetSoftFallbackDelegateInfo(new _U32_F32                      (SoftFallback.SatF32ToU32));
            SetSoftFallbackDelegateInfo(new _U32_F64                      (SoftFallback.SatF64ToU32));
            SetSoftFallbackDelegateInfo(new _U32_U32                      (SoftFallback.ReverseBits32));
            SetSoftFallbackDelegateInfo(new _U32_U32                      (SoftFallback.ReverseBytes16_32));
            SetSoftFallbackDelegateInfo(new _U32_U32                      (SoftFallback.FixedRotate));
            SetSoftFallbackDelegateInfo(new _U32_U32                      (SoftFallback.ReverseBits8));
            SetSoftFallbackDelegateInfo(new _U32_U32_U16                  (SoftFallback.Crc32h));
            SetSoftFallbackDelegateInfo(new _U32_U32_U16                  (SoftFallback.Crc32ch));
            SetSoftFallbackDelegateInfo(new _U32_U32_U32                  (SoftFallback.Crc32w));
            SetSoftFallbackDelegateInfo(new _U32_U32_U32                  (SoftFallback.Crc32cw));
            SetSoftFallbackDelegateInfo(new _U32_U32_U64                  (SoftFallback.Crc32x));
            SetSoftFallbackDelegateInfo(new _U32_U32_U64                  (SoftFallback.Crc32cx));
            SetSoftFallbackDelegateInfo(new _U32_U32_U8                   (SoftFallback.Crc32b));
            SetSoftFallbackDelegateInfo(new _U32_U32_U8                   (SoftFallback.Crc32cb));
            SetSoftFallbackDelegateInfo(new _U64_F32                      (SoftFallback.SatF32ToU64));
            SetSoftFallbackDelegateInfo(new _U64_F64                      (SoftFallback.SatF64ToU64));
            SetSoftFallbackDelegateInfo(new _U64_S64_S32                  (SoftFallback.SignedSrcUnsignedDstSatQ));
            SetSoftFallbackDelegateInfo(new _U64_S64_U64                  (SoftFallback.BinaryUnsignedSatQAcc));
            SetSoftFallbackDelegateInfo(new _U64_U64                      (SoftFallback.ReverseBits64));
            SetSoftFallbackDelegateInfo(new _U64_U64                      (SoftFallback.ReverseBytes16_64));
            SetSoftFallbackDelegateInfo(new _U64_U64                      (SoftFallback.ReverseBytes32_64));
            SetSoftFallbackDelegateInfo(new _U64_U64                      (SoftFallback.CountSetBits8));
            SetSoftFallbackDelegateInfo(new _U64_U64_S32                  (SoftFallback.CountLeadingSigns));
            SetSoftFallbackDelegateInfo(new _U64_U64_S32                  (SoftFallback.CountLeadingZeros));
            SetSoftFallbackDelegateInfo(new _U64_U64_S32                  (SoftFallback.UnsignedSrcUnsignedDstSatQ));
            SetSoftFallbackDelegateInfo(new _U64_U64_S64_S32              (SoftFallback.UnsignedShrImm64));
            SetSoftFallbackDelegateInfo(new _U64_U64_U64                  (SoftFallback.MaxU64));
            SetSoftFallbackDelegateInfo(new _U64_U64_U64                  (SoftFallback.MinU64));
            SetSoftFallbackDelegateInfo(new _U64_U64_U64                  (SoftFallback.BinaryUnsignedSatQAdd));
            SetSoftFallbackDelegateInfo(new _U64_U64_U64                  (SoftFallback.BinaryUnsignedSatQSub));
            SetSoftFallbackDelegateInfo(new _U64_U64_U64_Bool_S32         (SoftFallback.UnsignedShlRegSatQ));
            SetSoftFallbackDelegateInfo(new _U64_U64_U64_Bool_S32         (SoftFallback.UnsignedShlReg));
            SetSoftFallbackDelegateInfo(new _V128_V128                    (SoftFallback.InverseMixColumns));
            SetSoftFallbackDelegateInfo(new _V128_V128                    (SoftFallback.MixColumns));
            SetSoftFallbackDelegateInfo(new _V128_V128_U32_V128           (SoftFallback.HashChoose));
            SetSoftFallbackDelegateInfo(new _V128_V128_U32_V128           (SoftFallback.HashMajority));
            SetSoftFallbackDelegateInfo(new _V128_V128_U32_V128           (SoftFallback.HashParity));
            SetSoftFallbackDelegateInfo(new _V128_V128_V128               (SoftFallback.Decrypt));
            SetSoftFallbackDelegateInfo(new _V128_V128_V128               (SoftFallback.Encrypt));
            SetSoftFallbackDelegateInfo(new _V128_V128_V128               (SoftFallback.Sha1SchedulePart2));
            SetSoftFallbackDelegateInfo(new _V128_V128_V128               (SoftFallback.Sha256SchedulePart1));
            SetSoftFallbackDelegateInfo(new _V128_V128_V128               (SoftFallback.Tbl1_V64));
            SetSoftFallbackDelegateInfo(new _V128_V128_V128               (SoftFallback.Tbl1_V128));
            SetSoftFallbackDelegateInfo(new _V128_V128_V128_V128          (SoftFallback.Sha1SchedulePart1));
            SetSoftFallbackDelegateInfo(new _V128_V128_V128_V128          (SoftFallback.HashLower));
            SetSoftFallbackDelegateInfo(new _V128_V128_V128_V128          (SoftFallback.HashUpper));
            SetSoftFallbackDelegateInfo(new _V128_V128_V128_V128          (SoftFallback.Sha256SchedulePart2));
            SetSoftFallbackDelegateInfo(new _V128_V128_V128_V128          (SoftFallback.Tbl2_V64));
            SetSoftFallbackDelegateInfo(new _V128_V128_V128_V128          (SoftFallback.Tbl2_V128));
            SetSoftFallbackDelegateInfo(new _V128_V128_V128_V128_V128     (SoftFallback.Tbl3_V64));
            SetSoftFallbackDelegateInfo(new _V128_V128_V128_V128_V128     (SoftFallback.Tbl3_V128));
            SetSoftFallbackDelegateInfo(new _V128_V128_V128_V128_V128_V128(SoftFallback.Tbl4_V64));
            SetSoftFallbackDelegateInfo(new _V128_V128_V128_V128_V128_V128(SoftFallback.Tbl4_V128));

            SetSoftFloatDelegateInfo(new _F32_U16(SoftFloat16_32.FPConvert));

            SetSoftFloatDelegateInfo(new _F32_F32         (SoftFloat32.FPRecipEstimate));
            SetSoftFloatDelegateInfo(new _F32_F32         (SoftFloat32.FPRecpX));
            SetSoftFloatDelegateInfo(new _F32_F32         (SoftFloat32.FPRSqrtEstimate));
            SetSoftFloatDelegateInfo(new _F32_F32         (SoftFloat32.FPSqrt));
            SetSoftFloatDelegateInfo(new _F32_F32_F32     (SoftFloat32.FPCompareEQ));
            SetSoftFloatDelegateInfo(new _F32_F32_F32     (SoftFloat32.FPCompareGE));
            SetSoftFloatDelegateInfo(new _F32_F32_F32     (SoftFloat32.FPCompareGT));
            SetSoftFloatDelegateInfo(new _F32_F32_F32     (SoftFloat32.FPCompareLE));
            SetSoftFloatDelegateInfo(new _F32_F32_F32     (SoftFloat32.FPCompareLT));
            SetSoftFloatDelegateInfo(new _F32_F32_F32     (SoftFloat32.FPSub));
            SetSoftFloatDelegateInfo(new _F32_F32_F32     (SoftFloat32.FPAdd));
            SetSoftFloatDelegateInfo(new _F32_F32_F32     (SoftFloat32.FPDiv));
            SetSoftFloatDelegateInfo(new _F32_F32_F32     (SoftFloat32.FPMax));
            SetSoftFloatDelegateInfo(new _F32_F32_F32     (SoftFloat32.FPMaxNum));
            SetSoftFloatDelegateInfo(new _F32_F32_F32     (SoftFloat32.FPMin));
            SetSoftFloatDelegateInfo(new _F32_F32_F32     (SoftFloat32.FPMinNum));
            SetSoftFloatDelegateInfo(new _F32_F32_F32     (SoftFloat32.FPMul));
            SetSoftFloatDelegateInfo(new _F32_F32_F32     (SoftFloat32.FPMulX));
            SetSoftFloatDelegateInfo(new _F32_F32_F32     (SoftFloat32.FPRecipStepFused));
            SetSoftFloatDelegateInfo(new _F32_F32_F32     (SoftFloat32.FPRSqrtStepFused));
            SetSoftFloatDelegateInfo(new _F32_F32_F32_F32 (SoftFloat32.FPMulAdd));
            SetSoftFloatDelegateInfo(new _F32_F32_F32_F32 (SoftFloat32.FPMulSub));
            SetSoftFloatDelegateInfo(new _S32_F32_F32_Bool(SoftFloat32.FPCompare));

            SetSoftFloatDelegateInfo(new _U16_F32(SoftFloat32_16.FPConvert));

            SetSoftFloatDelegateInfo(new _F64_F64         (SoftFloat64.FPRecipEstimate));
            SetSoftFloatDelegateInfo(new _F64_F64         (SoftFloat64.FPRecpX));
            SetSoftFloatDelegateInfo(new _F64_F64         (SoftFloat64.FPRSqrtEstimate));
            SetSoftFloatDelegateInfo(new _F64_F64         (SoftFloat64.FPSqrt));
            SetSoftFloatDelegateInfo(new _F64_F64_F64     (SoftFloat64.FPCompareEQ));
            SetSoftFloatDelegateInfo(new _F64_F64_F64     (SoftFloat64.FPCompareGE));
            SetSoftFloatDelegateInfo(new _F64_F64_F64     (SoftFloat64.FPCompareGT));
            SetSoftFloatDelegateInfo(new _F64_F64_F64     (SoftFloat64.FPCompareLE));
            SetSoftFloatDelegateInfo(new _F64_F64_F64     (SoftFloat64.FPCompareLT));
            SetSoftFloatDelegateInfo(new _F64_F64_F64     (SoftFloat64.FPSub));
            SetSoftFloatDelegateInfo(new _F64_F64_F64     (SoftFloat64.FPAdd));
            SetSoftFloatDelegateInfo(new _F64_F64_F64     (SoftFloat64.FPDiv));
            SetSoftFloatDelegateInfo(new _F64_F64_F64     (SoftFloat64.FPMax));
            SetSoftFloatDelegateInfo(new _F64_F64_F64     (SoftFloat64.FPMaxNum));
            SetSoftFloatDelegateInfo(new _F64_F64_F64     (SoftFloat64.FPMin));
            SetSoftFloatDelegateInfo(new _F64_F64_F64     (SoftFloat64.FPMinNum));
            SetSoftFloatDelegateInfo(new _F64_F64_F64     (SoftFloat64.FPMul));
            SetSoftFloatDelegateInfo(new _F64_F64_F64     (SoftFloat64.FPMulX));
            SetSoftFloatDelegateInfo(new _F64_F64_F64     (SoftFloat64.FPRecipStepFused));
            SetSoftFloatDelegateInfo(new _F64_F64_F64     (SoftFloat64.FPRSqrtStepFused));
            SetSoftFloatDelegateInfo(new _F64_F64_F64_F64 (SoftFloat64.FPMulAdd));
            SetSoftFloatDelegateInfo(new _F64_F64_F64_F64 (SoftFloat64.FPMulSub));
            SetSoftFloatDelegateInfo(new _S32_F64_F64_Bool(SoftFloat64.FPCompare));
        }

        private delegate double _F64_F64(double a1);
        private delegate double _F64_F64_F64(double a1, double a2);
        private delegate double _F64_F64_F64_F64(double a1, double a2, double a3);
        private delegate double _F64_F64_MidpointRounding(double a1, MidpointRounding a2);

        private delegate float _F32_F32(float a1);
        private delegate float _F32_F32_F32(float a1, float a2);
        private delegate float _F32_F32_F32_F32(float a1, float a2, float a3);
        private delegate float _F32_F32_MidpointRounding(float a1, MidpointRounding a2);
        private delegate float _F32_U16(ushort a1);

        private delegate int _S32_F32(float a1);
        private delegate int _S32_F32_F32_Bool(float a1, float a2, bool a3);
        private delegate int _S32_F64(double a1);
        private delegate int _S32_F64_F64_Bool(double a1, double a2, bool a3);
        private delegate int _S32_U64_U16(ulong a1, ushort a2);
        private delegate int _S32_U64_U32(ulong a1, uint a2);
        private delegate int _S32_U64_U64(ulong a1, ulong a2);
        private delegate int _S32_U64_U8(ulong a1, byte a2);
        private delegate int _S32_U64_V128(ulong a1, V128 a2);

        private delegate long _S64_F32(float a1);
        private delegate long _S64_F64(double a1);
        private delegate long _S64_S64(long a1);
        private delegate long _S64_S64_S32(long a1, int a2);
        private delegate long _S64_S64_S64(long a1, long a2);
        private delegate long _S64_S64_S64_Bool_S32(long a1, long a2, bool a3, int a4);
        private delegate long _S64_S64_S64_S32(long a1, long a2, int a3);
        private delegate long _S64_U64_S32(ulong a1, int a2);
        private delegate long _S64_U64_S64(ulong a1, long a2);

        private delegate ushort _U16_F32(float a1);
        private delegate ushort _U16_U64(ulong a1);

        private delegate uint _U32_F32(float a1);
        private delegate uint _U32_F64(double a1);
        private delegate uint _U32_U32(uint a1);
        private delegate uint _U32_U32_U16(uint a1, ushort a2);
        private delegate uint _U32_U32_U32(uint a1, uint a2);
        private delegate uint _U32_U32_U64(uint a1, ulong a2);
        private delegate uint _U32_U32_U8(uint a1, byte a2);
        private delegate uint _U32_U64(ulong a1);

        private delegate ulong _U64();
        private delegate ulong _U64_F32(float a1);
        private delegate ulong _U64_F64(double a1);
        private delegate ulong _U64_S64_S32(long a1, int a2);
        private delegate ulong _U64_S64_U64(long a1, ulong a2);
        private delegate ulong _U64_U64(ulong a1);
        private delegate ulong _U64_U64_S32(ulong a1, int a2);
        private delegate ulong _U64_U64_S64_S32(ulong a1, long a2, int a3);
        private delegate ulong _U64_U64_U64(ulong a1, ulong a2);
        private delegate ulong _U64_U64_U64_Bool_S32(ulong a1, ulong a2, bool a3, int a4);

        private delegate byte _U8_U64(ulong a1);

        private delegate V128 _V128_U64(ulong a1);
        private delegate V128 _V128_V128(V128 a1);
        private delegate V128 _V128_V128_U32_V128(V128 a1, uint a2, V128 a3);
        private delegate V128 _V128_V128_V128(V128 a1, V128 a2);
        private delegate V128 _V128_V128_V128_V128(V128 a1, V128 a2, V128 a3);
        private delegate V128 _V128_V128_V128_V128_V128(V128 a1, V128 a2, V128 a3, V128 a4);
        private delegate V128 _V128_V128_V128_V128_V128_V128(V128 a1, V128 a2, V128 a3, V128 a4, V128 a5);

        private delegate void _Void();
        private delegate void _Void_U64(ulong a1);
        private delegate void _Void_U64_S32(ulong a1, int a2);
        private delegate void _Void_U64_U16(ulong a1, ushort a2);
        private delegate void _Void_U64_U32(ulong a1, uint a2);
        private delegate void _Void_U64_U64(ulong a1, ulong a2);
        private delegate void _Void_U64_U8(ulong a1, byte a2);
        private delegate void _Void_U64_V128(ulong a1, V128 a2);
    }
}

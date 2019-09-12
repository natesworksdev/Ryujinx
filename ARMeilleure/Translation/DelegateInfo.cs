using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using System;
using System.Runtime.InteropServices;

namespace ARMeilleure.Translation
{
    class DelegateInfo
    {
        private readonly Delegate _dlg; // Ensures that this delegate will not be garbage collected.

        public IntPtr      FuncPtr { get; }
        public OperandType RetType { get; }

        public DelegateInfo(Delegate dlg)
        {
            _dlg = dlg;

            FuncPtr = Marshal.GetFunctionPointerForDelegate<Delegate>(dlg);
            RetType = GetOperandType(dlg.Method.ReturnType);
        }

        private static OperandType GetOperandType(Type type)
        {
            if (type == typeof(bool)   || type == typeof(byte)  ||
                type == typeof(char)   || type == typeof(short) ||
                type == typeof(int)    || type == typeof(sbyte) ||
                type == typeof(ushort) || type == typeof(uint))
            {
                return OperandType.I32;
            }
            else if (type == typeof(long) || type == typeof(ulong))
            {
                return OperandType.I64;
            }
            else if (type == typeof(double))
            {
                return OperandType.FP64;
            }
            else if (type == typeof(float))
            {
                return OperandType.FP32;
            }
            else if (type == typeof(V128))
            {
                return OperandType.V128;
            }
            else if (type == typeof(void))
            {
                return OperandType.None;
            }
            else
            {
                throw new ArgumentException($"Invalid type \"{type.Name}\".");
            }
        }
    }
}

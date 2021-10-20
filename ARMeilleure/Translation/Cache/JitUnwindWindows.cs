// https://github.com/MicrosoftDocs/cpp-docs/blob/master/docs/build/exception-handling-x64.md

using ARMeilleure.CodeGen.Unwinding;
using ARMeilleure.Common;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ARMeilleure.Translation.Cache
{
    unsafe class JitUnwindWindows : IDisposable
    {
        private const int MaxUnwindCodesArraySize = 32; // Must be an even value.

        private struct RuntimeFunction
        {
            public uint BeginAddress;
            public uint EndAddress;
            public uint UnwindData;
        }

        private struct UnwindInfo
        {
            public byte VersionAndFlags;
            public byte SizeOfProlog;
            public byte CountOfUnwindCodes;
            public byte FrameRegister;
            public fixed ushort UnwindCodes[MaxUnwindCodesArraySize];
        }

        private enum UnwindOp
        {
            PushNonvol    = 0,
            AllocLarge    = 1,
            AllocSmall    = 2,
            SetFpreg      = 3,
            SaveNonvol    = 4,
            SaveNonvolFar = 5,
            SaveXmm128    = 8,
            SaveXmm128Far = 9,
            PushMachframe = 10
        }

        private delegate RuntimeFunction* GetRuntimeFunctionCallback(ulong controlPc, IntPtr context);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool RtlInstallFunctionTableCallback(
            ulong tableIdentifier,
            ulong baseAddress,
            uint length,
            GetRuntimeFunctionCallback callback,
            IntPtr context,
            string outOfProcessCallbackDll);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool RtlDeleteFunctionTable(ulong tableIdentifier);

        private bool _disposed;
        private readonly ulong _identifier;
        private readonly JitCache _jitCache;
        private readonly GetRuntimeFunctionCallback _getRuntimeFunctionCallback;

        private readonly void* _workBuffer;
        private readonly RuntimeFunction* _runtimeFunction;
        private readonly UnwindInfo* _unwindInfo;

        public JitUnwindWindows(JitCache jitCache)
        {
            _jitCache = jitCache;

            _workBuffer = (void*)jitCache.Base;
            _runtimeFunction = (RuntimeFunction*)_workBuffer;
            _unwindInfo = (UnwindInfo*)((byte*)_workBuffer + Marshal.SizeOf<RuntimeFunction>());

            _getRuntimeFunctionCallback = new GetRuntimeFunctionCallback(FunctionTableHandler);

            ulong codeCachePtr = (ulong)_jitCache.Base;
            uint codeCacheLength = (uint)_jitCache.Size;

            _identifier = codeCachePtr | 3;

            if (!RtlInstallFunctionTableCallback(
                    _identifier,
                    codeCachePtr,
                    codeCacheLength,
                    _getRuntimeFunctionCallback,
                    _jitCache.Base,
                    null))
            {
                throw new InvalidOperationException("Failure installing function table callback.");
            }
        }

        private RuntimeFunction* FunctionTableHandler(ulong controlPc, IntPtr context)
        {
            int offset = (int)((long)controlPc - context.ToInt64());

            if (!_jitCache.TryFind(offset, out CacheEntry funcEntry))
            {
                return null; // Not found.
            }

            var unwindInfo = funcEntry.UnwindInfo;

            int codeIndex = 0;

            for (int index = unwindInfo.PushEntries.Length - 1; index >= 0; index--)
            {
                var entry = unwindInfo.PushEntries[index];

                switch (entry.PseudoOp)
                {
                    case UnwindPseudoOp.SaveXmm128:
                    {
                        int stackOffset = entry.StackOffsetOrAllocSize;

                        Debug.Assert(stackOffset % 16 == 0);

                        if (stackOffset <= 0xFFFF0)
                        {
                            _unwindInfo->UnwindCodes[codeIndex++] = PackUnwindOp(UnwindOp.SaveXmm128, entry.PrologOffset, entry.RegIndex);
                            _unwindInfo->UnwindCodes[codeIndex++] = (ushort)(stackOffset / 16);
                        }
                        else
                        {
                            _unwindInfo->UnwindCodes[codeIndex++] = PackUnwindOp(UnwindOp.SaveXmm128Far, entry.PrologOffset, entry.RegIndex);
                            _unwindInfo->UnwindCodes[codeIndex++] = (ushort)(stackOffset >> 0);
                            _unwindInfo->UnwindCodes[codeIndex++] = (ushort)(stackOffset >> 16);
                        }

                        break;
                    }

                    case UnwindPseudoOp.AllocStack:
                    {
                        int allocSize = entry.StackOffsetOrAllocSize;

                        Debug.Assert(allocSize % 8 == 0);

                        if (allocSize <= 128)
                        {
                            _unwindInfo->UnwindCodes[codeIndex++] = PackUnwindOp(UnwindOp.AllocSmall, entry.PrologOffset, (allocSize / 8) - 1);
                        }
                        else if (allocSize <= 0x7FFF8)
                        {
                            _unwindInfo->UnwindCodes[codeIndex++] = PackUnwindOp(UnwindOp.AllocLarge, entry.PrologOffset, 0);
                            _unwindInfo->UnwindCodes[codeIndex++] = (ushort)(allocSize / 8);
                        }
                        else
                        {
                            _unwindInfo->UnwindCodes[codeIndex++] = PackUnwindOp(UnwindOp.AllocLarge, entry.PrologOffset, 1);
                            _unwindInfo->UnwindCodes[codeIndex++] = (ushort)(allocSize >> 0);
                            _unwindInfo->UnwindCodes[codeIndex++] = (ushort)(allocSize >> 16);
                        }

                        break;
                    }

                    case UnwindPseudoOp.PushReg:
                    {
                        _unwindInfo->UnwindCodes[codeIndex++] = PackUnwindOp(UnwindOp.PushNonvol, entry.PrologOffset, entry.RegIndex);

                        break;
                    }

                    default: throw new NotImplementedException($"({nameof(entry.PseudoOp)} = {entry.PseudoOp})");
                }
            }

            Debug.Assert(codeIndex <= MaxUnwindCodesArraySize);

            _unwindInfo->VersionAndFlags    = 1; // Flags: The function has no handler.
            _unwindInfo->SizeOfProlog       = (byte)unwindInfo.PrologSize;
            _unwindInfo->CountOfUnwindCodes = (byte)codeIndex;
            _unwindInfo->FrameRegister      = 0;

            _runtimeFunction->BeginAddress = (uint)funcEntry.Offset;
            _runtimeFunction->EndAddress   = (uint)(funcEntry.Offset + funcEntry.Size);
            _runtimeFunction->UnwindData   = (uint)Marshal.SizeOf<RuntimeFunction>();

            return _runtimeFunction;
        }

        private static ushort PackUnwindOp(UnwindOp op, int prologOffset, int opInfo)
        {
            return (ushort)(prologOffset | ((int)op << 8) | (opInfo << 12));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (!RtlDeleteFunctionTable(_identifier))
            {
                throw new InvalidOperationException("Failure uninstalling function table.");
            }

            _disposed = true;
        }

        ~JitUnwindWindows()
        {
            Dispose(false);
        }
    }
}
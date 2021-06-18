using ARMeilleure.Translation.Cache;
using ARMeilleure.Translation.PTC;
using Ryujinx.Common;
using Ryujinx.Common.Logging;
using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ARMeilleure.Translation.TTC
{
    static class Ttc
    {
        public static bool TryFastTranslateDyn(
            Translator translator,
            ulong address,
            ulong funcSize,
            bool highCq,
            ref TtcInfo ttcInfo,
            out TranslatedFunction translatedFuncDyn)
        {
            Debug.Assert(ttcInfo == null);
            translatedFuncDyn = null;

            if (!Translator.OverlapsWith(address, funcSize, Translator.StaticCodeStart, Translator.StaticCodeSize))
            {
                Hash128 preHash = translator.ComputeHash(address, funcSize);
                Hash128 hash = highCq ? ~preHash : preHash;

                if (!translator.TtcInfos.TryGetValue(hash, out ttcInfo))
                {
                    ttcInfo = new TtcInfo();

                    ttcInfo.LastGuestAddress = address;
                    ttcInfo.GuestSize = funcSize;

                    if (!translator.TtcInfos.TryAdd(hash, ttcInfo))
                    {
                        ttcInfo.Dispose();

                        ttcInfo = null;
                    }
                }
                else if (ttcInfo.IsBusy)
                {
                    ttcInfo = null;
                }
                else
                {
                    ttcInfo.IsBusy = true;

                    ttcInfo.LastGuestAddress = address;

                    if (ttcInfo.RelocEntriesCount != 0)
                    {
                        RelocEntry[] relocEntries = Ptc.GetRelocEntries(ttcInfo.RelocStream, ttcInfo.RelocEntriesCount, reloadStream: true);

                        JitCache.ModifyMapped(ttcInfo.TranslatedFunc.FuncPtr, ttcInfo.HostSize, (code) => PatchCodeDyn(translator, code, relocEntries, address));
                    }

                    if (ttcInfo.TranslatedFunc.CallCounter != null && Volatile.Read(ref ttcInfo.TranslatedFunc.CallCounter.Value) > Translator.MinsCallForRejit)
                    {
                        Volatile.Write(ref ttcInfo.TranslatedFunc.CallCounter.Value, Translator.MinsCallForRejit);
                    }

                    translatedFuncDyn = ttcInfo.TranslatedFunc;

                    Logger.Debug?.Print(LogClass.Ttc,
                        $"Fast translated dynamic function 0x{preHash} " +
                        $"(HighCq: {highCq}{(!highCq ? $" [CallCounter: {ttcInfo.TranslatedFunc.CallCounter.Value}]" : string.Empty)}, HostSize: {ttcInfo.HostSize}) " +
                        $"| DynFuncs: {translator.TtcInfos.Count}.");

                    return true;
                }
            }

            return false;
        }

        private static void PatchCodeDyn(Translator translator, Span<byte> code, RelocEntry[] relocEntries, ulong address)
        {
            foreach (RelocEntry relocEntry in relocEntries)
            {
                ulong? imm = null;
                Symbol symbol = relocEntry.Symbol;

                if (symbol.Type == SymbolType.FunctionTable)
                {
                    ulong offset = symbol.Value;

                    if (translator.FunctionTable.IsValid(address + offset))
                    {
                        unsafe { imm = (ulong)Unsafe.AsPointer(ref translator.FunctionTable.GetValue(address + offset)); }
                    }
                }
                else if (symbol.Type == SymbolType.DynFunc)
                {
                    ulong offset = symbol.Value;

                    imm = address + offset;
                }
                else if (symbol.Type == SymbolType.DynFuncAdrp)
                {
                    ulong offset = symbol.Value;

                    imm = (address + offset) & ~0xfffUL;
                }

                if (imm == null)
                {
                    throw new Exception($"Unexpected reloc entry {relocEntry}.");
                }

                BinaryPrimitives.WriteUInt64LittleEndian(code.Slice(relocEntry.Position, 8), imm.Value);
            }
        }
    }
}

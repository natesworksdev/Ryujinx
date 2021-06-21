using ARMeilleure.Translation.Cache;
using ARMeilleure.Translation.PTC;
using Ryujinx.Common;
using Ryujinx.Common.Logging;
using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ARMeilleure.Translation.TTC
{
    static class Ttc
    {
        private const int MinFuncSizeDyn = 128;

        public static bool TryFastTranslateDyn(
            Translator translator,
            ulong address,
            ulong funcSize,
            bool highCq,
            ref TtcInfo ttcInfoRef,
            out TranslatedFunction translatedFuncDyn)
        {
            ttcInfoRef = null;
            translatedFuncDyn = null;

            if (!Translator.OverlapsWith(address, funcSize, Translator.StaticCodeStart, Translator.StaticCodeSize) && (highCq || funcSize > MinFuncSizeDyn))
            {
                Hash128 preHash = translator.ComputeHash(address, funcSize);
                Hash128 hash = highCq ? ~preHash : preHash;

                if (!translator.TtcInfos.TryGetValue(hash, out TtcInfo ttcInfoOut))
                {
                    TtcInfo ttcInfoNew = new TtcInfo();

                    ttcInfoNew.IsBusy = true;

                    ttcInfoNew.LastGuestAddress = address;
                    ttcInfoNew.GuestSize = funcSize;

                    if (translator.TtcInfos.TryAdd(hash, ttcInfoNew))
                    {
                        ttcInfoRef = ttcInfoNew;
                    }
                    else
                    {
                        ttcInfoNew.Dispose();
                    }
                }
                else
                {
                    lock (ttcInfoOut)
                    {
                        if (!ttcInfoOut.IsBusy)
                        {
                            ttcInfoOut.IsBusy = true;

                            ttcInfoOut.LastGuestAddress = address;

                            if (ttcInfoOut.RelocEntriesCount != 0)
                            {
                                RelocEntry[] relocEntries = Ptc.GetRelocEntries(ttcInfoOut.RelocStream, ttcInfoOut.RelocEntriesCount, reloadStream: true);

                                JitCache.ModifyMapped(ttcInfoOut.TranslatedFunc.FuncPtr, ttcInfoOut.HostSize, (code) => PatchCodeDyn(translator, code, relocEntries, address));
                            }

                            if (ttcInfoOut.TranslatedFunc.CallCounter != null && Volatile.Read(ref ttcInfoOut.TranslatedFunc.CallCounter.Value) > Translator.MinsCallForRejit)
                            {
                                Volatile.Write(ref ttcInfoOut.TranslatedFunc.CallCounter.Value, Translator.MinsCallForRejit);
                            }

                            translatedFuncDyn = ttcInfoOut.TranslatedFunc;

                            Logger.Debug?.Print(LogClass.Ttc,
                                $"Fast translated dynamic function 0x{preHash} " +
                                $"(HighCq: {highCq}{(!highCq ? $" [CallCounter: {ttcInfoOut.TranslatedFunc.CallCounter.Value}]" : string.Empty)}, HostSize: {ttcInfoOut.HostSize}) " +
                                $"| DynFuncs: {translator.TtcInfos.Count}.");

                            return true;
                        }
                    }
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

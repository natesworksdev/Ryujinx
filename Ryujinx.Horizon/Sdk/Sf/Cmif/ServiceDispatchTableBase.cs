using Ryujinx.Horizon.Common;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk.Sf.Cmif
{
    abstract class ServiceDispatchTableBase
    {
        private const uint CmifInHeaderMagic = 0x49434653; // SFCI
        private const uint CmifOutHeaderMagic = 0x4f434653; // SFCO
        private const uint MaxCmifVersion = 1;

        public abstract Result ProcessMessage(ref ServiceDispatchContext context, ReadOnlySpan<byte> inRawData);

        protected Result ProcessMessageImpl(ref ServiceDispatchContext context, ReadOnlySpan<byte> inRawData, IReadOnlyDictionary<uint, CommandHandler> entries)
        {
            if (inRawData.Length < Unsafe.SizeOf<CmifInHeader>())
            {
                return SfResult.InvalidHeaderSize;
            }

            CmifInHeader inHeader = MemoryMarshal.Cast<byte, CmifInHeader>(inRawData)[0];

            if (inHeader.Magic != CmifInHeaderMagic || inHeader.Version > MaxCmifVersion)
            {
                return SfResult.InvalidInHeader;
            }

            ReadOnlySpan<byte> inMessageRawData = inRawData[Unsafe.SizeOf<CmifInHeader>()..];
            uint commandId = inHeader.CommandId;

            if (!entries.TryGetValue(commandId, out var commandHandler))
            {
                return SfResult.UnknownCommandId;
            }

            var outHeader = Span<CmifOutHeader>.Empty;

            Result commandResult = commandHandler.Invoke(ref outHeader, ref context, inMessageRawData);

            if (SfResult.RequestContextChanged(commandResult))
            {
                return commandResult;
            }

            if (outHeader.IsEmpty)
            {
                commandResult.AbortOnSuccess();
                return commandResult;
            }

            outHeader[0] = new CmifOutHeader() { Magic = CmifOutHeaderMagic, Result = commandResult };

            return Result.Success;
        }
    }
}

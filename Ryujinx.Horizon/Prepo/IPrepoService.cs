using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Account;
using Ryujinx.Horizon.Sdk.Sf;
using System;

namespace Ryujinx.Horizon.Prepo
{
    interface IPrepoService : IServiceObject
    {
        Result SaveReport(ReadOnlySpan<byte> gameRoomBuffer, ReadOnlySpan<byte> reportBuffer, ulong pid);
        Result SaveReportWithUser(UserId userId, ReadOnlySpan<byte> gameRoomBuffer, ReadOnlySpan<byte> reportBuffer, ulong pid);
        Result RequestImmediateTransmission();
        Result GetTransmissionStatus(out int resultCode);
        Result GetSystemSessionId(out ulong systemSessionId);
        Result SaveSystemReport(ReadOnlySpan<byte> gameRoomBuffer, ReadOnlySpan<byte> reportBuffer, ulong pid);
        Result SaveSystemReportWithUser(UserId userId, ReadOnlySpan<byte> gameRoomBuffer, ReadOnlySpan<byte> reportBuffer, ulong pid);
        Result IsUserAgreementCheckEnabled(out bool enabled);
        Result SetUserAgreementCheckEnabled(bool enabled);
    }
}
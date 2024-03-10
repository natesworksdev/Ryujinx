using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Applet;
using Ryujinx.Horizon.Sdk.Sf;
using System;

namespace Ryujinx.Horizon.Sdk.Irsensor
{
    interface IIrSensorServer : IServiceObject
    {
        Result ActivateIrsensor(AppletResourceUserId arg0, ulong pid);
        Result DeactivateIrsensor(AppletResourceUserId arg0, ulong pid);
        Result GetIrsensorSharedMemoryHandle(out int arg0, AppletResourceUserId arg1, ulong pid);
        Result StopImageProcessor(AppletResourceUserId arg0, IrCameraHandle arg1, ulong pid);
        Result RunMomentProcessor(AppletResourceUserId arg0, IrCameraHandle arg1, PackedMomentProcessorConfig arg2, ulong pid);
        Result RunClusteringProcessor(AppletResourceUserId arg0, IrCameraHandle arg1, PackedClusteringProcessorConfig arg2, ulong pid);
        Result RunImageTransferProcessor(AppletResourceUserId arg0, IrCameraHandle arg1, PackedImageTransferProcessorConfig arg2, int arg3, ulong arg4, ulong pid);
        Result GetImageTransferProcessorState(AppletResourceUserId arg0, out ImageTransferProcessorState arg1, Span<byte> arg2, IrCameraHandle arg3, ulong pid);
        Result RunTeraPluginProcessor(AppletResourceUserId arg0, IrCameraHandle arg1, PackedTeraPluginProcessorConfig arg2, ulong pid);
        Result GetNpadIrCameraHandle(out IrCameraHandle arg0, uint arg1);
        Result RunPointingProcessor(AppletResourceUserId arg0, IrCameraHandle arg1, PackedPointingProcessorConfig arg2, ulong pid);
        Result SuspendImageProcessor(AppletResourceUserId arg0, IrCameraHandle arg1, ulong pid);
        Result CheckFirmwareVersion(AppletResourceUserId arg0, IrCameraHandle arg1, PackedMcuVersion arg2, ulong pid);
        Result SetFunctionLevel(AppletResourceUserId arg0, IrCameraHandle arg1, PackedFunctionLevel arg2, ulong pid);
        Result RunImageTransferExProcessor(AppletResourceUserId arg0, IrCameraHandle arg1, PackedImageTransferProcessorExConfig arg2, int arg3, ulong arg4, ulong pid);
        Result RunIrLedProcessor(AppletResourceUserId arg0, IrCameraHandle arg1, PackedIrLedProcessorConfig arg2, ulong pid);
        Result StopImageProcessorAsync(AppletResourceUserId arg0, IrCameraHandle arg1, ulong pid);
        Result ActivateIrsensorWithFunctionLevel(AppletResourceUserId arg0, PackedFunctionLevel arg1, ulong pid);
    }
}

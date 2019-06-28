using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.HLE.HOS.Services.Hid;
using System;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Nfc.Nfp
{
    class IUser : IpcService
    {
        private State _state = State.NonInitialized;

        private KEvent _availabilityChangeEvent;
        private int    _availabilityChangeEventHandle = 0;

        private List<Device> _devices = new List<Device>();

        private Dictionary<int, ServiceProcessRequest> _commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _commands;

        public IUser()
        {
            _commands = new Dictionary<int, ServiceProcessRequest>
            {
                { 0,  Initialize                    },
                { 1,  Finalize                      },
                { 2,  ListDevices                   },
              //{ 3,  StartDetection                },
              //{ 4,  StopDetection                 },
              //{ 5,  Mount                         },
              //{ 6,  Unmount                       },
              //{ 7,  OpenApplicationArea           },
              //{ 8,  GetApplicationArea            },
              //{ 9,  SetApplicationArea            },
              //{ 10, Flush                         },
              //{ 11, Restore                       },
              //{ 12, CreateApplicationArea         },
              //{ 13, GetTagInfo                    },
              //{ 14, GetRegisterInfo               },
              //{ 15, GetCommonInfo                 },
              //{ 16, GetModelInfo                  },
                { 17, AttachActivateEvent           },
                { 18, AttachDeactivateEvent         },
                { 19, GetState                      },
                { 20, GetDeviceState                },
                { 21, GetNpadId                     },
              //{ 22, GetApplicationAreaSize        },
                { 23, AttachAvailabilityChangeEvent }, // 3.0.0+
              //{ 24, RecreateApplicationArea       }, // 3.0.0+
            };
        }

        // Initialize(u64, u64, pid, buffer<unknown, 5>)
        public long Initialize(ServiceCtx context)
        {
            long appletResourceUserId = context.RequestData.ReadInt64();
            long mcuVersionData       = context.RequestData.ReadInt64();

            long inputPosition = context.Request.SendBuff[0].Position;
            long inputSize     = context.Request.SendBuff[0].Size;

            byte[] unknownBuffer = context.Memory.ReadBytes(inputPosition, inputSize);

            // NOTE: appletResourceUserId, mcuVersionData and the buffer are stored inside an internal struct.
            //       The buffer seems to contains entries with a size of 0x40 bytes each.
            //       Sadly, this internal struct doesn't seems to be used in retail.

            // TODO: Add an instance of nn::nfc::server::Manager when it will be implemented.
            //       Add an instance of nn::nfc::server::SaveData when it will be implemented.

            // TODO: When we will be able to add multiple controllers add one entry by controller here.
            Device Device1 = new Device
            {
                NpadIdType = NpadIdType.Player1,
                Handle     = HidUtils.GetIndexFromNpadIdType(NpadIdType.Player1),
                State      = DeviceState.Initialized
            };

            _devices.Add(Device1);

            _state = State.Initialized;

            return 0;
        }

        // Finalize()
        public long Finalize(ServiceCtx context)
        {
            // TODO: Call StopDetection() and Unmount() when they will be implemented.
            //       Remove the instance of nn::nfc::server::Manager when it will be implemented.
            //       Remove the instance of nn::nfc::server::SaveData when it will be implemented.

            _devices.Clear();

            _state = State.NonInitialized;

            return 0;
        }

        // ListDevices() -> (u32, buffer<unknown, 0xa>)
        public long ListDevices(ServiceCtx context)
        {
            if (context.Request.RecvListBuff.Count == 0)
            {
                return ErrorCode.MakeError(ErrorModule.Nfp, NfpError.DevicesBufferIsNull);
            }

            long outputPosition = context.Request.RecvListBuff[0].Position;
            long outputSize     = context.Request.RecvListBuff[0].Size;

            if (_devices.Count == 0)
            {
                return ErrorCode.MakeError(ErrorModule.Nfp, NfpError.DeviceNotFound);
            }

            for (int i = 0; i < _devices.Count; i++)
            {
                context.Memory.WriteUInt32(outputPosition + (i * sizeof(long)), (uint)_devices[i].Handle);
            }

            context.ResponseData.Write(_devices.Count);

            return 0;
        }

        // AttachActivateEvent(bytes<8, 4>) -> handle<copy>
        public long AttachActivateEvent(ServiceCtx context)
        {
            uint DeviceHandle = context.RequestData.ReadUInt32();

            for (int i = 0; i < _devices.Count; i++)
            {
                if ((uint)_devices[i].Handle == DeviceHandle)
                {
                    if (_devices[i].ActivateEventHandle == 0)
                    {
                        _devices[i].ActivateEvent = new KEvent(context.Device.System);

                        if (context.Process.HandleTable.GenerateHandle(_devices[i].ActivateEvent.ReadableEvent, out _devices[i].ActivateEventHandle) != KernelResult.Success)
                        {
                            throw new InvalidOperationException("Out of handles!");
                        }
                    }

                    context.Response.HandleDesc = IpcHandleDesc.MakeCopy(_devices[i].ActivateEventHandle);

                    return 0;
                }
            }

            return ErrorCode.MakeError(ErrorModule.Nfp, NfpError.DeviceNotFound);
        }

        // AttachDeactivateEvent(bytes<8, 4>) -> handle<copy>
        public long AttachDeactivateEvent(ServiceCtx context)
        {
            uint DeviceHandle = context.RequestData.ReadUInt32();

            for (int i = 0; i < _devices.Count; i++)
            {
                if ((uint)_devices[i].Handle == DeviceHandle)
                {
                    if (_devices[i].DeactivateEventHandle == 0)
                    {
                        _devices[i].DeactivateEvent = new KEvent(context.Device.System);

                        if (context.Process.HandleTable.GenerateHandle(_devices[i].DeactivateEvent.ReadableEvent, out _devices[i].DeactivateEventHandle) != KernelResult.Success)
                        {
                            throw new InvalidOperationException("Out of handles!");
                        }
                    }

                    context.Response.HandleDesc = IpcHandleDesc.MakeCopy(_devices[i].DeactivateEventHandle);

                    return 0;
                }
            }

            return ErrorCode.MakeError(ErrorModule.Nfp, NfpError.DeviceNotFound);
        }

        // GetState() -> u32
        public long GetState(ServiceCtx context)
        {
            context.ResponseData.Write((int)_state);

            return 0;
        }

        // GetDeviceState(bytes<8, 4>) -> u32
        public long GetDeviceState(ServiceCtx context)
        {
            uint DeviceHandle = context.RequestData.ReadUInt32();

            for (int i = 0; i < _devices.Count; i++)
            {
                if ((uint)_devices[i].Handle == DeviceHandle)
                {
                    context.ResponseData.Write((uint)_devices[i].State);

                    return 0;
                }
            }

            context.ResponseData.Write((uint)DeviceState.Unavailable);

            return ErrorCode.MakeError(ErrorModule.Nfp, NfpError.DeviceNotFound);
        }

        // GetNpadId(bytes<8, 4>) -> u32
        public long GetNpadId(ServiceCtx context)
        {
            uint DeviceHandle = context.RequestData.ReadUInt32();

            for (int i = 0; i < _devices.Count; i++)
            {
                if ((uint)_devices[i].Handle == DeviceHandle)
                {
                    context.ResponseData.Write((uint)HidUtils.GetNpadIdTypeFromIndex(_devices[i].Handle));

                    return 0;
                }
            }

            return ErrorCode.MakeError(ErrorModule.Nfp, NfpError.DeviceNotFound);
        }

        // AttachAvailabilityChangeEvent() -> handle<copy>
        public long AttachAvailabilityChangeEvent(ServiceCtx context)
        {
            if (_availabilityChangeEventHandle == 0)
            {
                _availabilityChangeEvent = new KEvent(context.Device.System);

                if (context.Process.HandleTable.GenerateHandle(_availabilityChangeEvent.ReadableEvent, out _availabilityChangeEventHandle) != KernelResult.Success)
                {
                    throw new InvalidOperationException("Out of handles!");
                }
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(_availabilityChangeEventHandle);

            return 0;
        }
    }
}
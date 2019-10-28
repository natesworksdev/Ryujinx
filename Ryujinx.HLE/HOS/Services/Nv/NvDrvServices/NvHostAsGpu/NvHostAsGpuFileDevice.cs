using Ryujinx.Common.Logging;
using Ryujinx.Graphics.Memory;
using Ryujinx.HLE.HOS.Kernel.Process;
using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostAsGpu.Types;
using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvMap;
using System;
using System.Collections.Concurrent;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostAsGpu
{
    class NvHostAsGpuFileDevice : NvFileDevice
    {
        private const int FlagFixedOffset   = 1;
        private const int FlagRemapSubRange = 0x100;

        private static ConcurrentDictionary<KProcess, AddressSpaceContext> _addressSpaceContextRegistry = new ConcurrentDictionary<KProcess, AddressSpaceContext>();

        public NvHostAsGpuFileDevice(KProcess owner) : base(owner)
        {

        }

        public override NvInternalResult Ioctl(NvIoctl command, Span<byte> arguments)
        {
            NvInternalResult result = NvInternalResult.NotImplemented;

            if (command.GetTypeValue() == NvIoctl.NvGpuAsMagic)
            {
                switch (command.GetNumberValue())
                {
                    case 0x1:
                        result = CallIoctlMethod<BindChannelArguments>(BindChannel, arguments);
                        break;
                    case 0x2:
                        result = CallIoctlMethod<AllocSpaceArguments>(AllocSpace, arguments);
                        break;
                    case 0x3:
                        result = CallIoctlMethod<FreeSpaceArguments>(FreeSpace, arguments);
                        break;
                    case 0x5:
                        result = CallIoctlMethod<UnmapBufferArguments>(UnmapBuffer, arguments);
                        break;
                    case 0x6:
                        result = CallIoctlMethod<MapBufferExArguments>(MapBufferEx, arguments);
                        break;
                    case 0x8:
                        result = CallIoctlMethod<GetVaRegionsArguments>(GetVaRegions, arguments);
                        break;
                    case 0x9:
                        result = CallIoctlMethod<InitializeExArguments>(InitializeEx, arguments);
                        break;
                    case 0x14:
                        result = CallIoctlMethod<RemapArguments>(Remap, arguments);
                        break;
                }
            }

            return result;
        }

        private NvInternalResult BindChannel(ref BindChannelArguments arguments)
        {
            Logger.PrintStub(LogClass.ServiceNv);

            return NvInternalResult.Success;
        }

        private NvInternalResult AllocSpace(ref AllocSpaceArguments arguments)
        {
            AddressSpaceContext addressSpaceContext = GetAddressSpaceContext(_owner);

            ulong size = (ulong)arguments.Pages * (ulong)arguments.PageSize;

            NvInternalResult result = NvInternalResult.Success;

            lock (addressSpaceContext)
            {
                // Note: When the fixed offset flag is not set,
                // the Offset field holds the alignment size instead.
                if ((arguments.Flags & FlagFixedOffset) != 0)
                {
                    arguments.Offset = addressSpaceContext.Vmm.ReserveFixed(arguments.Offset, (long)size);
                }
                else
                {
                    arguments.Offset = addressSpaceContext.Vmm.Reserve((long)size, arguments.Offset);
                }

                if (arguments.Offset < 0)
                {
                    arguments.Offset = 0;

                    Logger.PrintWarning(LogClass.ServiceNv, $"Failed to allocate size {size:x16}!");

                    result = NvInternalResult.OutOfMemory;
                }
                else
                {
                    addressSpaceContext.AddReservation(arguments.Offset, (long)size);
                }
            }

            return result;
        }

        private NvInternalResult FreeSpace(ref FreeSpaceArguments arguments)
        {
            AddressSpaceContext addressSpaceContext = GetAddressSpaceContext(_owner);

            NvInternalResult result = NvInternalResult.Success;

            lock (addressSpaceContext)
            {
                ulong size = (ulong)arguments.Pages * (ulong)arguments.PageSize;

                if (addressSpaceContext.RemoveReservation(arguments.Offset))
                {
                    addressSpaceContext.Vmm.Free(arguments.Offset, (long)size);
                }
                else
                {
                    Logger.PrintWarning(LogClass.ServiceNv,
                        $"Failed to free offset 0x{arguments.Offset:x16} size 0x{size:x16}!");

                    result = NvInternalResult.InvalidInput;
                }
            }

            return result;
        }

        private NvInternalResult UnmapBuffer(ref UnmapBufferArguments arguments)
        {
            AddressSpaceContext addressSpaceContext = GetAddressSpaceContext(_owner);

            lock (addressSpaceContext)
            {
                if (addressSpaceContext.RemoveMap(arguments.Offset, out long size))
                {
                    if (size != 0)
                    {
                        addressSpaceContext.Vmm.Free(arguments.Offset, size);
                    }
                }
                else
                {
                    Logger.PrintWarning(LogClass.ServiceNv, $"Invalid buffer offset {arguments.Offset:x16}!");
                }
            }

            return NvInternalResult.Success;
        }

        private NvInternalResult MapBufferEx(ref MapBufferExArguments arguments)
        {
            const string mapErrorMsg = "Failed to map fixed buffer with offset 0x{0:x16} and size 0x{1:x16}!";

            AddressSpaceContext addressSpaceContext = GetAddressSpaceContext(_owner);

            NvMapHandle map = NvMapFileDevice.GetMapFromHandle(_owner, arguments.NvMapHandle, true);

            if (map == null)
            {
                Logger.PrintWarning(LogClass.ServiceNv, $"Invalid NvMap handle 0x{arguments.NvMapHandle:x8}!");

                return NvInternalResult.InvalidInput;
            }

            long pa;

            if ((arguments.Flags & FlagRemapSubRange) != 0)
            {
                lock (addressSpaceContext)
                {
                    if (addressSpaceContext.TryGetMapPhysicalAddress(arguments.Offset, out pa))
                    {
                        long va = arguments.Offset + arguments.BufferOffset;

                        pa += arguments.BufferOffset;

                        if (addressSpaceContext.Vmm.Map(pa, va, arguments.MappingSize) < 0)
                        {
                            string msg = string.Format(mapErrorMsg, va, arguments.MappingSize);

                            Logger.PrintWarning(LogClass.ServiceNv, msg);

                            return NvInternalResult.InvalidInput;
                        }

                        return NvInternalResult.Success;
                    }
                    else
                    {
                        Logger.PrintWarning(LogClass.ServiceNv, $"Address 0x{arguments.Offset:x16} not mapped!");

                        return NvInternalResult.InvalidInput;
                    }
                }
            }

            pa = map.Address + arguments.BufferOffset;

            long size = arguments.MappingSize;

            if (size == 0)
            {
                size = (uint)map.Size;
            }

            NvInternalResult result = NvInternalResult.Success;

            lock (addressSpaceContext)
            {
                // Note: When the fixed offset flag is not set,
                // the Offset field holds the alignment size instead.
                bool vaAllocated = (arguments.Flags & FlagFixedOffset) == 0;

                if (!vaAllocated)
                {
                    if (addressSpaceContext.ValidateFixedBuffer(arguments.Offset, size))
                    {
                        arguments.Offset = addressSpaceContext.Vmm.Map(pa, arguments.Offset, size);
                    }
                    else
                    {
                        string msg = string.Format(mapErrorMsg, arguments.Offset, size);

                        Logger.PrintWarning(LogClass.ServiceNv, msg);

                        result = NvInternalResult.InvalidInput;
                    }
                }
                else
                {
                    arguments.Offset = addressSpaceContext.Vmm.Map(pa, size);
                }

                if (arguments.Offset < 0)
                {
                    arguments.Offset = 0;

                    Logger.PrintWarning(LogClass.ServiceNv, $"Failed to map size 0x{size:x16}!");

                    result = NvInternalResult.InvalidInput;
                }
                else
                {
                    addressSpaceContext.AddMap(arguments.Offset, size, pa, vaAllocated);
                }
            }

            return result;
        }

        private NvInternalResult GetVaRegions(ref GetVaRegionsArguments arguments)
        {
            Logger.PrintStub(LogClass.ServiceNv);

            return NvInternalResult.Success;
        }

        private NvInternalResult InitializeEx(ref InitializeExArguments arguments)
        {
            Logger.PrintStub(LogClass.ServiceNv);

            return NvInternalResult.Success;
        }

        private NvInternalResult Remap(Span<RemapArguments> arguments)
        {
            for (int index = 0; index < arguments.Length; index++)
            {
                NvGpuVmm vmm = GetAddressSpaceContext(_owner).Vmm;

                NvMapHandle map = NvMapFileDevice.GetMapFromHandle(_owner, arguments[index].NvMapHandle, true);

                if (map == null)
                {
                    Logger.PrintWarning(LogClass.ServiceNv, $"Invalid NvMap handle 0x{arguments[index].NvMapHandle:x8}!");

                    return NvInternalResult.InvalidInput;
                }

                long result = vmm.Map(map.Address, (long)arguments[index].Offset << 16,
                                                   (long)arguments[index].Pages << 16);

                if (result < 0)
                {
                    Logger.PrintWarning(LogClass.ServiceNv,
                        $"Page 0x{arguments[index].Offset:x16} size 0x{arguments[index].Pages:x16} not allocated!");

                    return NvInternalResult.InvalidInput;
                }
            }

            return NvInternalResult.Success;
        }

        public override void Close()
        {
            // TODO
        }

        public static AddressSpaceContext GetAddressSpaceContext(KProcess process)
        {
            return _addressSpaceContextRegistry.GetOrAdd(process, (key) => new AddressSpaceContext(process));
        }
    }
}

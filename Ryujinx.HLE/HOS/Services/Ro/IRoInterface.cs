using LibHac.FsSystem;
using Ryujinx.Common;
using Ryujinx.HLE.HOS.Kernel;
using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Kernel.Memory;
using Ryujinx.HLE.Loaders.Executables;
using Ryujinx.HLE.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Ryujinx.HLE.HOS.Services.Ro
{
    [Service("ldr:ro")]
    [Service("ro:1")] // 7.0.0+
    class IRoInterface : IpcService, IDisposable
    {
        private const int MaxNrr         = 0x40;
        private const int MaxNro         = 0x40;
        private const int MaxMapRetries  = 0x200;
        private const int GuardPagesSize = 0x4000;

        private const uint NrrMagic = 0x3052524E;
        private const uint NroMagic = 0x304F524E;

        private List<NrrInfo> _nrrInfos;
        private List<NroInfo> _nroInfos;

        private long _ownerPid;
        private int _ownerProcessHandle;

        private static Random _random = new Random();

        public IRoInterface(ServiceCtx context)
        {
            _nrrInfos = new List<NrrInfo>(MaxNrr);
            _nroInfos = new List<NroInfo>(MaxNro);
        }

        private ResultCode ParseNrr(out NrrInfo nrrInfo, ServiceCtx context, long nrrAddress, long nrrSize)
        {
            nrrInfo = null;

            if (nrrSize == 0 || nrrAddress + nrrSize <= nrrAddress || (nrrSize & 0xFFF) != 0)
            {
                return ResultCode.InvalidSize;
            }
            else if ((nrrAddress & 0xFFF) != 0)
            {
                return ResultCode.InvalidAddress;
            }

            var memory = KernelStatic.GetAddressSpace(_ownerProcessHandle);

            StructReader reader = new StructReader(memory, nrrAddress);
            NrrHeader    header = reader.Read<NrrHeader>();

            if (header.Magic != NrrMagic)
            {
                return ResultCode.InvalidNrr;
            }
            else if (header.NrrSize != nrrSize)
            {
                return ResultCode.InvalidSize;
            }

            List<byte[]> hashes = new List<byte[]>();

            for (int i = 0; i < header.HashCount; i++)
            {
                byte[] temp = new byte[0x20];

                memory.Read((ulong)(nrrAddress + header.HashOffset + (i * 0x20)), temp);

                hashes.Add(temp);
            }

            nrrInfo = new NrrInfo(nrrAddress, header, hashes);

            return ResultCode.Success;
        }

        public bool IsNroHashPresent(byte[] nroHash)
        {
            foreach (NrrInfo info in _nrrInfos)
            {
                foreach (byte[] hash in info.Hashes)
                {
                    if (hash.SequenceEqual(nroHash))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsNroLoaded(byte[] nroHash)
        {
            foreach (NroInfo info in _nroInfos)
            {
                if (info.Hash.SequenceEqual(nroHash))
                {
                    return true;
                }
            }

            return false;
        }

        public ResultCode ParseNro(out NroInfo res, ServiceCtx context, ulong nroAddress, ulong nroSize, ulong bssAddress, ulong bssSize)
        {
            res = null;

            if (_nroInfos.Count >= MaxNro)
            {
                return ResultCode.TooManyNro;
            }
            else if (nroSize == 0 || nroAddress + nroSize <= nroAddress || (nroSize & 0xFFF) != 0)
            {
                return ResultCode.InvalidSize;
            }
            else if (bssSize != 0 && bssAddress + bssSize <= bssAddress)
            {
                return ResultCode.InvalidSize;
            }
            else if ((nroAddress & 0xFFF) != 0)
            {
                return ResultCode.InvalidAddress;
            }

            var memory = KernelStatic.GetAddressSpace(_ownerProcessHandle);

            uint magic       = memory.Read<uint>(nroAddress + 0x10);
            uint nroFileSize = memory.Read<uint>(nroAddress + 0x18);

            if (magic != NroMagic || nroSize != nroFileSize)
            {
                return ResultCode.InvalidNro;
            }

            byte[] nroData = new byte[nroSize];

            memory.Read(nroAddress, nroData);

            byte[] nroHash = null;

            MemoryStream stream = new MemoryStream(nroData);

            using (SHA256 hasher = SHA256.Create())
            {
                nroHash = hasher.ComputeHash(stream);
            }

            if (!IsNroHashPresent(nroHash))
            {
                return ResultCode.NotRegistered;
            }

            if (IsNroLoaded(nroHash))
            {
                return ResultCode.AlreadyLoaded;
            }

            stream.Position = 0;

            NroExecutable nro = new NroExecutable(stream.AsStorage(), nroAddress, bssAddress);

            // Check if everything is page align.
            if ((nro.Text.Length & 0xFFF) != 0 || (nro.Ro.Length & 0xFFF) != 0 ||
                (nro.Data.Length & 0xFFF) != 0 || (nro.BssSize & 0xFFF)   != 0)
            {
                return ResultCode.InvalidNro;
            }

            // Check if everything is contiguous.
            if (nro.RoOffset   != nro.TextOffset + nro.Text.Length ||
                nro.DataOffset != nro.RoOffset   + nro.Ro.Length   ||
                nroFileSize    != nro.DataOffset + nro.Data.Length)
            {
                return ResultCode.InvalidNro;
            }

            // Check the bss size match.
            if ((ulong)nro.BssSize != bssSize)
            {
                return ResultCode.InvalidNro;
            }

            int totalSize = nro.Text.Length + nro.Ro.Length + nro.Data.Length + nro.BssSize;

            // Apply patches
            context.Device.FileSystem.ModLoader.ApplyNroPatches(nro);

            res = new NroInfo(
                nro,
                nroHash,
                nroAddress,
                nroSize,
                bssAddress,
                bssSize,
                (ulong)totalSize);

            return ResultCode.Success;
        }

        private ResultCode MapNro(int processHandle, NroInfo info, out ulong nroMappedAddress)
        {
            int retryCount = 0;

            nroMappedAddress = 0;

            while (retryCount++ < MaxMapRetries)
            {
                ResultCode result = MapCodeMemoryInProcess(processHandle, info.NroAddress, info.NroSize, out nroMappedAddress);

                if (result != ResultCode.Success)
                {
                    return result;
                }

                if (info.BssSize > 0)
                {
                    KernelResult bssMappingResult = KernelStatic.Syscall.MapProcessCodeMemory(
                        processHandle,
                        nroMappedAddress + info.NroSize,
                        info.BssAddress,
                        info.BssSize);

                    if (bssMappingResult == KernelResult.InvalidMemState)
                    {
                        KernelStatic.Syscall.UnmapProcessCodeMemory(processHandle, nroMappedAddress + info.NroSize, info.BssAddress, info.BssSize);
                        KernelStatic.Syscall.UnmapProcessCodeMemory(processHandle, nroMappedAddress, info.NroAddress, info.NroSize);

                        continue;
                    }
                    else if (bssMappingResult != KernelResult.Success)
                    {
                        KernelStatic.Syscall.UnmapProcessCodeMemory(processHandle, nroMappedAddress + info.NroSize, info.BssAddress, info.BssSize);
                        KernelStatic.Syscall.UnmapProcessCodeMemory(processHandle, nroMappedAddress, info.NroAddress, info.NroSize);

                        return (ResultCode)bssMappingResult;
                    }
                }

                if (CanAddGuardRegionsInProcess(processHandle, nroMappedAddress, info.TotalSize))
                {
                    return ResultCode.Success;
                }
            }

            return ResultCode.InsufficientAddressSpace;
        }

        private bool CanAddGuardRegionsInProcess(int processHandle, ulong baseAddress, ulong size)
        {
            KernelStatic.Syscall.QueryProcessMemory(out var memInfo, processHandle, baseAddress - 1);

            if (memInfo.State == 0 && baseAddress - GuardPagesSize >= memInfo.Address)
            {
                KernelStatic.Syscall.QueryProcessMemory(out memInfo, processHandle, baseAddress + size);

                if (memInfo.State == 0)
                {
                    return baseAddress + size + GuardPagesSize <= memInfo.Address + memInfo.Size;
                }
            }
            return false;
        }

        private ResultCode MapCodeMemoryInProcess(int processHandle, ulong baseAddress, ulong size, out ulong targetAddress)
        {
            targetAddress = 0;

            int retryCount;

            Map.GetAddressSpaceInfo(out var info, processHandle);

            ulong addressSpacePageLimit = (info.Aslr.Size - size) >> 12;

            for (retryCount = 0; retryCount < MaxMapRetries; retryCount++)
            {
                while (true)
                {
                    ulong randomOffset = (ulong)(uint)_random.Next(0, (int)addressSpacePageLimit) << 12;

                    targetAddress = info.Aslr.Base + randomOffset;

                    if (info.Heap.Size != 0 && (info.Heap.Base <= targetAddress + size - 1 && targetAddress <= info.Heap.End - 1))
                    {
                        continue;
                    }

                    if (info.Alias.Size != 0 && (info.Alias.Base <= targetAddress + size - 1 && targetAddress <= info.Alias.End - 1))
                    {
                        continue;
                    }

                    break;
                }

                KernelResult result = KernelStatic.Syscall.MapProcessCodeMemory(processHandle, targetAddress, baseAddress, size);

                if (result == KernelResult.InvalidMemState)
                {
                    continue;
                }
                else if (result != KernelResult.Success)
                {
                    return (ResultCode)result;
                }

                if (!CanAddGuardRegionsInProcess(processHandle, targetAddress, size))
                {
                    continue;
                }

                return ResultCode.Success;
            }

            if (retryCount == MaxMapRetries)
            {
                return ResultCode.InsufficientAddressSpace;
            }

            return ResultCode.Success;
        }

        private KernelResult SetNroMemoryPermissions(int processHandle, IExecutable relocatableObject, ulong baseAddress)
        {
            ulong textStart = baseAddress + (ulong)relocatableObject.TextOffset;
            ulong roStart   = baseAddress + (ulong)relocatableObject.RoOffset;
            ulong dataStart = baseAddress + (ulong)relocatableObject.DataOffset;

            ulong bssStart = dataStart + (ulong)relocatableObject.Data.Length;

            ulong bssEnd = BitUtils.AlignUp(bssStart + (ulong)relocatableObject.BssSize, KMemoryManager.PageSize);

            KernelResult result;

            result = KernelStatic.Syscall.SetProcessMemoryPermission(processHandle, textStart, roStart - textStart, KMemoryPermission.ReadAndExecute);

            if (result != KernelResult.Success)
            {
                return result;
            }

            result = KernelStatic.Syscall.SetProcessMemoryPermission(processHandle, roStart, dataStart - roStart, KMemoryPermission.Read);

            if (result != KernelResult.Success)
            {
                return result;
            }

            return KernelStatic.Syscall.SetProcessMemoryPermission(processHandle, dataStart, bssEnd - dataStart, KMemoryPermission.ReadAndWrite);
        }

        private ResultCode RemoveNrrInfo(long nrrAddress)
        {
            foreach (NrrInfo info in _nrrInfos)
            {
                if (info.NrrAddress == nrrAddress)
                {
                    _nrrInfos.Remove(info);

                    return ResultCode.Success;
                }
            }

            return ResultCode.NotLoaded;
        }

        private ResultCode RemoveNroInfo(ulong nroMappedAddress)
        {
            foreach (NroInfo info in _nroInfos)
            {
                if (info.NroMappedAddress == nroMappedAddress)
                {
                    _nroInfos.Remove(info);

                    return UnmapNroFromInfo(_ownerProcessHandle, info);
                }
            }

            return ResultCode.NotLoaded;
        }

        private ResultCode UnmapNroFromInfo(int processHandle, NroInfo info)
        {
            ulong textSize = (ulong)info.Executable.Text.Length;
            ulong roSize   = (ulong)info.Executable.Ro.Length;
            ulong dataSize = (ulong)info.Executable.Data.Length;
            ulong bssSize  = (ulong)info.Executable.BssSize;

            KernelResult result = KernelResult.Success;

            if (info.Executable.BssSize != 0)
            {
                result = KernelStatic.Syscall.UnmapProcessCodeMemory(
                    processHandle,
                    info.NroMappedAddress + textSize + roSize + dataSize,
                    info.Executable.BssAddress,
                    bssSize);
            }

            if (result == KernelResult.Success)
            {
                result = KernelStatic.Syscall.UnmapProcessCodeMemory(
                    processHandle,
                    info.NroMappedAddress + textSize + roSize,
                    info.Executable.SourceAddress + textSize + roSize,
                    dataSize);

                if (result == KernelResult.Success)
                {
                    result = KernelStatic.Syscall.UnmapProcessCodeMemory(
                        processHandle,
                        info.NroMappedAddress,
                        info.Executable.SourceAddress,
                        textSize + roSize);
                }
            }

            return (ResultCode)result;
        }

        private ResultCode IsInitialized(long pid)
        {
            if (_ownerPid != 0 && _ownerPid == pid)
            {
                return ResultCode.Success;
            }

            return ResultCode.InvalidProcess;
        }

        [Command(0)]
        // LoadNro(u64, u64, u64, u64, u64, pid) -> u64
        public ResultCode LoadNro(ServiceCtx context)
        {
            ResultCode result = IsInitialized(context.Request.HandleDesc.PId);

            // Zero
            context.RequestData.ReadUInt64();

            ulong nroHeapAddress = context.RequestData.ReadUInt64();
            ulong nroSize        = context.RequestData.ReadUInt64();
            ulong bssHeapAddress = context.RequestData.ReadUInt64();
            ulong bssSize        = context.RequestData.ReadUInt64();

            ulong nroMappedAddress = 0;

            if (result == ResultCode.Success)
            {
                result = ParseNro(out NroInfo info, context, nroHeapAddress, nroSize, bssHeapAddress, bssSize);

                if (result == ResultCode.Success)
                {
                    result = MapNro(_ownerProcessHandle, info, out nroMappedAddress);

                    if (result == ResultCode.Success)
                    {
                        result = (ResultCode)SetNroMemoryPermissions(_ownerProcessHandle, info.Executable, nroMappedAddress);

                        if (result == ResultCode.Success)
                        {
                            info.NroMappedAddress = nroMappedAddress;

                            _nroInfos.Add(info);
                        }
                    }
                }
            }

            context.ResponseData.Write(nroMappedAddress);

            return result;
        }

        [Command(1)]
        // UnloadNro(u64, u64, pid)
        public ResultCode UnloadNro(ServiceCtx context)
        {
            ResultCode result = IsInitialized(context.Request.HandleDesc.PId);

            // Zero
            context.RequestData.ReadUInt64();

            ulong nroMappedAddress = context.RequestData.ReadUInt64();

            if (result == ResultCode.Success)
            {
                if ((nroMappedAddress & 0xFFF) != 0)
                {
                    return ResultCode.InvalidAddress;
                }

                result = RemoveNroInfo(nroMappedAddress);
            }

            return result;
        }

        [Command(2)]
        // LoadNrr(u64, u64, u64, pid)
        public ResultCode LoadNrr(ServiceCtx context)
        {
            ResultCode result = IsInitialized(context.Request.HandleDesc.PId);

            // pid placeholder, zero
            context.RequestData.ReadUInt64();

            long nrrAddress = context.RequestData.ReadInt64();
            long nrrSize    = context.RequestData.ReadInt64();

            if (result == ResultCode.Success)
            {
                NrrInfo info;
                result = ParseNrr(out info, context, nrrAddress, nrrSize);

                if (result == ResultCode.Success)
                {
                    if (_nrrInfos.Count >= MaxNrr)
                    {
                        result = ResultCode.NotLoaded;
                    }
                    else
                    {
                        _nrrInfos.Add(info);
                    }
                }
            }

            return result;
        }

        [Command(3)]
        // UnloadNrr(u64, u64, pid)
        public ResultCode UnloadNrr(ServiceCtx context)
        {
            ResultCode result = IsInitialized(context.Request.HandleDesc.PId);

            // pid placeholder, zero
            context.RequestData.ReadUInt64();

            long nrrHeapAddress = context.RequestData.ReadInt64();

            if (result == ResultCode.Success)
            {
                if ((nrrHeapAddress & 0xFFF) != 0)
                {
                    return ResultCode.InvalidAddress;
                }

                result = RemoveNrrInfo(nrrHeapAddress);
            }

            return result;
        }

        [Command(4)]
        // Initialize(u64, pid, handle<copy>)
        public ResultCode Initialize(ServiceCtx context)
        {
            if (_ownerPid != 0)
            {
                return ResultCode.InvalidSession;
            }

            _ownerPid = context.Request.HandleDesc.PId;
            _ownerProcessHandle = context.Request.HandleDesc.ToCopy[0];

            return ResultCode.Success;
        }

        public void Dispose()
        {
            foreach (NroInfo info in _nroInfos)
            {
                UnmapNroFromInfo(_ownerProcessHandle, info);
            }

            _nroInfos.Clear();

            KernelStatic.Syscall.CloseHandle(_ownerProcessHandle);
        }
    }
}
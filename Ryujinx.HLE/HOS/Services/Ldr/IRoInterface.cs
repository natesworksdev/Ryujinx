using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.Loaders.Executables;
using Ryujinx.HLE.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using static Ryujinx.HLE.HOS.ErrorCode;

namespace Ryujinx.HLE.HOS.Services.Ldr
{
    [StructLayout(LayoutKind.Explicit, Size = 0x350)]
    unsafe struct NrrHeader
    {
        [FieldOffset(0)]
        public uint  Magic;

        [FieldOffset(0x10)]
        public ulong TitleIdMask;

        [FieldOffset(0x18)]
        public ulong TitleIdPattern;

        [FieldOffset(0x30)]
        public fixed byte Modulus[0x100];

        [FieldOffset(0x130)]
        public fixed byte FixedKeySignature[0x100];

        [FieldOffset(0x230)]
        public fixed byte NrrSignature[0x100];

        [FieldOffset(0x330)]
        public ulong TitleIdMin;

        [FieldOffset(0x338)]
        public uint  NrrSize;

        [FieldOffset(0x340)]
        public uint HashOffset;

        [FieldOffset(0x344)]
        public uint HashCount;
    }

    class NrrInfo
    {
        public NrrHeader    Header     { get; private set; }
        public List<byte[]> Hashes     { get; private set; }
        public long         NrrAddress { get; private set; }

        public NrrInfo(long nrrAddress, NrrHeader header, List<byte[]> hashes)
        {
            this.NrrAddress = nrrAddress;
            this.Header     = header;
            this.Hashes     = hashes;
        }
    }

    class NroInfo
    {
        public Nro    Executable       { get; private set; }
        public byte[] Hash             { get; private set; }
        public long   NroAddress       { get; private set; }
        public long   TotalSize        { get; private set; }
        public long   NroMappedAddress { get; set; }

        public NroInfo(Nro executable, byte[] hash, long totalSize)
        {
            this.Executable = executable;
            this.Hash       = hash;
            this.TotalSize = totalSize;
        }
    }

    class RoInterface : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        private const int MaxNrr = 0x40;
        private const int MaxNro = 0x40;

        private const uint NrrMagic = 0x3052524E;
        private const uint NroMagic = 0x304F524E;

        private List<NrrInfo> _nrrInfos;
        private List<NroInfo> _nroInfos;

        private bool _isInitialized;

        public RoInterface()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, LoadNro    },
                { 1, UnloadNro  },
                { 2, LoadNrr    },
                { 3, UnloadNrr  },
                { 4, Initialize },
            };

            _nrrInfos = new List<NrrInfo>(MaxNrr);
            _nroInfos = new List<NroInfo>(MaxNro);
        }

        private long ParseNrr(out NrrInfo nrrInfo, ServiceCtx context, long nrrAddress, long nrrSize)
        {
            nrrInfo = null;

            if (nrrSize == 0 || nrrAddress + nrrSize <= nrrAddress || (nrrSize & 0xFFF) != 0)
            {
                return MakeError(ErrorModule.Loader, LoaderErr.BadSize);
            }
            else if ((nrrAddress & 0xFFF) != 0)
            {
                return MakeError(ErrorModule.Loader, LoaderErr.UnalignedAddress);
            }

            StructReader reader = new StructReader(context.Memory, nrrAddress);
            NrrHeader    header = reader.Read<NrrHeader>();

            if (header.Magic != NrrMagic)
            {
                return MakeError(ErrorModule.Loader, LoaderErr.InvalidNrr);
            }
            else if (header.NrrSize != nrrSize)
            {
                return MakeError(ErrorModule.Loader, LoaderErr.BadSize);
            }

            List<byte[]> hashes = new List<byte[]>();

            for (int i = 0; i < header.HashCount; i++)
            {
                hashes.Add(context.Memory.ReadBytes(nrrAddress + header.HashOffset + (i * 0x20), 0x20));
            }

            nrrInfo = new NrrInfo(nrrAddress, header, hashes);

            return 0;
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

        public long ParseNro(out NroInfo res, ServiceCtx context, long nroHeapAddress, long nroSize, long bssHeapAddress, long bssSize)
        {
            res = null;

            if (_nroInfos.Count >= MaxNro)
            {
                return MakeError(ErrorModule.Loader, LoaderErr.MaxNro);
            }
            else if (nroSize == 0 || nroHeapAddress + nroSize <= nroHeapAddress || (nroSize & 0xFFF) != 0)
            {
                return MakeError(ErrorModule.Loader, LoaderErr.BadSize);
            }
            else if (bssSize != 0 && (bssHeapAddress + bssSize) <= bssHeapAddress)
            {
                return MakeError(ErrorModule.Loader, LoaderErr.BadSize);
            }
            else if ((nroHeapAddress & 0xFFF) != 0)
            {
                return MakeError(ErrorModule.Loader, LoaderErr.UnalignedAddress);
            }

            uint magic       = context.Memory.ReadUInt32(nroHeapAddress + 0x10);
            uint nroFileSize = context.Memory.ReadUInt32(nroHeapAddress + 0x18);

            if (magic != NroMagic || nroSize != nroFileSize)
            {
                return MakeError(ErrorModule.Loader, LoaderErr.InvalidNro);
            }

            byte[] nroData = context.Memory.ReadBytes(nroHeapAddress, nroSize);
            byte[] nroHash = null;

            MemoryStream stream = new MemoryStream(nroData);

            using (SHA256 hasher = SHA256.Create())
            {
                nroHash = hasher.ComputeHash(stream);
            }

            if (!IsNroHashPresent(nroHash))
            {
                return MakeError(ErrorModule.Loader, LoaderErr.NroHashNotPresent);
            }

            if (IsNroLoaded(nroHash))
            {
                return MakeError(ErrorModule.Loader, LoaderErr.NroAlreadyLoaded);
            }

            stream.Position = 0;

            Nro executable = new Nro(stream, "memory", nroHeapAddress, bssHeapAddress);

            // check if everything is page align.
            if ((executable.Text.Length & 0xFFF) != 0 || (executable.Ro.Length & 0xFFF) != 0
                || (executable.Data.Length & 0xFFF) != 0 || (executable.BssSize & 0xFFF) !=  0)
            {
                return MakeError(ErrorModule.Loader, LoaderErr.InvalidNro);
            }

            // check if everything is contiguous.
            if (executable.RoOffset != executable.TextOffset + executable.Text.Length
                || executable.DataOffset != executable.RoOffset + executable.Ro.Length
                || nroFileSize != executable.DataOffset + executable.Data.Length)
            {
                return MakeError(ErrorModule.Loader, LoaderErr.InvalidNro);
            }

            // finally check the bss size match.
            if (executable.BssSize != bssSize)
            {
                return MakeError(ErrorModule.Loader, LoaderErr.InvalidNro);
            }

            res = new NroInfo(executable, nroHash, executable.Text.Length + executable.Ro.Length + executable.Data.Length + executable.BssSize);

            return 0;
        }

        private long MapNro(ServiceCtx context, NroInfo info, out long nroMappedAddress)
        {
            nroMappedAddress = 0;
            long targetAddress = context.Process.MemoryManager.AddrSpaceStart;

            long heapRegionStart = context.Process.MemoryManager.HeapRegionStart;
            long heapRegionEnd   = context.Process.MemoryManager.HeapRegionEnd;

            long mapRegionStart = context.Process.MemoryManager.MapRegionStart;
            long mapRegionEnd   = context.Process.MemoryManager.MapRegionEnd;

            while (true)
            {
                if (targetAddress + info.TotalSize >= context.Process.MemoryManager.AddrSpaceEnd)
                {
                    return MakeError(ErrorModule.Loader, LoaderErr.InvalidMemoryState);
                }

                bool isValidAddress = !(heapRegionStart > 0 && heapRegionStart <= targetAddress + info.TotalSize - 1
                    && targetAddress <= heapRegionEnd - 1)
                    && !(mapRegionStart > 0
                    && mapRegionStart <= targetAddress + info.TotalSize - 1
                    && targetAddress <= mapRegionEnd - 1);

                if (isValidAddress && context.Process.MemoryManager.HleIsUnmapped(targetAddress, info.TotalSize))
                {
                    break;
                }

                targetAddress += 0x1000;
            }

            context.Process.LoadProgram(info.Executable, targetAddress);

            info.NroMappedAddress = targetAddress;
            nroMappedAddress      = targetAddress;

            return 0;
        }

        private long RemoveNrrInfo(long nrrAddress)
        {
            foreach (NrrInfo info in _nrrInfos)
            {
                if (info.NrrAddress == nrrAddress)
                {
                    _nrrInfos.Remove(info);

                    return 0;
                }
            }

            return MakeError(ErrorModule.Loader, LoaderErr.BadNrrAddress);
        }

        private long RemoveNroInfo(ServiceCtx context, long nroMappedAddress, long nroHeapAddress)
        {
            foreach (NroInfo info in _nroInfos)
            {
                if (info.NroMappedAddress == nroMappedAddress && info.Executable.SourceAddress == nroHeapAddress)
                {
                    _nroInfos.Remove(info);

                    context.Process.RemoveProgram(info.NroMappedAddress);

                    long result = context.Process.MemoryManager.UnmapProcessCodeMemory(info.NroMappedAddress, info.Executable.SourceAddress, info.TotalSize - info.Executable.BssSize);

                    if (result == 0 && info.Executable.BssSize != 0)
                    {
                        result = context.Process.MemoryManager.UnmapProcessCodeMemory(info.NroMappedAddress + info.TotalSize - info.Executable.BssSize, info.Executable.BssAddress, info.Executable.BssSize);
                    }

                    return result;
                }
            }

            return MakeError(ErrorModule.Loader, LoaderErr.BadNroAddress);
        }

        // LoadNro(u64, u64, u64, u64, u64, pid) -> u64
        public long LoadNro(ServiceCtx context)
        {
            long result = MakeError(ErrorModule.Loader, LoaderErr.BadInitialization);

            // Zero
            context.RequestData.ReadUInt64();

            long nroHeapAddress = context.RequestData.ReadInt64();
            long nroSize        = context.RequestData.ReadInt64();
            long bssHeapAddress = context.RequestData.ReadInt64();
            long bssSize        = context.RequestData.ReadInt64();

            long nroMappedAddress = 0;

            if (_isInitialized)
            {
                NroInfo info;

                result = ParseNro(out info, context, nroHeapAddress, nroSize, bssHeapAddress, bssSize);

                if (result == 0)
                {
                    result = MapNro(context, info, out nroMappedAddress);

                    if (result == 0)
                    {
                        _nroInfos.Add(info);
                    }
                }
            }

            context.ResponseData.Write(nroMappedAddress);

            return result;
        }

        // UnloadNro(u64, u64, pid)
        public long UnloadNro(ServiceCtx context)
        {
            long result = MakeError(ErrorModule.Loader, LoaderErr.BadInitialization);

            long nroMappedAddress = context.RequestData.ReadInt64();
            long nroHeapAddress   = context.RequestData.ReadInt64();

            if (_isInitialized)
            {
                if ((nroMappedAddress & 0xFFF) != 0 || (nroHeapAddress & 0xFFF) != 0)
                {
                    return MakeError(ErrorModule.Loader, LoaderErr.UnalignedAddress);
                }

                result = RemoveNroInfo(context, nroMappedAddress, nroHeapAddress);
            }

            return result;
        }

        // LoadNrr(u64, u64, u64, pid)
        public long LoadNrr(ServiceCtx context)
        {
            long result = MakeError(ErrorModule.Loader, LoaderErr.BadInitialization);

            // Zero
            context.RequestData.ReadUInt64();

            long nrrAddress = context.RequestData.ReadInt64();
            long nrrSize    = context.RequestData.ReadInt64();

            if (_isInitialized)
            {
                NrrInfo info;
                result = ParseNrr(out info, context, nrrAddress, nrrSize);

                if(result == 0)
                {
                    if (_nrrInfos.Count >= MaxNrr)
                    {
                        result = MakeError(ErrorModule.Loader, LoaderErr.MaxNrr);
                    }
                    else
                    {
                        _nrrInfos.Add(info);
                    }
                }
            }

            return result;
        }

        // UnloadNrr(u64, u64, pid)
        public long UnloadNrr(ServiceCtx context)
        {
            long result = MakeError(ErrorModule.Loader, LoaderErr.BadInitialization);

            // Zero
            context.RequestData.ReadUInt64();

            long nrrHeapAddress = context.RequestData.ReadInt64();

            if (_isInitialized)
            {
                if ((nrrHeapAddress & 0xFFF) != 0)
                {
                    return MakeError(ErrorModule.Loader, LoaderErr.UnalignedAddress);
                }

                result = RemoveNrrInfo(nrrHeapAddress);
            }

            return result;
        }

        // Initialize(u64, pid, KObject)
        public long Initialize(ServiceCtx context)
        {
            // TODO: we actually ignore the pid and process handle receive, we will need to use them when we will have multi process support.
            _isInitialized = true;

            return 0;
        }
    }
}

using Ryujinx.Common.Utilities;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Mii.Types
{
    [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 0x1A98)]
    struct NintendoFigurineDatabase
    {
        private const int DatabaseMagic = ('N' << 0) | ('F' << 8) | ('D' << 16) | ('B' << 24);
        private const byte MaxMii = 100;
        private const byte CurrentVersion = 1;

        private const int FigurineArraySize = MaxMii * StoreData.Size;

        private uint _magic;

        private FigurineStorageStruct _figurineStorage;

        private byte _version;
        private byte _figurineCount;
        private ushort _crc;

        // Set to true to allow fixing database with invalid storedata device crc instead of deleting them.
        private const bool AcceptInvalidDeviceCrc = true;

        public int Length => _figurineCount;

        [StructLayout(LayoutKind.Sequential, Size = FigurineArraySize)]
        private struct FigurineStorageStruct { }

        private Span<StoreData> Figurines => SpanHelpers.AsSpan<FigurineStorageStruct, StoreData>(ref _figurineStorage);
        
        public StoreData Get(int index)
        {
            return Figurines[index];
        }

        public bool IsFull()
        {
            return Length >= MaxMii;
        }

        public bool GetIndexByCreatorId(out int index, CreateId createId)
        {
            for (int i = 0; i < Length; i++)
            {
                if (Figurines[i].CreateId == createId)
                {
                    index = i;

                    return true;
                }
            }

            index = -1;

            return false;
        }

        public ResultCode Move(int newIndex, int oldIndex)
        {
            if (newIndex == oldIndex)
            {
                return ResultCode.NotUpdated;
            }

            StoreData tmp = Figurines[oldIndex];

            int targetLength;
            int sourceIndex;
            int destinationIndex;

            if (newIndex < oldIndex)
            {
                targetLength     = oldIndex - newIndex;
                sourceIndex      = newIndex;
                destinationIndex = newIndex + 1;
            }
            else
            {
                targetLength     = newIndex - oldIndex;
                sourceIndex      = oldIndex + 1;
                destinationIndex = oldIndex;
            }

            Figurines.Slice(sourceIndex, targetLength).CopyTo(Figurines.Slice(destinationIndex, targetLength));

            Figurines[newIndex] = tmp;

            UpdateCrc();

            return ResultCode.Success;
        }

        public void Replace(int index, StoreData storeData)
        {
            Figurines[index] = storeData;

            UpdateCrc();
        }

        public void Add(StoreData storeData)
        {
            Replace(_figurineCount++, storeData);
        }

        public void Delete(int index)
        {
            // If this isn't the only element in the list, move the data in it.
            if (_figurineCount - 1 > index)
            {
                int targetLength     = _figurineCount - index;
                int sourceIndex      = index + 1;
                int destinationIndex = index;

                Figurines.Slice(sourceIndex, targetLength).CopyTo(Figurines.Slice(destinationIndex, targetLength));
            }

            _figurineCount--;

            UpdateCrc();
        }

        public bool FixDatabase()
        {
            bool isBroken = false;

            for (int i = 0; i < Length; i++)
            {
                if (!Figurines[i].IsValid())
                {
                    // If the device crc is the only part invalid, we fix it (This is useful to allow importing arbitrary database in Ryujinx)
                    if (AcceptInvalidDeviceCrc && Figurines[i].CoreData.IsValid() && Figurines[i].IsValidDataCrc())
                    {
                        Figurines[i].UpdateCrc();

                        UpdateCrc();
                    }
                    else
                    {
                        Delete(i);

                        isBroken = true;
                    }
                }
            }

            return isBroken;
        }

        public ResultCode Verify()
        {
            if (_magic != DatabaseMagic)
            {
                return ResultCode.InvalidDatabaseMagic;
            }

            if (_version != CurrentVersion)
            {
                return ResultCode.InvalidDatabaseVersion;
            }

            if (!IsValidCrc())
            {
                return ResultCode.InvalidCrc;
            }

            if (_figurineCount > 100)
            {
                return ResultCode.InvalidDatabaseSize;
            }

            return ResultCode.Success;
        }

        public void Format()
        {
            _magic         = DatabaseMagic;
            _version       = CurrentVersion;
            _figurineCount = 0;

            // Fill with empty data
            Figurines.Fill(new StoreData());

            UpdateCrc();
        }

        public void CorruptDatabase()
        {
            UpdateCrc();

            _crc = (ushort)~_crc;
        }

        private void UpdateCrc()
        {
            _crc = CalculateCrc();
        }

        public bool IsValidCrc()
        {
            return _crc == CalculateCrc();
        }

        private ushort CalculateCrc()
        {
            return Helper.CalculateCrc16BE(AsSpanWithoutCrc());
        }

        public Span<byte> AsSpan()
        {
            return SpanHelpers.AsByteSpan(ref this);
        }

        public ReadOnlySpan<byte> AsReadOnlySpan()
        {
            return SpanHelpers.AsReadOnlyByteSpan(ref this);
        }

        private ReadOnlySpan<byte> AsSpanWithoutCrc()
        {
            return AsReadOnlySpan().Slice(0, Unsafe.SizeOf<NintendoFigurineDatabase>() - 2);
        }
    }
}

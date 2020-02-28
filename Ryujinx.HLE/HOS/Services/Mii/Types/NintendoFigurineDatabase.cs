using Ryujinx.Common.Utilities;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Mii.Types
{
    [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 0x1A98)]
    unsafe struct NintendoFigurineDatabase
    {
        private const int DatabaseMagic = ('N' << 0) | ('F' << 8) | ('D' << 16) | ('B' << 24);
        private const byte MaxMii = 100;
        private const byte CurrentVersion = 1;

        private const int FigurineArraySize = MaxMii * StoreData.Size;

        private uint _magic;

        private fixed byte _figurinesStorage[FigurineArraySize];

        private byte _version;
        private byte _figurineCount;
        private ushort _crc;

        // Set to true to allow fixing database with invalid storedata device crc instead of deleting them.
        private const bool AcceptInvalidDeviceCrc = true;

        public int Length => _figurineCount;

        private Span<StoreData> _figurines
        {
            get
            {
                unsafe
                {
                    fixed (byte *storage = _figurinesStorage)
                    {
                        return new Span<StoreData>(storage, MaxMii);
                    }
                }
            }
        }

        public StoreData Get(int index)
        {
            return _figurines[index];
        }

        public bool IsFull()
        {
            return Length >= MaxMii;
        }

        public bool GetIndexByCreatorId(out int index, CreateId createId)
        {
            for (int i = 0; i < Length; i++)
            {
                if (_figurines[i].CreateId == createId)
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

            StoreData tmp = _figurines[oldIndex];

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

            _figurines.Slice(sourceIndex, targetLength).CopyTo(_figurines.Slice(destinationIndex, targetLength));

            _figurines[newIndex] = tmp;

            UpdateCrc();

            return ResultCode.Success;
        }

        public void Replace(int index, StoreData storeData)
        {
            _figurines[index] = storeData;

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

                _figurines.Slice(sourceIndex, targetLength).CopyTo(_figurines.Slice(destinationIndex, targetLength));
            }

            _figurineCount--;

            UpdateCrc();
        }

        public bool FixDatabase()
        {
            bool isBroken = false;

            while (true)
            {
                int i = 0;

                for (i = 0; i < Length; i++)
                {
                    if (!_figurines[i].IsValid())
                    {
                        // If the device crc is the only part invalid, we fix it (This is useful to allow importing arbitrary database in Ryujinx)
                        if (AcceptInvalidDeviceCrc && _figurines[i].CoreData.IsValid() && _figurines[i].IsValidDataCrc())
                        {
                            _figurines[i].UpdateCrc();

                            UpdateCrc();
                        }
                        else
                        {
                            Delete(i);

                            isBroken = true;
                        }

                        break;
                    }
                }

                if (i == Length)
                {
                    break;
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
            _figurines.Fill(new StoreData());

            UpdateCrc();
        }

        public void CorruptDatabase()
        {
            UpdateCrc();

            _crc = (ushort)~_crc;
        }

        private void UpdateCrc()
        {
            _crc = CalculcateCrc();
        }

        public bool IsValidCrc()
        {
            return _crc == CalculcateCrc();
        }

        private ushort CalculcateCrc()
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

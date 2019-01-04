namespace ChocolArm64.Memory
{
    public interface IBus
    {
        byte ReadByte(ulong address);
        ushort ReadUInt16(ulong address);
        uint ReadUInt32(ulong address);
        ulong ReadUInt64(ulong address);

        void WriteByte(ulong address, byte value);
        void WriteUInt16(ulong address, ushort value);
        void WriteUInt32(ulong address, uint value);
        void WriteUInt64(ulong address, ulong value);
    }
}
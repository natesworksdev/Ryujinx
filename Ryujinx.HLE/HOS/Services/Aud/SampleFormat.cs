namespace Ryujinx.HLE.HOS.Services.Aud
{
    internal enum SampleFormat : byte
    {
        Invalid  = 0,
        PcmInt8  = 1,
        PcmInt16 = 2,
        PcmInt24 = 3,
        PcmInt32 = 4,
        PcmFloat = 5,
        Adpcm    = 6
    }
}
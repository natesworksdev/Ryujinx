namespace Ryujinx.HLE.OsHle.Services.Aud
{
    enum MemoryPoolStates : int
    {
        MPS_Invalid = 0x0,
        MPS_Unknown = 0x1,
        MPS_RequestDetatch = 0x2,
        MPS_Detatched = 0x3,
        MPS_RequestAttach = 0x4,
        MPS_Attached = 0x5,
        MPS_Released = 0x6
    }
}

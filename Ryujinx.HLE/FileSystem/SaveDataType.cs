namespace Ryujinx.HLE.FileSystem
{
    internal enum SaveDataType : byte
    {
        SystemSaveData,
        SaveData,
        BcatDeliveryCacheStorage,
        DeviceSaveData,
        TemporaryStorage,
        CacheStorage
    }
}

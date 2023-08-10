using System;

namespace Ryujinx.Ava.Common.SaveManager
{
    [Flags]
    public enum SaveOptions
    {
        // Save Data Types
        SaveTypeAccount,
        SaveTypeBcat,
        SaveTypeDevice,
        SaveTypeAll = SaveTypeAccount | SaveTypeBcat | SaveTypeDevice,

        // Request Semantics -- Not Implemented
        SkipEmptyDirectories,
        FlattenSaveStructure,
        StopOnFirstFailure,
        UseDateInName,
        ObfuscateZipExtension,

        Default = SaveTypeAll
    }
}
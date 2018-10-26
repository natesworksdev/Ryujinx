namespace Ryujinx.Tests.Unicorn
{
    public enum UnicornError
    {
        UcErrOk = 0,             // No error: everything was fine
        UcErrNomem,              // Out-Of-Memory error: uc_open(), uc_emulate()
        UcErrArch,               // Unsupported architecture: uc_open()
        UcErrHandle,             // Invalid handle
        UcErrMode,               // Invalid/unsupported mode: uc_open()
        UcErrVersion,            // Unsupported version (bindings)
        UcErrReadUnmapped,      // Quit emulation due to READ on unmapped memory: uc_emu_start()
        UcErrWriteUnmapped,     // Quit emulation due to WRITE on unmapped memory: uc_emu_start()
        UcErrFetchUnmapped,     // Quit emulation due to FETCH on unmapped memory: uc_emu_start()
        UcErrHook,               // Invalid hook type: uc_hook_add()
        UcErrInsnInvalid,       // Quit emulation due to invalid instruction: uc_emu_start()
        UcErrMap,                // Invalid memory mapping: uc_mem_map()
        UcErrWriteProt,         // Quit emulation due to UC_MEM_WRITE_PROT violation: uc_emu_start()
        UcErrReadProt,          // Quit emulation due to UC_MEM_READ_PROT violation: uc_emu_start()
        UcErrFetchProt,         // Quit emulation due to UC_MEM_FETCH_PROT violation: uc_emu_start()
        UcErrArg,                // Inavalid argument provided to uc_xxx function (See specific function API)
        UcErrReadUnaligned,     // Unaligned read
        UcErrWriteUnaligned,    // Unaligned write
        UcErrFetchUnaligned,    // Unaligned fetch
        UcErrHookExist,         // hook for this event already existed
        UcErrResource,           // Insufficient resource: uc_emu_start()
        UcErrException           // Unhandled CPU exception
    }
}

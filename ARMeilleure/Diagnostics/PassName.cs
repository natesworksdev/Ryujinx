namespace ARMeilleure.Diagnostics
{
    enum PassName
    {
        Decoding,
        Translation,
        RegisterUsage,
        Dominance,
        SsaConstruction,
        RegisterToLocal,
        Optimization,
        PreAllocation,
        RegisterAllocation,
        CodeGeneration,

        Count
    }
}
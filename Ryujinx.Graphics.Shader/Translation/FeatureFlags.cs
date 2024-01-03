using System;

namespace Ryujinx.Graphics.Shader.Translation
{
    /// <summary>
    /// Features used by the shader that are important for the code generator to know in advance.
    /// These typically change the declarations in the shader header.
    /// </summary>
    [Flags]
    public enum FeatureFlags
    {
        None = 0,

        // Affected by resolution scaling.
        IntegerSampling = 1 << 0,
        FragCoordXY     = 1 << 1,

        Bindless = 1 << 2,
        InstanceId = 1 << 3,
        DrawParameters = 1 << 4,
        GlobalMemory = 1 << 5,
        GlobalMemoryWrite = 1 << 6,
        RtLayer = 1 << 7,
        CbIndexing = 1 << 8,
        IaIndexing = 1 << 9,
        OaIndexing = 1 << 10,
        FixedFuncAttr = 1 << 11
    }
}

namespace Ryujinx.Graphics.Shader.Decoders
{
    interface IOpCodeRegCbuf : IOpCode
    {
        int Offset { get; }
        int Slot   { get; }

        Register Rb { get; }
    }
}
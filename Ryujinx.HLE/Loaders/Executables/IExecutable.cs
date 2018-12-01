namespace Ryujinx.HLE.Loaders.Executables
{
    internal interface IExecutable
    {
        byte[] Text { get; }
        byte[] Ro   { get; }
        byte[] Data { get; }

        int TextOffset { get; }
        int RoOffset   { get; }
        int DataOffset { get; }
        int BssOffset  { get; }
        int BssSize    { get; }
    }
}
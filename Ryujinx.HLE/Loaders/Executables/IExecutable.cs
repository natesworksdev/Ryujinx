namespace Ryujinx.HLE.Loaders.Executables
{
    public interface IExecutable
    {
        string FilePath { get; }

        byte[] Text { get; }
        byte[] Ro   { get; }
        byte[] Data { get; }

        long SourceAddress { get; }
        long BssAddress    { get; }

        int Mod0Offset { get; }
        int TextOffset { get; }
        int RoOffset   { get; }
        int DataOffset { get; }
        int BssSize    { get; }
    }
}
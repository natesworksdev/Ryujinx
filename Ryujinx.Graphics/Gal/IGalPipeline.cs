namespace Ryujinx.Graphics.Gal
{
    public interface IGalPipeline
    {
        void Bind(GalPipelineState state);

        void ResetDepthMask();
        void ResetColorMask(int index);
    }
}
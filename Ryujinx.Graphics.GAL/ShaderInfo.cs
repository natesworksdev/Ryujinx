namespace Ryujinx.Graphics.GAL
{
    public struct ShaderInfo
    {
        public int FragmentOutputMap { get; }
        public ProgramPipelineState? State { get; }
        public bool BackgroundCompile { get; set; }

        public ShaderInfo(int fragmentOutputMap, ProgramPipelineState state)
        {
            FragmentOutputMap = fragmentOutputMap;
            State = state;
            BackgroundCompile = false;
        }

        public ShaderInfo(int fragmentOutputMap)
        {
            FragmentOutputMap = fragmentOutputMap;
            State = null;
            BackgroundCompile = false;
        }
    }
}
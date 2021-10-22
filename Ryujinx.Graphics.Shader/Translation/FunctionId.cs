namespace Ryujinx.Graphics.Shader.Translation
{
    struct FunctionId
    {
        public bool IsCompilerGenerated => MatchName != FunctionMatchResult.NoMatch;
        public FunctionMatchResult MatchName { get; }
        public int Index { get; }

        public FunctionId(FunctionMatchResult matchName)
        {
            MatchName = matchName;
            Index = 0;
        }

        public FunctionId(int index)
        {
            MatchName = FunctionMatchResult.NoMatch;
            Index = index;
        }
    }
}

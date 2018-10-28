namespace ChocolArm64.Translation
{
    struct AilOpCodeLog : IailEmit
    {
        private string _text;

        public AilOpCodeLog(string text)
        {
            _text = text;
        }

        public void Emit(AilEmitter context)
        {
            context.Generator.EmitWriteLine(_text);
        }
    }
}
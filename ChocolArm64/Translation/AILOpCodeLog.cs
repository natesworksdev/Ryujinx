namespace ChocolArm64.Translation
{
    struct AilOpCodeLog : IAilEmit
    {
        private string _text;

        public AilOpCodeLog(string text)
        {
            this._text = text;
        }

        public void Emit(AilEmitter context)
        {
            context.Generator.EmitWriteLine(_text);
        }
    }
}
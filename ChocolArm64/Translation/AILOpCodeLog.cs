namespace ChocolArm64.Translation
{
    internal struct AILOpCodeLog : IAilEmit
    {
        private string _text;

        public AILOpCodeLog(string text)
        {
            this._text = text;
        }

        public void Emit(AILEmitter context)
        {
            context.Generator.EmitWriteLine(_text);
        }
    }
}
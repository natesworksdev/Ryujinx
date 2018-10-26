using System.Reflection.Emit;

namespace ChocolArm64.Translation
{
    internal class AILLabel : IAilEmit
    {
        private bool _hasLabel;

        private Label _lbl;

        public void Emit(AILEmitter context)
        {
            context.Generator.MarkLabel(GetLabel(context));
        }

        public Label GetLabel(AILEmitter context)
        {
            if (!_hasLabel)
            {
                _lbl = context.Generator.DefineLabel();

                _hasLabel = true;
            }

            return _lbl;
        }
    }
}
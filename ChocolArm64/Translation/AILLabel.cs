using System.Reflection.Emit;

namespace ChocolArm64.Translation
{
    class AilLabel : IAilEmit
    {
        private bool _hasLabel;

        private Label _lbl;

        public void Emit(AilEmitter context)
        {
            context.Generator.MarkLabel(GetLabel(context));
        }

        public Label GetLabel(AilEmitter context)
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
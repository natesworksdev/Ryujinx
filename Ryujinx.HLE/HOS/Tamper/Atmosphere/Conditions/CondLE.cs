using Ryujinx.HLE.HOS.Tamper.Atmosphere.Operations;

namespace Ryujinx.HLE.HOS.Tamper.Atmosphere.Conditions
{
    class CondLE<T> : ICondition where T : unmanaged
    {
        private IOperand _lhs;
        private IOperand _rhs;

        public CondLE(IOperand lhs, IOperand rhs)
        {
            _lhs = lhs;
            _rhs = rhs;
        }

        public bool Evaluate()
        {
            return (dynamic)_lhs.Get<T>() <= (dynamic)_rhs.Get<T>();
        }
    }
}

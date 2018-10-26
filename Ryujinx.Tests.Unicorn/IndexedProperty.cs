using System;

namespace Ryujinx.Tests.Unicorn
{
    public class IndexedProperty<TIndex, TValue>
    {
        private readonly Action<TIndex, TValue> _setAction;
        private readonly Func<TIndex, TValue> _getFunc;

        public IndexedProperty(Func<TIndex, TValue> getFunc, Action<TIndex, TValue> setAction)
        {
            this._getFunc = getFunc;
            this._setAction = setAction;
        }

        public TValue this[TIndex i]
        {
            get => _getFunc(i);
            set => _setAction(i, value);
        }
    }
}

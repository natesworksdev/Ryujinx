using System;

namespace Ryujinx.HLE
{
    public interface IDynamicTextInputHandler : IDisposable
    {
        event DynamicTextChangedEvent TextChanged;

        void SetText(string text);
        void SetMaxLength(int maxLength);

        string AcceptKeyName { get; }
        string CancelKeyName { get; }
    }
}
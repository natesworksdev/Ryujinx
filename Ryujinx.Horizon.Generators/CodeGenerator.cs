using System;
using System.Text;

namespace Ryujinx.Horizon.Generators
{
    class CodeGenerator
    {
        private const int SingleIndentLength = 4;

        private readonly StringBuilder _sb;
        private int _currentIndentCharsCount;

        public CodeGenerator()
        {
            _sb = new StringBuilder();
        }

        public void EnterScope(string header = null)
        {
            if (header != null)
            {
                AppendLine(header);
            }

            AppendLine("{");
            IncreaseIndentation();
        }

        public void LeaveScope(string suffix = "")
        {
            DecreaseIndentation();
            AppendLine($"}}{suffix}");
        }

        public void IncreaseIndentation()
        {
            _currentIndentCharsCount += SingleIndentLength;
        }

        public void DecreaseIndentation()
        {
            _currentIndentCharsCount = Math.Max(_currentIndentCharsCount - SingleIndentLength, 0);
        }

        public void AppendLine()
        {
            _sb.AppendLine();
        }

        public void AppendLine(string text)
        {
            _sb.Append(' ', _currentIndentCharsCount);
            _sb.AppendLine(text);
        }

        public override string ToString()
        {
            return _sb.ToString();
        }
    }
}

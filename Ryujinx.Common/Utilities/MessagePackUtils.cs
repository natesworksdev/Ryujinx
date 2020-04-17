using MsgPack;
using System;
using System.Text;

namespace Ryujinx.Common.Utilities
{
    public static class MessagePackUtils
    {
        public static string ToString(this MessagePackObject obj, bool pretty)
        {
            if (pretty)
            {
                return Format(obj);
            }
            else
            {
                return obj.ToString();
            }
        }

        public static string Format(MessagePackObject obj)
        {
            var builder = new IndentedStringBuilder();

            FormatMsgPackObj(obj, builder);

            return builder.ToString();
        }

        private static void FormatMsgPackObj(MessagePackObject obj, IndentedStringBuilder builder)
        {
            if (obj.IsMap || obj.IsDictionary)
            {
                FormatMsgPackMap(obj, builder);
            }
            else if (obj.IsArray || obj.IsList)
            {
                FormatMsgPackArray(obj, builder);
            }
            else if (obj.IsNil)
            {
                builder.Append("null");
            }
            else
            {
                var literal = obj.ToObject();

                switch (literal)
                {
                    case string _:
                        builder.AppendQuotedString(obj.AsStringUtf16());
                        break;
                    case byte[] byteArray:
                        FormatByteArray(byteArray, builder);
                        break;
                    case MessagePackExtendedTypeObject extObject:
                        builder.Append('{');

                        // Indent
                        builder.IncreaseIndent()
                               .AppendLine();

                        // Print TypeCode field
                        builder.AppendQuotedString("TypeCode")
                               .Append(": ")
                               .Append(extObject.TypeCode)
                               .AppendLine(",");

                        // Print Value field
                        builder.AppendQuotedString("Value")
                               .Append(": ");

                        FormatByteArrayAsString(extObject.GetBody(), builder, true);

                        // Unindent
                        builder.DecreaseIndent()
                               .AppendLine();

                        builder.Append('}');
                        break;
                    default:
                        builder.Append(literal);
                        break;
                }
            }
        }

        private static void FormatByteArray(byte[] arr, IndentedStringBuilder builder)
        {
            builder.Append("[ ");

            foreach (var b in arr)
            {
                builder.Append("0x");
                builder.Append(HexUtils.ToHexChar(b >> 4));
                builder.Append(HexUtils.ToHexChar(b & 0xF));
                builder.Append(", ");
            }

            // Remove trailing comma
            builder.Remove(builder.Length - 2, 2);

            builder.Append(" ]");
        }

        private static void FormatByteArrayAsString(byte[] arr, IndentedStringBuilder builder, bool withPrefix)
        {
            builder.Append('"');

            if (withPrefix)
            {
                builder.Append("0x");
            }

            foreach (var b in arr)
            {
                builder.Append(HexUtils.ToHexChar(b >> 4));
                builder.Append(HexUtils.ToHexChar(b & 0xF));
            }

            builder.Append('"');
        }

        private static void FormatMsgPackMap(MessagePackObject obj, IndentedStringBuilder builder)
        {
            var map = obj.AsDictionary();

            builder.Append('{');

            // Indent
            builder.IncreaseIndent()
                   .AppendLine();

            foreach (var item in map)
            {
                FormatMsgPackObj(item.Key, builder);

                builder.Append(": ");

                FormatMsgPackObj(item.Value, builder);

                builder.AppendLine(",");
            }

            // Remove the trailing new line and comma
            builder.TrimLastLine()
                   .Remove(builder.Length - 1, 1);

            // Unindent
            builder.DecreaseIndent()
                   .AppendLine();

            builder.Append('}');
        }

        private static void FormatMsgPackArray(MessagePackObject obj, IndentedStringBuilder builder)
        {
            var arr = obj.AsList();

            builder.Append("[ ");

            foreach (var item in arr)
            {
                FormatMsgPackObj(item, builder);

                builder.Append(", ");
            }

            // Remove trailing comma
            builder.Remove(builder.Length - 2, 2);

            builder.Append(" ]");
        }

        internal class IndentedStringBuilder
        {
            const string DefaultIndent = "    ";

            private int _indentCount = 0;
            private int _newLineIndex = 0;
            private readonly StringBuilder _builder;

            public string IndentString { get; set; } = DefaultIndent;

            public IndentedStringBuilder(StringBuilder builder)
            {
                _builder = builder;
            }

            public IndentedStringBuilder()
                : this(new StringBuilder())
            { }

            public IndentedStringBuilder(string str)
                : this(new StringBuilder(str))
            { }

            public IndentedStringBuilder(int length)
                : this(new StringBuilder(length))
            { }

            public int Length { get => _builder.Length; }

            public IndentedStringBuilder IncreaseIndent()
            {
                _indentCount++;

                return this;
            }

            public IndentedStringBuilder DecreaseIndent()
            {
                _indentCount--;

                return this;
            }

            public IndentedStringBuilder Append(char value)
            {
                _builder.Append(value);

                return this;
            }

            public IndentedStringBuilder Append(string value)
            {
                _builder.Append(value);

                return this;
            }

            public IndentedStringBuilder Append(object value)
            {
                return Append(value.ToString());
            }

            public IndentedStringBuilder AppendQuotedString(string value)
            {
                _builder.Append('"');
                _builder.Append(value);
                _builder.Append('"');

                return this;
            }

            public IndentedStringBuilder AppendLine()
            {
                _newLineIndex = _builder.Length;

                _builder.AppendLine();

                for (int i = 0; i < _indentCount; i++)
                {
                    _builder.Append(IndentString);
                }

                return this;
            }

            public IndentedStringBuilder AppendLine(string value)
            {
                _builder.Append(value);

                this.AppendLine();

                return this;
            }

            public IndentedStringBuilder TrimLastLine()
            {
                _builder.Remove(_newLineIndex, _builder.Length - _newLineIndex);

                return this;
            }

            public IndentedStringBuilder Remove(int startIndex, int length)
            {
                _builder.Remove(startIndex, length);

                return this;
            }

            public override string ToString()
            {
                return _builder.ToString();
            }
        }
    }
}

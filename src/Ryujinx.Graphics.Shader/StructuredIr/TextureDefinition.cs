namespace Ryujinx.Graphics.Shader
{
    readonly struct TextureDefinition
    {
        public int Binding { get; }
        public string Name { get; }
        public SamplerType Type { get; }
        public TextureFormat Format { get; }
        public int CbufSlot { get; }
        public int HandleIndex { get; }
        public TextureUsageFlags Flags { get; }

        public TextureDefinition(int binding, string name, SamplerType type, TextureFormat format, int cbufSlot, int handleIndex, TextureUsageFlags flags)
        {
            Binding = binding;
            Name = name;
            Type = type;
            Format = format;
            CbufSlot = cbufSlot;
            HandleIndex = handleIndex;
            Flags = flags;
        }

        public TextureDefinition SetFlag(TextureUsageFlags flag)
        {
            return new TextureDefinition(Binding, Name, Type, Format, CbufSlot, HandleIndex, Flags | flag);
        }
    }
}
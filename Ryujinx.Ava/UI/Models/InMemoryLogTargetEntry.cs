using Avalonia.Media;

namespace Ryujinx.Ava.UI.Models
{
    struct InMemoryLogTargetEntry
    {
        public InMemoryLogTargetEntry(Color color, string text)
        {
            Color = color;
            Text = text;
        }

        public Color Color { get; }
        public string Text { get; }
    }
}
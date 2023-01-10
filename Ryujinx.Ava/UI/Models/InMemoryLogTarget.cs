using Avalonia.Media;
using Ryujinx.Common.Logging;
using System.Collections.ObjectModel;

namespace Ryujinx.Ava.UI.Models
{
    internal class InMemoryLogTarget : ILogTarget
    {
        internal struct Entry
        {
            public Entry(Color color, string text)
            {
                Color = color;
                Text = text;
            }

            public Color Color { get; }
            public string Text { get; }
        }

        private static Color GetLogColor(LogLevel level)
        {
            return level switch
            {
                LogLevel.Info    => Colors.White,
                LogLevel.Warning => Colors.Yellow,
                LogLevel.Error   => Colors.Red,
                LogLevel.Stub    => Colors.DarkGray,
                LogLevel.Notice  => Colors.Cyan,
                LogLevel.Trace   => Colors.DarkCyan,
                LogLevel.StdErr  => Colors.Gray,
                _                => Colors.Gray,
            };
        }

        private static readonly InMemoryLogTarget _instance = new InMemoryLogTarget("inMemory");

        private readonly ILogFormatter _formatter;
        private readonly string _name;

        private const int MaximumSize = 1000;

        public readonly ObservableCollection<Entry> Entries;

        string ILogTarget.Name => _name;

        public static InMemoryLogTarget Instance => _instance;

        private InMemoryLogTarget(string name)
        {
            _formatter = new DefaultLogFormatter();
            _name      = name;
            Entries    = new ObservableCollection<Entry>();
        }

        public static void Register()
        {
            Logger.AddTarget(new AsyncLogTargetWrapper(Instance, 1000, AsyncLogTargetOverflowAction.Block));
        }

        public void Log(object sender, LogEventArgs args)
        {
            AddEntry(GetLogColor(args.Level), _formatter.Format(args));
        }

        private void AddEntry(Color color, string text)
        {
            Entries.Insert(0, new Entry(color, text));

            if (Entries.Count > MaximumSize)
            {
                Entries.RemoveAt(Entries.Count - 1);
            }
        }

        public void Dispose()
        {
        }
    }
}
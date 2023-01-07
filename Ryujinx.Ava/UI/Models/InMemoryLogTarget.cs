using Avalonia.Media;
using Ryujinx.Common.Logging;
using System;
using System.Collections.ObjectModel;

namespace Ryujinx.Ava.UI.Models
{
    public class InMemoryLogTarget : ILogTarget
    {
        public class Entry
        {
            public Color Color { get; set; }
            public string Text { get; set; }
        }

        private static Color GetLogColor(LogLevel level) => level switch {
            LogLevel.Info    => Colors.White,
            LogLevel.Warning => Colors.Yellow,
            LogLevel.Error   => Colors.Red,
            LogLevel.Stub    => Colors.DarkGray,
            LogLevel.Notice  => Colors.Cyan,
            LogLevel.Trace   => Colors.DarkCyan,
            _                => Colors.Gray,
        };

        private static readonly InMemoryLogTarget _instance = new InMemoryLogTarget("inMemory");

        private static int _maximumSize = 20000;
        private readonly ILogFormatter _formatter;
        private readonly string _name;

        public ObservableCollection<Entry> Entries;

        string ILogTarget.Name { get => _name; }

        public static InMemoryLogTarget Instance => _instance;

        static InMemoryLogTarget()
        {
        }

        private InMemoryLogTarget(string name)
        {
            _formatter = new DefaultLogFormatter();
            _name      = name;
            Entries    = new ObservableCollection<Entry>();
        }

        public static void Register()
        {
            Logger.AddTarget(new AsyncLogTargetWrapper(Instance, 1000, AsyncLogTargetOverflowAction.Block));
            Logger.RemoveTarget("console");
        }

        public void Log(object sender, LogEventArgs args)
        {
            Entries.Add(new Entry()
            {
                Color = GetLogColor(args.Level),
                Text = _formatter.Format(args),
            });

            if (Entries.Count > _maximumSize)
            {
                Entries.RemoveAt(0);
            }
        }

        public void Dispose()
        {
        }
    }
}
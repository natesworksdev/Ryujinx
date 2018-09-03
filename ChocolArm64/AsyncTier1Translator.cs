using ChocolArm64.Memory;
using ChocolArm64.State;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ChocolArm64
{
    public class AsyncTier1Translator
    {

        private struct QueueObj
        {
            public long Position;
            public ATranslator Translator;
            public AThreadState State;
            public AMemory Memory;

            public QueueObj(long Position, ATranslator Translator, AThreadState State, AMemory Memory)
            {
                this.Position = Position;
                this.Translator = Translator;
                this.State = State;
                this.Memory = Memory;
            }
        }

        Thread Thread;
        BlockingCollection<QueueObj> Queue = new BlockingCollection<QueueObj>();

        public AsyncTier1Translator()
        {
            Thread = new Thread(Run);
            Thread.Start();
        }

        public void Run()
        {
            while (true)
            {
                QueueObj QObj = Queue.Take();
                QObj.Translator.TranslateTier1(QObj.State, QObj.Memory, QObj.Position);
            }
        }

        public void Enqueue(long Position, ATranslator Translator, AThreadState State, AMemory Memory)
        {
            Queue.Add(new QueueObj(Position, Translator, State, Memory));
        }

        public bool InTranslation(long Position)
        {
            return Queue.Where(QObj => QObj.Position == Position).Count() > 0;
        }
    }
}

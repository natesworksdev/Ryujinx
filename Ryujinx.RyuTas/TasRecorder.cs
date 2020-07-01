using System;
using System.Collections.Generic;
using System.Text;
using Ryujinx.RyuTas;

namespace Ryujinx.RyuTas
{   
    public class TasRecorder
    {
        private List<TASInstruction> Recording;

        public TasRecorder()
        {
            Recording = new List<TASInstruction>();
        }

        public void AppendInstruction(TASInstruction ins)
        {
            Recording.Add(ins);
        }

        public void RemoveLastInstruction()
        {
            Recording.RemoveAt(Recording.Count - 1);
        }

        public List<TASInstruction> GetRecording()
        {
            return Recording;
        }
    }
}

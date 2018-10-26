using System.Collections.Generic;

namespace ChocolArm64.Translation
{
    internal class AILBlock : IAilEmit
    {
        public long IntInputs    { get; private set; }
        public long IntOutputs   { get; private set; }
        public long IntAwOutputs { get; private set; }

        public long VecInputs    { get; private set; }
        public long VecOutputs   { get; private set; }
        public long VecAwOutputs { get; private set; }

        public bool HasStateStore { get; private set; }

        public List<IAilEmit> IlEmitters { get; private set; }

        public AILBlock Next   { get; set; }
        public AILBlock Branch { get; set; }

        public AILBlock()
        {
            IlEmitters = new List<IAilEmit>();
        }

        public void Add(IAilEmit ilEmitter)
        {
            if (ilEmitter is AILBarrier)
            {
                //Those barriers are used to separate the groups of CIL
                //opcodes emitted by each ARM instruction.
                //We can only consider the new outputs for doing input elimination
                //after all the CIL opcodes used by the instruction being emitted.
                IntAwOutputs = IntOutputs;
                VecAwOutputs = VecOutputs;
            }
            else if (ilEmitter is AILOpCodeLoad ld && AILEmitter.IsRegIndex(ld.Index))
            {
                switch (ld.IoType)
                {
                    case AIoType.Flag:   IntInputs |= (1L << ld.Index << 32) & ~IntAwOutputs; break;
                    case AIoType.Int:    IntInputs |=  (1L << ld.Index)        & ~IntAwOutputs; break;
                    case AIoType.Vector: VecInputs |=  (1L << ld.Index)        & ~VecAwOutputs; break;
                }
            }
            else if (ilEmitter is AILOpCodeStore st)
            {
                if (AILEmitter.IsRegIndex(st.Index))
                {
                    switch (st.IoType)
                    {
                        case AIoType.Flag:   IntOutputs |= 1L << st.Index << 32; break;
                        case AIoType.Int:    IntOutputs |= 1L << st.Index;        break;
                        case AIoType.Vector: VecOutputs |= 1L << st.Index;        break;
                    }
                }

                if (st.IoType == AIoType.Fields)
                {
                    HasStateStore = true;
                }
            }

            IlEmitters.Add(ilEmitter);
        }

        public void Emit(AILEmitter context)
        {
            foreach (IAilEmit ilEmitter in IlEmitters)
            {
                ilEmitter.Emit(context);
            }
        }
    }
}
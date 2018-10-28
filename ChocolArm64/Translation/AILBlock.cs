using System.Collections.Generic;

namespace ChocolArm64.Translation
{
    class AilBlock : IailEmit
    {
        public long IntInputs    { get; private set; }
        public long IntOutputs   { get; private set; }
        public long IntAwOutputs { get; private set; }

        public long VecInputs    { get; private set; }
        public long VecOutputs   { get; private set; }
        public long VecAwOutputs { get; private set; }

        public bool HasStateStore { get; private set; }

        public List<IailEmit> IlEmitters { get; private set; }

        public AilBlock Next   { get; set; }
        public AilBlock Branch { get; set; }

        public AilBlock()
        {
            IlEmitters = new List<IailEmit>();
        }

        public void Add(IailEmit ilEmitter)
        {
            if (ilEmitter is AilBarrier)
            {
                //Those barriers are used to separate the groups of CIL
                //opcodes emitted by each ARM instruction.
                //We can only consider the new outputs for doing input elimination
                //after all the CIL opcodes used by the instruction being emitted.
                IntAwOutputs = IntOutputs;
                VecAwOutputs = VecOutputs;
            }
            else if (ilEmitter is AilOpCodeLoad ld && AilEmitter.IsRegIndex(ld.Index))
            {
                switch (ld.IoType)
                {
                    case AIoType.Flag:   IntInputs |= ((1L << ld.Index) << 32) & ~IntAwOutputs; break;
                    case AIoType.Int:    IntInputs |=  (1L << ld.Index)        & ~IntAwOutputs; break;
                    case AIoType.Vector: VecInputs |=  (1L << ld.Index)        & ~VecAwOutputs; break;
                }
            }
            else if (ilEmitter is AilOpCodeStore st)
            {
                if (AilEmitter.IsRegIndex(st.Index))
                {
                    switch (st.IoType)
                    {
                        case AIoType.Flag:   IntOutputs |= (1L << st.Index) << 32; break;
                        case AIoType.Int:    IntOutputs |=  1L << st.Index;        break;
                        case AIoType.Vector: VecOutputs |=  1L << st.Index;        break;
                    }
                }

                if (st.IoType == AIoType.Fields)
                {
                    HasStateStore = true;
                }
            }

            IlEmitters.Add(ilEmitter);
        }

        public void Emit(AilEmitter context)
        {
            foreach (IailEmit ilEmitter in IlEmitters)
            {
                ilEmitter.Emit(context);
            }
        }
    }
}
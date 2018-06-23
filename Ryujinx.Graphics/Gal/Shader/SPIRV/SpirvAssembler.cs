using System.IO;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gal.Shader.SPIRV
{
    public class Assembler
    {
        private List<Instruction> Instructions;

        public Assembler()
        {
            Instructions = new List<Instruction>();
        }

        public void Write(Stream Output)
        {
            uint Bound = DoBindings();

            BinaryWriter BW = new BinaryWriter(Output);

            BW.Write((uint)BinaryForm.MagicNumber);
            BW.Write((uint)BinaryForm.VersionNumber);
            BW.Write((uint)BinaryForm.GeneratorMagicNumber);
            BW.Write((uint)Bound);
            BW.Write((uint)0); // Reserved for instruction schema

            foreach (Instruction Instruction in Instructions)
            {
                Instruction.Write(BW);
            }
        }

        public void Add(Instruction Instruction)
        {
            Instructions.Add(Instruction);
        }

        public void Add(Instruction[] Instructions)
        {
            foreach (Instruction Instruction in Instructions)
            {
                Add(Instruction);
            }
        }

        public void Add(List<Instruction> Instructions)
        {
            foreach (Instruction Instruction in Instructions)
            {
                Add(Instruction);
            }
        }

        private uint DoBindings()
        {
            uint Bind = 1;

            foreach (Instruction Instruction in Instructions)
            {
                if (Instruction.HoldsResultId)
                {
                    Instruction.ResultId = Bind;
                    Bind++;
                }
            }

            return Bind;
        }
    }
}
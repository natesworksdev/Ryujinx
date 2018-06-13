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

            var BinaryWriter = new BinaryWriter(Output);

            BinaryWriter.Write((uint)BinaryForm.MagicNumber);
            BinaryWriter.Write((uint)BinaryForm.VersionNumber);
            BinaryWriter.Write((uint)BinaryForm.GeneratorMagicNumber);
            BinaryWriter.Write((uint)Bound);
            BinaryWriter.Write((uint)0); // Reserved for instruction schema

            foreach (var Instruction in Instructions)
            {
                Instruction.Write(BinaryWriter);
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

            foreach (var Instruction in Instructions)
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
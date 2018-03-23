namespace Ryujinx.Graphics.Gal.Shader
{
    class ShaderIrOperOp : ShaderIrOper
    {
        public ShaderIrInst Inst     { get; private set; }
        public ShaderIrOper OperandA { get; set; }
        public ShaderIrOper OperandB { get; set; }
        public ShaderIrOper OperandC { get; set; }

        public ShaderIrOperOp(
            ShaderIrInst Inst,
            ShaderIrOper OperandA = null,
            ShaderIrOper OperandB = null,
            ShaderIrOper OperandC = null)
        {
            this.Inst     = Inst;
            this.OperandA = OperandA;
            this.OperandB = OperandB;
            this.OperandC = OperandC;
        }
    }
}
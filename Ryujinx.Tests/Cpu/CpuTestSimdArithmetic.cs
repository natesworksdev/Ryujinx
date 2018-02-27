using ChocolArm64.State;
using NUnit.Framework;

namespace Ryujinx.Tests.Cpu
{
    public class CpuTestSimdArithmetic : CpuTest
    {
    	[TestCase(0x3FE66666u, 'N', 0x40000000u)]
    	[TestCase(0x3F99999Au, 'N', 0x3F800000u)]
    	[TestCase(0x404CCCCDu, 'P', 0x40800000u)]
    	[TestCase(0x40733333u, 'P', 0x40800000u)]
    	[TestCase(0x404CCCCDu, 'M', 0x40400000u)]
    	[TestCase(0x40733333u, 'M', 0x40400000u)]
    	[TestCase(0x3F99999Au, 'Z', 0x3F800000u)]
    	[TestCase(0x3FE66666u, 'Z', 0x3F800000u)]
    	[TestCase(0x7F800002u, 'N', 0x7F800002u)]
    	[TestCase(0x7F800002u, 'P', 0x7F800002u)]
    	[TestCase(0x7F800002u, 'M', 0x7F800002u)]
    	[TestCase(0x7F800002u, 'Z', 0x7F800002u)]
    	[TestCase(0x7FC00002u, 'N', 0x7FC00002u)]
    	[TestCase(0x7FC00002u, 'P', 0x7FC00002u)]
    	[TestCase(0x7FC00002u, 'M', 0x7FC00002u)]
    	[TestCase(0x7FC00002u, 'Z', 0x7FC00002u)]
    	public void Frintx_S(uint A, char RoundType, uint Result)
        {
        	int FpcrTemp = 0x0;
        	switch(RoundType)
        	{
        		case 'N':
        		FpcrTemp &= ~((1 << 23) | (1 << 22));
        		break;

        		case 'P':
        		FpcrTemp &= ~(1 << 23);
        		FpcrTemp |= 1 << 22;
        		break;

        		case 'M':
        		FpcrTemp |= 1 << 23;
        		FpcrTemp &= ~(1 << 22);
        		break;

        		case 'Z':
        		FpcrTemp |= (1 << 23) | (1 << 22);
        		break;
        	}
        	AVec V1 = new AVec { X0 = A };
        	AThreadState ThreadState = SingleOpcode(0x1E274020, V1: V1, Fpcr: FpcrTemp);
        	Assert.AreEqual(Result, ThreadState.V0.X0);
        }
    }
}
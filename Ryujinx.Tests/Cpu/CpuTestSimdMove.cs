using ChocolArm64.State;
using NUnit.Framework;

namespace Ryujinx.Tests.Cpu
{
    public class CpuTestSimdMove : CpuTest
    {
        [Test, Description("trn1 v0.4s, v1.4s, v2.4s")]
        public void Trn1_V([Random(2)] uint A0, [Random(2)] uint A1, [Random(2)] uint A2, [Random(2)] uint A3,
                           [Random(2)] uint B0, [Random(2)] uint B1, [Random(2)] uint B2, [Random(2)] uint B3)
        {
            uint Opcode = 0x4E822820;
            AVec V1 = new AVec { W0 = A0, W1 = A1, W2 = A2, W3 = A3 };
            AVec V2 = new AVec { W0 = B0, W1 = B1, W2 = B2, W3 = B3 };

            AThreadState ThreadState = SingleOpcode(Opcode, V1: V1, V2: V2);

            Assert.That(ThreadState.V0.W0, Is.EqualTo(A0));
            Assert.That(ThreadState.V0.W1, Is.EqualTo(B0));
            Assert.That(ThreadState.V0.W2, Is.EqualTo(A2));
            Assert.That(ThreadState.V0.W3, Is.EqualTo(B2));
        }

        [Test, Description("trn2 v0.4s, v1.4s, v2.4s")]
        public void Trn2_V([Random(2)] uint A0, [Random(2)] uint A1, [Random(2)] uint A2, [Random(2)] uint A3,
                           [Random(2)] uint B0, [Random(2)] uint B1, [Random(2)] uint B2, [Random(2)] uint B3)
        {
            uint Opcode = 0x4E826820;
            AVec V1 = new AVec { W0 = A0, W1 = A1, W2 = A2, W3 = A3 };
            AVec V2 = new AVec { W0 = B0, W1 = B1, W2 = B2, W3 = B3 };

            AThreadState ThreadState = SingleOpcode(Opcode, V1: V1, V2: V2);

            Assert.That(ThreadState.V0.W0, Is.EqualTo(A1));
            Assert.That(ThreadState.V0.W1, Is.EqualTo(B1));
            Assert.That(ThreadState.V0.W2, Is.EqualTo(A3));
            Assert.That(ThreadState.V0.W3, Is.EqualTo(B3));
        }

        [TestCase(0u, 0u, 0x2313221221112010ul, 0x0000000000000000ul)]
        [TestCase(1u, 0u, 0x2313221221112010ul, 0x2717261625152414ul)]
        [TestCase(0u, 1u, 0x2322131221201110ul, 0x0000000000000000ul)]
        [TestCase(1u, 1u, 0x2322131221201110ul, 0x2726171625241514ul)]
        [TestCase(0u, 2u, 0x2322212013121110ul, 0x0000000000000000ul)]
        [TestCase(1u, 2u, 0x2322212013121110ul, 0x2726252417161514ul)]
        [TestCase(1u, 3u, 0x1716151413121110ul, 0x2726252423222120ul)]
        public void Zip1_V(uint Q, uint size, ulong Result_0, ulong Result_1)
        {
            // ZIP1 V0.<T>, V1.<T>, V2.<T>
            uint Opcode = 0x0E023820 | (Q << 30) | (size << 22);
            AVec V1 = new AVec { X0 = 0x1716151413121110, X1 = 0x1F1E1D1C1B1A1918 };
            AVec V2 = new AVec { X0 = 0x2726252423222120, X1 = 0x2F2E2D2C2B2A2928 };
            AThreadState ThreadState = SingleOpcode(Opcode, V1: V1, V2: V2);
            Assert.AreEqual(Result_0, ThreadState.V0.X0);
            Assert.AreEqual(Result_1, ThreadState.V0.X1);
        }

        [TestCase(0u, 0u, 0x2717261625152414ul, 0x0000000000000000ul)]
        [TestCase(1u, 0u, 0x2B1B2A1A29192818ul, 0x2F1F2E1E2D1D2C1Cul)]
        [TestCase(0u, 1u, 0x2726171625241514ul, 0x0000000000000000ul)]
        [TestCase(1u, 1u, 0x2B2A1B1A29281918ul, 0x2F2E1F1E2D2C1D1Cul)]
        [TestCase(0u, 2u, 0x2726252417161514ul, 0x0000000000000000ul)]
        [TestCase(1u, 2u, 0x2B2A29281B1A1918ul, 0x2F2E2D2C1F1E1D1Cul)]
        [TestCase(1u, 3u, 0x1F1E1D1C1B1A1918ul, 0x2F2E2D2C2B2A2928ul)]
        public void Zip2_V(uint Q, uint size, ulong Result_0, ulong Result_1)
        {
            // ZIP2 V0.<T>, V1.<T>, V2.<T>
            uint Opcode = 0x0E027820 | (Q << 30) | (size << 22);
            AVec V1 = new AVec { X0 = 0x1716151413121110, X1 = 0x1F1E1D1C1B1A1918 };
            AVec V2 = new AVec { X0 = 0x2726252423222120, X1 = 0x2F2E2D2C2B2A2928 };
            AThreadState ThreadState = SingleOpcode(Opcode, V1: V1, V2: V2);
            Assert.AreEqual(Result_0, ThreadState.V0.X0);
            Assert.AreEqual(Result_1, ThreadState.V0.X1);
        }
    }
}

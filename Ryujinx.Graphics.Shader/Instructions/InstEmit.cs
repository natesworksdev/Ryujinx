using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.Translation;

namespace Ryujinx.Graphics.Shader.Instructions
{
    static partial class InstEmit
    {
        public static void AtomCas(EmitterContext context)
        {
            InstAtomCas op = context.GetOp<InstAtomCas>();
        }

        public static void AtomsCas(EmitterContext context)
        {
            InstAtomsCas op = context.GetOp<InstAtomsCas>();
        }

        public static void B2r(EmitterContext context)
        {
            InstB2r op = context.GetOp<InstB2r>();
        }

        public static void Bpt(EmitterContext context)
        {
            InstBpt op = context.GetOp<InstBpt>();
        }

        public static void Cctl(EmitterContext context)
        {
            InstCctl op = context.GetOp<InstCctl>();
        }

        public static void Cctll(EmitterContext context)
        {
            InstCctll op = context.GetOp<InstCctll>();
        }

        public static void Cctlt(EmitterContext context)
        {
            InstCctlt op = context.GetOp<InstCctlt>();
        }

        public static void Cont(EmitterContext context)
        {
            InstContUnsup op = context.GetOp<InstContUnsup>();
        }

        public static void Cset(EmitterContext context)
        {
            InstCsetUnsup op = context.GetOp<InstCsetUnsup>();
        }

        public static void Cs2r(EmitterContext context)
        {
            InstCs2r op = context.GetOp<InstCs2r>();
        }

        public static void DmnmxR(EmitterContext context)
        {
            InstDmnmxR op = context.GetOp<InstDmnmxR>();
        }

        public static void DmnmxI(EmitterContext context)
        {
            InstDmnmxI op = context.GetOp<InstDmnmxI>();
        }

        public static void DmnmxC(EmitterContext context)
        {
            InstDmnmxC op = context.GetOp<InstDmnmxC>();
        }

        public static void DsetR(EmitterContext context)
        {
            InstDsetR op = context.GetOp<InstDsetR>();
        }

        public static void DsetI(EmitterContext context)
        {
            InstDsetI op = context.GetOp<InstDsetI>();
        }

        public static void DsetC(EmitterContext context)
        {
            InstDsetC op = context.GetOp<InstDsetC>();
        }

        public static void DsetpR(EmitterContext context)
        {
            InstDsetpR op = context.GetOp<InstDsetpR>();
        }

        public static void DsetpI(EmitterContext context)
        {
            InstDsetpI op = context.GetOp<InstDsetpI>();
        }

        public static void DsetpC(EmitterContext context)
        {
            InstDsetpC op = context.GetOp<InstDsetpC>();
        }

        public static void FchkR(EmitterContext context)
        {
            InstFchkR op = context.GetOp<InstFchkR>();
        }

        public static void FchkI(EmitterContext context)
        {
            InstFchkI op = context.GetOp<InstFchkI>();
        }

        public static void FchkC(EmitterContext context)
        {
            InstFchkC op = context.GetOp<InstFchkC>();
        }

        public static void Getcrsptr(EmitterContext context)
        {
            InstGetcrsptr op = context.GetOp<InstGetcrsptr>();
        }

        public static void Getlmembase(EmitterContext context)
        {
            InstGetlmembase op = context.GetOp<InstGetlmembase>();
        }

        public static void Ide(EmitterContext context)
        {
            InstIde op = context.GetOp<InstIde>();
        }

        public static void IdpR(EmitterContext context)
        {
            InstIdpR op = context.GetOp<InstIdpR>();
        }

        public static void IdpC(EmitterContext context)
        {
            InstIdpC op = context.GetOp<InstIdpC>();
        }

        public static void ImadspR(EmitterContext context)
        {
            InstImadspR op = context.GetOp<InstImadspR>();
        }

        public static void ImadspI(EmitterContext context)
        {
            InstImadspI op = context.GetOp<InstImadspI>();
        }

        public static void ImadspC(EmitterContext context)
        {
            InstImadspC op = context.GetOp<InstImadspC>();
        }

        public static void ImadspRc(EmitterContext context)
        {
            InstImadspRc op = context.GetOp<InstImadspRc>();
        }

        public static void ImulR(EmitterContext context)
        {
            InstImulR op = context.GetOp<InstImulR>();
        }

        public static void ImulI(EmitterContext context)
        {
            InstImulI op = context.GetOp<InstImulI>();
        }

        public static void ImulC(EmitterContext context)
        {
            InstImulC op = context.GetOp<InstImulC>();
        }

        public static void Imul32i(EmitterContext context)
        {
            InstImul32i op = context.GetOp<InstImul32i>();
        }

        public static void Jcal(EmitterContext context)
        {
            InstJcal op = context.GetOp<InstJcal>();
        }

        public static void Jmp(EmitterContext context)
        {
            InstJmp op = context.GetOp<InstJmp>();
        }

        public static void Jmx(EmitterContext context)
        {
            InstJmx op = context.GetOp<InstJmx>();
        }

        public static void Ld(EmitterContext context)
        {
            InstLd op = context.GetOp<InstLd>();
        }

        public static void Lepc(EmitterContext context)
        {
            InstLepc op = context.GetOp<InstLepc>();
        }

        public static void Longjmp(EmitterContext context)
        {
            InstLongjmp op = context.GetOp<InstLongjmp>();
        }

        public static void Nop(EmitterContext context)
        {
            InstNop op = context.GetOp<InstNop>();
        }

        public static void P2rR(EmitterContext context)
        {
            InstP2rR op = context.GetOp<InstP2rR>();
        }

        public static void P2rI(EmitterContext context)
        {
            InstP2rI op = context.GetOp<InstP2rI>();
        }

        public static void P2rC(EmitterContext context)
        {
            InstP2rC op = context.GetOp<InstP2rC>();
        }

        public static void Pcnt(EmitterContext context)
        {
            InstPcnt op = context.GetOp<InstPcnt>();
        }

        public static void Pexit(EmitterContext context)
        {
            InstPexit op = context.GetOp<InstPexit>();
        }

        public static void Pixld(EmitterContext context)
        {
            InstPixld op = context.GetOp<InstPixld>();
        }

        public static void Plongjmp(EmitterContext context)
        {
            InstPlongjmp op = context.GetOp<InstPlongjmp>();
        }

        public static void Pret(EmitterContext context)
        {
            InstPret op = context.GetOp<InstPret>();
        }

        public static void PrmtR(EmitterContext context)
        {
            InstPrmtR op = context.GetOp<InstPrmtR>();
        }

        public static void PrmtI(EmitterContext context)
        {
            InstPrmtI op = context.GetOp<InstPrmtI>();
        }

        public static void PrmtC(EmitterContext context)
        {
            InstPrmtC op = context.GetOp<InstPrmtC>();
        }

        public static void PrmtRc(EmitterContext context)
        {
            InstPrmtRc op = context.GetOp<InstPrmtRc>();
        }

        public static void R2b(EmitterContext context)
        {
            InstR2b op = context.GetOp<InstR2b>();
        }

        public static void Ram(EmitterContext context)
        {
            InstRam op = context.GetOp<InstRam>();
        }

        public static void Rtt(EmitterContext context)
        {
            InstRtt op = context.GetOp<InstRtt>();
        }

        public static void Sam(EmitterContext context)
        {
            InstSam op = context.GetOp<InstSam>();
        }

        public static void Setcrsptr(EmitterContext context)
        {
            InstSetcrsptr op = context.GetOp<InstSetcrsptr>();
        }

        public static void Setlmembase(EmitterContext context)
        {
            InstSetlmembase op = context.GetOp<InstSetlmembase>();
        }

        public static void ShfLR(EmitterContext context)
        {
            InstShfLR op = context.GetOp<InstShfLR>();
        }

        public static void ShfRR(EmitterContext context)
        {
            InstShfRR op = context.GetOp<InstShfRR>();
        }

        public static void ShfLI(EmitterContext context)
        {
            InstShfLI op = context.GetOp<InstShfLI>();
        }

        public static void ShfRI(EmitterContext context)
        {
            InstShfRI op = context.GetOp<InstShfRI>();
        }

        public static void St(EmitterContext context)
        {
            InstSt op = context.GetOp<InstSt>();
        }

        public static void Stp(EmitterContext context)
        {
            InstStp op = context.GetOp<InstStp>();
        }

        public static void Txa(EmitterContext context)
        {
            InstTxa op = context.GetOp<InstTxa>();
        }

        public static void Vabsdiff(EmitterContext context)
        {
            InstVabsdiff op = context.GetOp<InstVabsdiff>();
        }

        public static void Vabsdiff4(EmitterContext context)
        {
            InstVabsdiff4 op = context.GetOp<InstVabsdiff4>();
        }

        public static void Vadd(EmitterContext context)
        {
            InstVadd op = context.GetOp<InstVadd>();
        }

        public static void Votevtg(EmitterContext context)
        {
            InstVotevtg op = context.GetOp<InstVotevtg>();
        }

        public static void Vset(EmitterContext context)
        {
            InstVset op = context.GetOp<InstVset>();
        }

        public static void Vsetp(EmitterContext context)
        {
            InstVsetp op = context.GetOp<InstVsetp>();
        }

        public static void Vshl(EmitterContext context)
        {
            InstVshl op = context.GetOp<InstVshl>();
        }

        public static void Vshr(EmitterContext context)
        {
            InstVshr op = context.GetOp<InstVshr>();
        }
    }
}
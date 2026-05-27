#region

using System.Linq;
using KoiVM.AST.IL;
using KoiVM.VMIL;

#endregion

namespace KoiVM.Protections.SMC
{
    internal class SMCILTransform : ITransform
    {
        private int adrKey;
        private SMCBlock newTrampoline;
        private ILBlock trampoline;

        public void Initialize(ILTransformer tr)
        {
            trampoline = null;
            tr.RootScope.ProcessBasicBlocks<ILInstrList>(b =>
            {
                if(b.Content.Any(instr => instr.IR != null && instr.IR.Annotation == SMCBlock.AddressPart2))
                    trampoline = (ILBlock) b;
            });
            if(trampoline == null)
                return;

            var scope = tr.RootScope.SearchBlock(trampoline).Last();
            newTrampoline = new SMCBlock(trampoline.Id, trampoline.Content);
            scope.Content[scope.Content.IndexOf(trampoline)] = newTrampoline;

            adrKey = tr.VM.Random.Next();

            newTrampoline.LcgMultValue = (uint) (tr.VM.Random.Next() | 1);
            newTrampoline.LcgAddValue = (uint) tr.VM.Random.Next();
            newTrampoline.MethodSeed1Value = (uint) tr.VM.Random.Next();
            newTrampoline.MethodSeed2Value = (uint) tr.VM.Random.Next();
        }

        public void Transform(ILTransformer tr)
        {
            if(trampoline == null)
                return;

            if(tr.Block.Targets.Contains(trampoline))
                tr.Block.Targets[tr.Block.Targets.IndexOf(trampoline)] = newTrampoline;

            if(tr.Block.Sources.Contains(trampoline))
                tr.Block.Sources[tr.Block.Sources.IndexOf(trampoline)] = newTrampoline;

            tr.Instructions.VisitInstrs(VisitInstr, tr);
        }

        private void VisitInstr(ILInstrList instrs, ILInstruction instr, ref int index, ILTransformer tr)
        {
            if(trampoline == null)
                return;

            if(instr.Operand is ILBlockTarget)
            {
                var target = (ILBlockTarget) instr.Operand;
                if(target.Target == trampoline)
                    target.Target = newTrampoline;
            }
            else if(instr.IR == null)
            {
                return;
            }

            if(instr.IR.Annotation == SMCBlock.BlockOffset && instr.OpCode == ILOpCode.PUSHI_DWORD)
            {
                var imm = (ILImmediate) instr.Operand;
                if((int) imm.Value == 0x0f000001) newTrampoline.BlockOffsetOperand = imm;
            }
            else if(instr.IR.Annotation == SMCBlock.MethodSeed1 && instr.OpCode == ILOpCode.PUSHI_DWORD)
            {
                var imm = (ILImmediate) instr.Operand;
                if((int) imm.Value == 0x0f000002) imm.Value = (int) newTrampoline.MethodSeed1Value;
            }
            else if(instr.IR.Annotation == SMCBlock.MethodSeed2 && instr.OpCode == ILOpCode.PUSHI_DWORD)
            {
                var imm = (ILImmediate) instr.Operand;
                if((int) imm.Value == 0x0f000003) imm.Value = (int) newTrampoline.MethodSeed2Value;
            }
            else if(instr.IR.Annotation == SMCBlock.LcgMult && instr.OpCode == ILOpCode.PUSHI_DWORD)
            {
                var imm = (ILImmediate) instr.Operand;
                if((int) imm.Value == 0x0f000004) imm.Value = (int) newTrampoline.LcgMultValue;
            }
            else if(instr.IR.Annotation == SMCBlock.LcgAdd && instr.OpCode == ILOpCode.PUSHI_DWORD)
            {
                var imm = (ILImmediate) instr.Operand;
                if((int) imm.Value == 0x0f000005) imm.Value = (int) newTrampoline.LcgAddValue;
            }
            else if(instr.IR.Annotation == SMCBlock.DwordCount && instr.OpCode == ILOpCode.PUSHI_DWORD)
            {
                var imm = (ILImmediate) instr.Operand;
                if((int) imm.Value == 0x0f000006) newTrampoline.DwordCountOperand = imm;
            }
            else if(instr.IR.Annotation == SMCBlock.AddressPart1 && instr.OpCode == ILOpCode.PUSHI_DWORD &&
                    instr.Operand is ILBlockTarget)
            {
                var target = (ILBlockTarget) instr.Operand;

                var relBase = new ILInstruction(ILOpCode.PUSHR_QWORD, ILRegister.IP, instr);
                instr.OpCode = ILOpCode.PUSHI_DWORD;
                instr.Operand = new SMCBlockRef(target, relBase, (uint) adrKey);

                instrs.Replace(index, new[]
                {
                    relBase,
                    instr,
                    new ILInstruction(ILOpCode.ADD_QWORD, null, instr)
                });
            }
            else if(instr.IR.Annotation == SMCBlock.AddressPart2 && instr.OpCode == ILOpCode.PUSHI_DWORD)
            {
                var imm = (ILImmediate) instr.Operand;
                if((int) imm.Value == 0x0f000007) imm.Value = adrKey;
            }
        }
    }
}

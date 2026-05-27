#region

using System;
using System.Collections.Generic;
using System.Linq;
using KoiVM.AST;
using KoiVM.AST.IR;
using KoiVM.CFG;
using KoiVM.VMIR;

#endregion

namespace KoiVM.Protections.OpaquePredicate
{
    public class OpaquePredicateTransform : ITransform
    {
        private const double InjectionProbability = 0.4;
        private int nextBlockId = -100;
        private bool doWork;

        public void Initialize(IRTransformer tr)
        {
            if(tr.Context.Method.Module == null)
                return;

            doWork = true;

            var blocks = new List<BasicBlock<IRInstrList>>();
            tr.RootScope.ProcessBasicBlocks<IRInstrList>(block => blocks.Add(block));

            if(blocks.Count < 3)
                return;

            var rng = tr.VM.Random;
            var zeroFlag = 1 << tr.VM.Architecture.Flags.ZERO;
            var signFlag = 1 << tr.VM.Architecture.Flags.SIGN;

            var entryBlock = blocks[0];
            var exitBlock = blocks[blocks.Count - 1];

            foreach(var block in blocks.ToList())
            {
                if(!ShouldInject(block, entryBlock, exitBlock, tr))
                    continue;

                if(rng.NextDouble() >= InjectionProbability)
                    continue;

                var pattern = (Pattern) (rng.Next() % 4);
                var x = rng.Next();
                var k = rng.Next();

                InjectPredicate(tr, block, pattern, x, k, zeroFlag, signFlag);
            }
        }

        public void Transform(IRTransformer tr)
        {
            // All work done in Initialize
        }

        private bool ShouldInject(BasicBlock<IRInstrList> block, BasicBlock<IRInstrList> entry, BasicBlock<IRInstrList> exit, IRTransformer tr)
        {
            if(block.Id < 0)
                return false;
            if(block == entry || block == exit)
                return false;
            if(block.Content.Count < 2)
                return false;

            // Skip blocks in EH handler/filter scopes
            var scopePath = tr.RootScope.SearchBlock(block);
            if(scopePath.Any(s => s.Type == ScopeType.Handler || s.Type == ScopeType.Filter))
                return false;

            // Skip blocks containing __ENTRY/__EXIT
            if(block.Content.Any(i => i.OpCode == IROpCode.__ENTRY || i.OpCode == IROpCode.__EXIT))
                return false;

            return true;
        }

        private void InjectPredicate(IRTransformer tr, BasicBlock<IRInstrList> target, Pattern pattern, int x, int k, int zeroFlag, int signFlag)
        {
            var t1 = tr.Context.AllocateVRegister(ASTType.I4);
            var t2 = tr.Context.AllocateVRegister(ASTType.I4);
            var flagReg = tr.Context.AllocateVRegister(ASTType.I4);

            var rng = tr.VM.Random;
            var phId = nextBlockId--;
            var junkId = nextBlockId--;

            var phContent = new IRInstrList();
            var junkContent = new IRInstrList();

            switch(pattern)
            {
                case Pattern.SquareNonNeg:
                    // (x * x) >= 0 always true => SIGN flag is 0
                    phContent.Add(new IRInstruction(IROpCode.MOV, t1, IRConstant.FromI4(x)));
                    phContent.Add(new IRInstruction(IROpCode.MOV, t2, t1));
                    phContent.Add(new IRInstruction(IROpCode.MUL, t2, t1));
                    phContent.Add(new IRInstruction(IROpCode.CMP, t2, IRConstant.FromI4(0)));
                    phContent.Add(new IRInstruction(IROpCode.__GETF, flagReg, IRConstant.FromI4(signFlag)));
                    phContent.Add(new IRInstruction(IROpCode.JZ, new IRBlockTarget(target), flagReg));
                    phContent.Add(new IRInstruction(IROpCode.JMP, new IRBlockTarget(null)));
                    break;

                case Pattern.XorSelfZero:
                    // (x ^ x) == 0 always true => ZERO flag is set
                    phContent.Add(new IRInstruction(IROpCode.MOV, t1, IRConstant.FromI4(x)));
                    phContent.Add(new IRInstruction(IROpCode.__XOR, t1, t1));
                    phContent.Add(new IRInstruction(IROpCode.CMP, t1, IRConstant.FromI4(0)));
                    phContent.Add(new IRInstruction(IROpCode.__GETF, flagReg, IRConstant.FromI4(zeroFlag)));
                    phContent.Add(new IRInstruction(IROpCode.JNZ, new IRBlockTarget(target), flagReg));
                    phContent.Add(new IRInstruction(IROpCode.JMP, new IRBlockTarget(null)));
                    break;

                case Pattern.ComplementAll:
                    // (x | ~x) == -1 always true => ZERO flag set after CMP with -1
                    phContent.Add(new IRInstruction(IROpCode.MOV, t1, IRConstant.FromI4(x)));
                    phContent.Add(new IRInstruction(IROpCode.MOV, t2, t1));
                    phContent.Add(new IRInstruction(IROpCode.__NOT, t1, t1));
                    phContent.Add(new IRInstruction(IROpCode.__OR, t2, t1));
                    phContent.Add(new IRInstruction(IROpCode.CMP, t2, IRConstant.FromI4(-1)));
                    phContent.Add(new IRInstruction(IROpCode.__GETF, flagReg, IRConstant.FromI4(zeroFlag)));
                    phContent.Add(new IRInstruction(IROpCode.JNZ, new IRBlockTarget(target), flagReg));
                    phContent.Add(new IRInstruction(IROpCode.JMP, new IRBlockTarget(null)));
                    break;

                case Pattern.DoubleXorCancel:
                    // (x ^ k) ^ k == x always true => ZERO flag set after CMP with x
                    phContent.Add(new IRInstruction(IROpCode.MOV, t1, IRConstant.FromI4(x)));
                    phContent.Add(new IRInstruction(IROpCode.__XOR, t1, IRConstant.FromI4(k)));
                    phContent.Add(new IRInstruction(IROpCode.__XOR, t1, IRConstant.FromI4(k)));
                    phContent.Add(new IRInstruction(IROpCode.CMP, t1, IRConstant.FromI4(x)));
                    phContent.Add(new IRInstruction(IROpCode.__GETF, flagReg, IRConstant.FromI4(zeroFlag)));
                    phContent.Add(new IRInstruction(IROpCode.JNZ, new IRBlockTarget(target), flagReg));
                    phContent.Add(new IRInstruction(IROpCode.JMP, new IRBlockTarget(null)));
                    break;
            }

            // Junk block: nonsense + VCALL EXIT
            var junkReg = tr.Context.AllocateVRegister(ASTType.I4);
            junkContent.Add(new IRInstruction(IROpCode.MOV, junkReg, IRConstant.FromI4(rng.Next())));
            junkContent.Add(new IRInstruction(IROpCode.ADD, junkReg, IRConstant.FromI4(rng.Next())));

            var phBlock = new BasicBlock<IRInstrList>(phId, phContent);
            var junkBlock = new BasicBlock<IRInstrList>(junkId, junkContent);

            // Fix the JMP in phContent to point to junkBlock
            // (the last instruction is always JMP to junk)
            var jmpToJunk = phContent[phContent.Count - 1];
            ((IRBlockTarget) jmpToJunk.Operand1).Target = junkBlock;

            Rewire(tr, target, phBlock, junkBlock);
        }

        private void Rewire(IRTransformer tr, BasicBlock<IRInstrList> target, BasicBlock<IRInstrList> phBlock, BasicBlock<IRInstrList> junkBlock)
        {
            var oldSources = target.Sources.ToList();
            target.Sources.Clear();

            // Redirect source blocks to phBlock
            foreach(var source in oldSources)
            {
                int idx = source.Targets.IndexOf(target);
                if(idx >= 0)
                    source.Targets[idx] = phBlock;

                // Update IR instruction operands
                foreach(var instr in source.Content)
                {
                    if(instr.Operand1 is IRBlockTarget bt1 && bt1.Target == target)
                        bt1.Target = phBlock;
                    if(instr.Operand2 is IRBlockTarget bt2 && bt2.Target == target)
                        bt2.Target = phBlock;
                }
            }

            // phBlock <- old sources
            phBlock.Sources.Clear();
            foreach(var source in oldSources)
                phBlock.Sources.Add(source);

            // phBlock -> target (true path), phBlock -> junk (false path)
            phBlock.Targets.Add(target);
            phBlock.Targets.Add(junkBlock);

            // target <- phBlock
            target.Sources.Add(phBlock);

            // junkBlock <- phBlock
            junkBlock.Sources.Add(phBlock);

            // Insert into scope
            var scopePath = tr.RootScope.SearchBlock(target);
            var containingScope = scopePath.Last();
            int targetIndex = containingScope.Content.IndexOf(target);
            containingScope.Content.Insert(targetIndex, junkBlock);
            containingScope.Content.Insert(targetIndex, phBlock);
        }

        private enum Pattern
        {
            SquareNonNeg,
            XorSelfZero,
            ComplementAll,
            DoubleXorCancel
        }
    }
}

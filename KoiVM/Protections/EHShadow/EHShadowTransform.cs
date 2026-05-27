#region

using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet.Emit;
using KoiVM.AST;
using KoiVM.AST.IR;
using KoiVM.CFG;
using KoiVM.VMIR;

#endregion

namespace KoiVM.Protections.EHShadow
{
    /// <summary>
    ///     P1b: EH Shadowing — injects fake TRY/LEAVE pairs of type EH_FAULT
    ///     around ~30% of eligible basic blocks.  During normal execution FAULT
    ///     frames are simply popped by LEAVE (no control-flow change).  During
    ///     exception unwinding they are dispatched via SetupFinallyFrame to a
    ///     junk handler whose sole instruction is __EHRET (→ RET), which causes
    ///     DarkInternal to return Exit and ObjectPool.Load to resume HandleEH.
    ///     The scheme is fully transparent to both normal flow and EH unwinding.
    /// </summary>
    public class EHShadowTransform : ITransform
    {
        private const double InjectionProbability = 0.3;

        // Block-ID space for injected junk handlers.
        // SMC stubs use -1 .. -4; opaque predicates use -100 and below.
        // We use -200 and below to stay clear of both ranges.
        private int nextBlockId = -200;

        public void Initialize(IRTransformer tr)
        {
            // Guard: skip runtime methods and methods without a module.
            if (tr.Context.Method.Module == null)
                return;

            // Collect every basic block in the method.
            var blocks = new List<BasicBlock<IRInstrList>>();
            tr.RootScope.ProcessBasicBlocks<IRInstrList>(b => blocks.Add(b));

            // Need at least 4 blocks to make injection worthwhile.
            if (blocks.Count < 4)
                return;

            var entryBlock = blocks[0];
            var exitBlock  = blocks[blocks.Count - 1];
            var rng        = tr.VM.Random;

            foreach (var block in blocks.ToList())
            {
                if (!ShouldInject(block, entryBlock, exitBlock, tr))
                    continue;

                if (rng.NextDouble() >= InjectionProbability)
                    continue;

                InjectShadowEH(tr, block);
            }
        }

        // Transform() is intentionally empty — all work is done in Initialize()
        // so that junk blocks are visible to subsequent transforms (e.g. SMC).
        public void Transform(IRTransformer tr)
        {
        }

        // ---------------------------------------------------------------
        // Eligibility predicate
        // ---------------------------------------------------------------

        private static bool ShouldInject(
            BasicBlock<IRInstrList> block,
            BasicBlock<IRInstrList> entry,
            BasicBlock<IRInstrList> exit,
            IRTransformer tr)
        {
            // Skip injected blocks (SMC: -1..-4, opaque: -100+, us: -200+)
            if (block.Id < 0)
                return false;

            // Never touch the method entry or exit sentinel blocks
            if (block == entry || block == exit)
                return false;

            // Skip blocks that contain __ENTRY / __EXIT pseudo-instructions
            if (block.Content.Any(i =>
                    i.OpCode == IROpCode.__ENTRY || i.OpCode == IROpCode.__EXIT))
                return false;

            // Need at least 3 real instructions so PUSH+TRY / LEAVE don't
            // dominate the entire block content.
            if (block.Content.Count < 3)
                return false;

            // Must end on a recognised control-flow terminator so we know
            // where to insert LEAVE (one slot before the terminator).
            var last = block.Content[block.Content.Count - 1].OpCode;
            if (last != IROpCode.JMP  && last != IROpCode.JZ  &&
                last != IROpCode.JNZ  && last != IROpCode.SWT &&
                last != IROpCode.RET)
                return false;

            // Skip blocks that already sit inside a Try, Handler or Filter
            // scope — injecting nested fake frames there complicates the EH
            // stack ordering and is not required for obfuscation purposes.
            var scopePath = tr.RootScope.SearchBlock(block);
            if (scopePath.Any(s =>
                    s.Type == ScopeType.Try     ||
                    s.Type == ScopeType.Handler ||
                    s.Type == ScopeType.Filter))
                return false;

            return true;
        }

        // ---------------------------------------------------------------
        // Injection
        // ---------------------------------------------------------------

        private void InjectShadowEH(IRTransformer tr, BasicBlock<IRInstrList> block)
        {
            // Create a synthetic ExceptionHandler of type Fault.
            // dnlib only requires the HandlerType to be set for our purposes;
            // the try/handler IL offsets are never written for IR-level EH.
            var fakeEH = new ExceptionHandler(ExceptionHandlerType.Fault);

            // ------------------------------------------------------------------
            // Build the junk handler block.
            //   • Negative ID to avoid collisions with real blocks.
            //   • Single instruction: __EHRET  (translates to RET at VMIL level)
            //   • Flag ExitEHReturn so the serialiser knows this is an EH return.
            // ------------------------------------------------------------------
            var junkContent = new IRInstrList();
            junkContent.Add(new IRInstruction(IROpCode.__EHRET));

            var junkBlock = new BasicBlock<IRInstrList>(nextBlockId--, junkContent)
            {
                Flags = BlockFlags.ExitEHReturn
            };

            // ------------------------------------------------------------------
            // Prepend PUSH + TRY at the top of the target block.
            //
            //   PUSH  IRBlockTarget(junkBlock)        ← handler address
            //   TRY   IRConstant.FromI4(EH_FAULT)     ← push fault frame
            //         (operand2 = null for FAULT)
            //   [original instructions…]
            // ------------------------------------------------------------------
            var ehFaultType = tr.VM.Runtime.RTFlags.EH_FAULT;

            var pushHandlerAddr = new IRInstruction(
                IROpCode.PUSH,
                new IRBlockTarget(junkBlock));

            var tryInstr = new IRInstruction(
                IROpCode.TRY,
                IRConstant.FromI4(ehFaultType),
                null)                                // operand2 null ⟹ no extra pop
            {
                Annotation = new EHInfo(fakeEH)
            };

            block.Content.Insert(0, tryInstr);
            block.Content.Insert(0, pushHandlerAddr);

            // ------------------------------------------------------------------
            // Insert LEAVE immediately before the terminator.
            //
            //   LEAVE IRBlockTarget(junkBlock)        ← pop fault frame
            //   [terminator: JMP / JZ / JNZ / SWT / RET]
            //
            // TryHandler (EHHandlers.cs) already PUSHes operand2 if present,
            // then operand1 (ehType) then emits TRY.  At runtime Try.cs pops:
            //   type, optional_operand, handler_addr.
            // LeaveHandler PUSHes operand1 (handler addr) then emits LEAVE.
            // Leave.cs pops handler_addr, matches EHStack top, removes frame.
            // For FAULT frames Leave.cs does NOT redirect IP — pure no-op.
            // ------------------------------------------------------------------
            var terminatorIndex = block.Content.Count - 1;
            var leaveInstr = new IRInstruction(
                IROpCode.LEAVE,
                new IRBlockTarget(junkBlock))
            {
                Annotation = new EHInfo(fakeEH)
            };
            block.Content.Insert(terminatorIndex, leaveInstr);

            // ------------------------------------------------------------------
            // Insert the junk handler block into the containing scope so that
            // subsequent passes (VMIL translation, serialisation) can find it.
            // We use the same scope-lookup pattern as OpaquePredicateTransform.
            // ------------------------------------------------------------------
            var scopePath       = tr.RootScope.SearchBlock(block);
            var containingScope = scopePath.Last();
            var targetIndex     = containingScope.Content.IndexOf(block);

            // Insert junk handler right after the target block so it is
            // serialised adjacent to it, which is tidy but not required.
            containingScope.Content.Insert(targetIndex + 1, junkBlock);
        }
    }
}

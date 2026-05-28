#region

using System.Linq;
using dnlib.DotNet;
using KoiVM.AST;
using KoiVM.AST.IR;
using KoiVM.CFG;
using KoiVM.VMIR;

#endregion

namespace KoiVM.Protections.SMC
{
    public class SMCIRTransform : ITransform
    {
        private bool doWork = true;

        public void Initialize(IRTransformer tr)
        {
            if(tr.Context.Method.Module == null)
            {
                doWork = false;
                return;
            }

            var key = tr.Context.AllocateVRegister(ASTType.I4);
            var temp = tr.Context.AllocateVRegister(ASTType.I4);
            var counter = tr.Context.AllocateVRegister(ASTType.I4);
            var pointer = tr.Context.AllocateVRegister(ASTType.Ptr);
            var dwordVal = tr.Context.AllocateVRegister(ASTType.I4);
            var entryAddr = tr.Context.AllocateVRegister(ASTType.Ptr);

            var entry = (BasicBlock<IRInstrList>) tr.RootScope.GetBasicBlocks().First();

            var entryStub = new BasicBlock<IRInstrList>(-4, new IRInstrList());
            var dwordLoop = new BasicBlock<IRInstrList>(-3, new IRInstrList());
            var loopCheck = new BasicBlock<IRInstrList>(-2, new IRInstrList());
            var trampoline = new BasicBlock<IRInstrList>(-1, new IRInstrList());

            var int32Type = tr.Context.Method.Module.CorLibTypes.Int32.ToTypeDefOrRef();

            // Randomized sentinel placeholders (replaced by SMCILTransform)
            var sentinelBlkOff = 0x0f000001;
            var sentinelMs1 = 0x0f000002;
            var sentinelMs2 = 0x0f000003;
            var sentinelLm = 0x0f000004;
            var sentinelLa = 0x0f000005;
            var sentinelDwCount = 0x0f000006;
            var sentinelAdrKey = 0x0f000007;

            // entryStub: derive key and call AES
            entryStub.Content.AddRange(new[]
            {
                // K1 = blockOffset ^ methodSeed1
                new IRInstruction(IROpCode.MOV, IRRegister.K1, IRConstant.FromI4(sentinelBlkOff), SMCBlock.BlockOffset),
                new IRInstruction(IROpCode.__XOR, IRRegister.K1, IRConstant.FromI4(sentinelMs1), SMCBlock.MethodSeed1),

                // K2 = methodSeed2
                new IRInstruction(IROpCode.MOV, IRRegister.K2, IRConstant.FromI4(sentinelMs2), SMCBlock.MethodSeed2),

                // pointer = &trampoline
                new IRInstruction(IROpCode.MOV, pointer, new IRBlockTarget(trampoline)),

                // counter = byteCount
                new IRInstruction(IROpCode.MOV, counter, IRConstant.FromI4(sentinelDwCount), SMCBlock.DwordCount),

                // AES(pointer, counter)
                new IRInstruction(IROpCode.__AES, pointer, counter),

                new IRInstruction(IROpCode.JMP, new IRBlockTarget(trampoline))
            });
            entryStub.LinkTo(trampoline);

            // trampoline: de-obfuscate entry address and jump
            trampoline.Content.AddRange(new[]
            {
                new IRInstruction(IROpCode.NOP),
                new IRInstruction(IROpCode.NOP),
                new IRInstruction(IROpCode.MOV, entryAddr, new IRBlockTarget(entry), SMCBlock.AddressPart1),
                new IRInstruction(IROpCode.__XOR, entryAddr, IRConstant.FromI4(sentinelAdrKey), SMCBlock.AddressPart2),
                new IRInstruction(IROpCode.JMP, new IRBlockTarget(entry))
            });
            trampoline.LinkTo(entry);

            var scope = tr.RootScope.SearchBlock(entry).Last();
            scope.Content.Insert(0, entryStub);
            scope.Content.Insert(1, trampoline);
        }

        public void Transform(IRTransformer tr)
        {
            if(doWork)
                tr.Block.Id += 2;
        }
    }
}

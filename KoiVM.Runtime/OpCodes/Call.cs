#region

using KoiVM.Runtime.Dynamic;
using KoiVM.Runtime.Execution;

#endregion

namespace KoiVM.Runtime.OpCodes
{
    internal class Call : IOpCode
    {
        public byte Code => NeonVMConstants.OP_CALL;

        public void Load(NeonVMContext ctx, out ExecutionState state)
        {
            var sp = ctx.Registers[NeonVMConstants.REG_SP].U4;
            var slot = ctx.Stack[sp];
            ctx.Stack[sp] = ctx.Registers[NeonVMConstants.REG_IP];
            ctx.Registers[NeonVMConstants.REG_IP].U8 = slot.U8;
            state = ExecutionState.Next;
        }
    }
}
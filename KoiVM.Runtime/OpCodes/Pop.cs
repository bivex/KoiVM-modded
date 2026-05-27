#region

using System.Runtime.Serialization.Formatters.Dynamic;
using System.Runtime.Serialization.Formatters.Execution;

#endregion

namespace System.Runtime.Serialization.Formatters.OpCodes
{
    internal class Pop : IOpCode
    {
        public byte Code => NeonVMConstants.OP_POP;

        public void Load(NeonVMContext ctx, out ExecutionState state)
        {
            var sp = ctx.Registers[NeonVMConstants.REG_SP].U4;
            var slot = ctx.Stack[sp];
            ctx.Stack.SetTopPosition(--sp);
            ctx.Registers[NeonVMConstants.REG_SP].U4 = sp;

            var regId = ctx.ReadByte();
            if((regId == NeonVMConstants.REG_SP || regId == NeonVMConstants.REG_BP) && slot.O is StackRef)
                ctx.Registers[regId] = new NeonVMSlot {U4 = ((StackRef) slot.O).StackPos};
            else
                ctx.Registers[regId] = slot;
            state = ExecutionState.Next;
        }
    }
}
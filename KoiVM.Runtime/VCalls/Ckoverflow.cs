#region

using System;
using System.Runtime.Serialization.Formatters.Dynamic;
using System.Runtime.Serialization.Formatters.Execution;

#endregion

namespace System.Runtime.Serialization.Formatters.VCalls
{
    internal class Ckoverflow : IVCall
    {
        public byte Code => NeonVMConstants.VCALL_CKOVERFLOW;

        public void Load(NeonVMContext ctx, out ExecutionState state)
        {
            var sp = ctx.Registers[NeonVMConstants.REG_SP].U4;
            var fSlot = ctx.Stack[sp--];

            if(fSlot.U4 != 0)
                throw new OverflowException();

            ctx.Stack.SetTopPosition(sp);
            ctx.Registers[NeonVMConstants.REG_SP].U4 = sp;
            state = ExecutionState.Next;
        }
    }
}
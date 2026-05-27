#region

using System.Runtime.Serialization.Formatters.Dynamic;
using System.Runtime.Serialization.Formatters.Execution;

#endregion

namespace System.Runtime.Serialization.Formatters.VCalls
{
    internal class Rangechk : IVCall
    {
        public byte Code => NeonVMConstants.VCALL_RANGECHK;

        public void Load(NeonVMContext ctx, out ExecutionState state)
        {
            var sp = ctx.Registers[NeonVMConstants.REG_SP].U4;
            var valueSlot = ctx.Stack[sp--];
            var maxSlot = ctx.Stack[sp--];
            var minSlot = ctx.Stack[sp];

            valueSlot.U8 = (long) valueSlot.U8 > (long) maxSlot.U8 || (long) valueSlot.U8 < (long) minSlot.U8 ? 1u : 0;

            ctx.Stack[sp] = valueSlot;

            ctx.Stack.SetTopPosition(sp);
            ctx.Registers[NeonVMConstants.REG_SP].U4 = sp;
            state = ExecutionState.Next;
        }
    }
}
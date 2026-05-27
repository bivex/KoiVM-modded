#region

using System.Runtime.Serialization.Formatters.Data;
using System.Runtime.Serialization.Formatters.Dynamic;
using System.Runtime.Serialization.Formatters.Execution;

#endregion

namespace System.Runtime.Serialization.Formatters.OpCodes
{
    internal class Vcall : IOpCode
    {
        public byte Code => NeonVMConstants.OP_VCALL;

        public void Load(NeonVMContext ctx, out ExecutionState state)
        {
            var sp = ctx.Registers[NeonVMConstants.REG_SP].U4;
            var slot = ctx.Stack[sp];
            ctx.Stack.SetTopPosition(--sp);
            ctx.Registers[NeonVMConstants.REG_SP].U4 = sp;

            var vCall = VCallMap.Lookup(slot.U1);
            vCall.Load(ctx, out state);
        }
    }
}
#region

using System.Runtime.Serialization.Formatters.Dynamic;
using System.Runtime.Serialization.Formatters.Execution;

#endregion

namespace System.Runtime.Serialization.Formatters.VCalls
{
    internal class Localloc : IVCall
    {
        public byte Code => NeonVMConstants.VCALL_LOCALLOC;

        public void Load(NeonVMContext ctx, out ExecutionState state)
        {
            var sp = ctx.Registers[NeonVMConstants.REG_SP].U4;
            var bp = ctx.Registers[NeonVMConstants.REG_BP].U4;
            var size = ctx.Stack[sp].U4;
            ctx.Stack[sp] = new NeonVMSlot
            {
                U8 = (ulong) ctx.Stack.Localloc(bp, size)
            };

            state = ExecutionState.Next;
        }
    }
}
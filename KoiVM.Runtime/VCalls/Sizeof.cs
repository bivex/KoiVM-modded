#region

using System;
using System.Runtime.Serialization.Formatters.Dynamic;
using System.Runtime.Serialization.Formatters.Execution;
using System.Runtime.Serialization.Formatters.Execution.Internal;

#endregion

namespace System.Runtime.Serialization.Formatters.VCalls
{
    internal class Sizeof : IVCall
    {
        public byte Code => NeonVMConstants.VCALL_SIZEOF;

        public void Load(NeonVMContext ctx, out ExecutionState state)
        {
            var sp = ctx.Registers[NeonVMConstants.REG_SP].U4;
            var bp = ctx.Registers[NeonVMConstants.REG_BP].U4;
            var type = (Type) ctx.Instance.Data.LookupReference(ctx.Stack[sp].U4);
            ctx.Stack[sp] = new NeonVMSlot
            {
                U4 = (uint) SizeOfHelper.SizeOf(type)
            };

            state = ExecutionState.Next;
        }
    }
}
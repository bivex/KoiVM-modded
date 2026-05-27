#region

using System.Runtime.Serialization.Formatters.Dynamic;
using System.Runtime.Serialization.Formatters.Execution;

#endregion

namespace System.Runtime.Serialization.Formatters.OpCodes
{
    internal class FConvR32 : IOpCode
    {
        public byte Code => NeonVMConstants.OP_FCONV_R32;

        public void Load(NeonVMContext ctx, out ExecutionState state)
        {
            var sp = ctx.Registers[NeonVMConstants.REG_SP].U4;
            var valueSlot = ctx.Stack[sp];

            valueSlot.R4 = (long) valueSlot.U8;

            ctx.Stack[sp] = valueSlot;

            state = ExecutionState.Next;
        }
    }

    internal class FConvR64 : IOpCode
    {
        public byte Code => NeonVMConstants.OP_FCONV_R64;

        public void Load(NeonVMContext ctx, out ExecutionState state)
        {
            var sp = ctx.Registers[NeonVMConstants.REG_SP].U4;
            var valueSlot = ctx.Stack[sp];

            var fl = ctx.Registers[NeonVMConstants.REG_FL].U1;
            if((fl & NeonVMConstants.FL_UNSIGNED) != 0) valueSlot.R8 = valueSlot.U8;
            else valueSlot.R8 = (long) valueSlot.U8;
            ctx.Registers[NeonVMConstants.REG_FL].U1 = (byte) (fl & ~NeonVMConstants.FL_UNSIGNED);

            ctx.Stack[sp] = valueSlot;

            state = ExecutionState.Next;
        }
    }

    internal class FConvR32R64 : IOpCode
    {
        public byte Code => NeonVMConstants.OP_FCONV_R32_R64;

        public void Load(NeonVMContext ctx, out ExecutionState state)
        {
            var sp = ctx.Registers[NeonVMConstants.REG_SP].U4;
            var valueSlot = ctx.Stack[sp];
            valueSlot.R8 = valueSlot.R4;
            ctx.Stack[sp] = valueSlot;

            state = ExecutionState.Next;
        }
    }

    internal class FConvR64R32 : IOpCode
    {
        public byte Code => NeonVMConstants.OP_FCONV_R64_R32;

        public void Load(NeonVMContext ctx, out ExecutionState state)
        {
            var sp = ctx.Registers[NeonVMConstants.REG_SP].U4;
            var valueSlot = ctx.Stack[sp];
            valueSlot.R4 = (float) valueSlot.R8;
            ctx.Stack[sp] = valueSlot;

            state = ExecutionState.Next;
        }
    }
}
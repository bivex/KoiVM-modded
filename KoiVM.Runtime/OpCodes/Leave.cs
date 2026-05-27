#region

using System;
using System.Runtime.Serialization.Formatters.Dynamic;
using System.Runtime.Serialization.Formatters.Execution;

#endregion

namespace System.Runtime.Serialization.Formatters.OpCodes
{
    internal class Leave : IOpCode
    {
        public byte Code => NeonVMConstants.OP_LEAVE;

        public void Load(NeonVMContext ctx, out ExecutionState state)
        {
            var sp = ctx.Registers[NeonVMConstants.REG_SP].U4;
            var handler = ctx.Stack[sp--].U8;

            var frameIndex = ctx.EHStack.Count - 1;
            var frame = ctx.EHStack[frameIndex];

            if(frame.HandlerAddr != handler)
                throw new InvalidProgramException();
            ctx.EHStack.RemoveAt(frameIndex);

            if(frame.EHType == NeonVMConstants.EH_FINALLY)
            {
                ctx.Stack[++sp] = ctx.Registers[NeonVMConstants.REG_IP];
                ctx.Registers[NeonVMConstants.REG_K1].U1 = 0;
                ctx.Registers[NeonVMConstants.REG_IP].U8 = frame.HandlerAddr;
            }

            ctx.Stack.SetTopPosition(sp);
            ctx.Registers[NeonVMConstants.REG_SP].U4 = sp;

            state = ExecutionState.Next;
        }
    }
}
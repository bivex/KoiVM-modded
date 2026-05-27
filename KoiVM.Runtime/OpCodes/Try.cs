#region

using System;
using System.Runtime.Serialization.Formatters.Dynamic;
using System.Runtime.Serialization.Formatters.Execution;

#endregion

namespace System.Runtime.Serialization.Formatters.OpCodes
{
    internal class Try : IOpCode
    {
        public byte Code => NeonVMConstants.OP_TRY;

        public void Load(NeonVMContext ctx, out ExecutionState state)
        {
            var sp = ctx.Registers[NeonVMConstants.REG_SP].U4;
            var type = ctx.Stack[sp--].U1;

            var frame = new EHFrame();
            frame.EHType = type;
            if(type == NeonVMConstants.EH_CATCH) frame.CatchType = (Type) ctx.Instance.Data.LookupReference(ctx.Stack[sp--].U4);
            else if(type == NeonVMConstants.EH_FILTER) frame.FilterAddr = ctx.Stack[sp--].U8;
            frame.HandlerAddr = ctx.Stack[sp--].U8;

            ctx.Stack.SetTopPosition(sp);
            ctx.Registers[NeonVMConstants.REG_SP].U4 = sp;

            frame.BP = ctx.Registers[NeonVMConstants.REG_BP];
            frame.SP = ctx.Registers[NeonVMConstants.REG_SP];
            ctx.EHStack.Add(frame);

            state = ExecutionState.Next;
        }
    }
}
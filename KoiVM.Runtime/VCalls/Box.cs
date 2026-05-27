#region

using System;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Dynamic;
using System.Runtime.Serialization.Formatters.Execution;

#endregion

namespace System.Runtime.Serialization.Formatters.VCalls
{
    internal class Box : IVCall
    {
        public byte Code => NeonVMConstants.VCALL_BOX;

        public void Load(NeonVMContext ctx, out ExecutionState state)
        {
            var sp = ctx.Registers[NeonVMConstants.REG_SP].U4;
            var typeSlot = ctx.Stack[sp--];
            var valSlot = ctx.Stack[sp];

            var valType = (Type) ctx.Instance.Data.LookupReference(typeSlot.U4);
            if(Type.GetTypeCode(valType) == TypeCode.String && valSlot.O == null)
            {
                valSlot.O = ctx.Instance.Data.LookupString(valSlot.U4);
            }
            else
            {
                Debug.Assert(valType.IsValueType);
                valSlot.O = valSlot.ToObject(valType);
            }
            ctx.Stack[sp] = valSlot;

            ctx.Stack.SetTopPosition(sp);
            ctx.Registers[NeonVMConstants.REG_SP].U4 = sp;
            state = ExecutionState.Next;
        }
    }
}
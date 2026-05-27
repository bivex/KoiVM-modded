#region

using System;
using System.Runtime.Serialization.Formatters.Dynamic;
using System.Runtime.Serialization.Formatters.Execution;

#endregion

namespace System.Runtime.Serialization.Formatters.VCalls
{
    internal class Cast : IVCall
    {
        public byte Code => NeonVMConstants.VCALL_CAST;

        public void Load(NeonVMContext ctx, out ExecutionState state)
        {
            var sp = ctx.Registers[NeonVMConstants.REG_SP].U4;
            var typeSlot = ctx.Stack[sp--];
            var valSlot = ctx.Stack[sp];

            var castclass = (typeSlot.U4 & 0x80000000) != 0;
            var castType = (Type) ctx.Instance.Data.LookupReference(typeSlot.U4 & ~0x80000000);
            if(Type.GetTypeCode(castType) == TypeCode.String && valSlot.O == null)
            {
                valSlot.O = ctx.Instance.Data.LookupString(valSlot.U4);
            }
            else if(valSlot.O == null)
            {
                valSlot.O = null;
            }
            else if(!castType.IsInstanceOfType(valSlot.O))
            {
                valSlot.O = null;
                if(castclass)
                    throw new InvalidCastException();
            }
            ctx.Stack[sp] = valSlot;

            ctx.Stack.SetTopPosition(sp);
            ctx.Registers[NeonVMConstants.REG_SP].U4 = sp;
            state = ExecutionState.Next;
        }
    }
}
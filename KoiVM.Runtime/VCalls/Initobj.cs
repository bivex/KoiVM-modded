#region

using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Dynamic;
using System.Runtime.Serialization.Formatters.Execution;
using System.Runtime.Serialization.Formatters.Execution.Internal;

#endregion

namespace System.Runtime.Serialization.Formatters.VCalls
{
    internal class Initobj : IVCall
    {
        public byte Code => NeonVMConstants.VCALL_INITOBJ;

        public void Load(NeonVMContext ctx, out ExecutionState state)
        {
            var sp = ctx.Registers[NeonVMConstants.REG_SP].U4;
            var typeSlot = ctx.Stack[sp--];
            var addrSlot = ctx.Stack[sp--];

            var type = (Type) ctx.Instance.Data.LookupReference(typeSlot.U4);
            if(addrSlot.O is IReference)
            {
                var reference = (IReference) addrSlot.O;
                var slot = new NeonVMSlot();
                if(type.IsValueType)
                {
                    object def = null;
                    if(Nullable.GetUnderlyingType(type) == null)
                        def = FormatterServices.GetUninitializedObject(type);
                    slot.O = ValueTypeBox.Box(def, type);
                }
                else
                {
                    slot.O = null;
                }
                reference.SetValue(ctx, slot, PointerType.OBJECT);
            }
            else
            {
                throw new NotSupportedException();
            }

            ctx.Stack.SetTopPosition(sp);
            ctx.Registers[NeonVMConstants.REG_SP].U4 = sp;
            state = ExecutionState.Next;
        }
    }
}
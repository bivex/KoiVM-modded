#region

using System;

#endregion

namespace System.Runtime.Serialization.Formatters.Execution
{
    internal interface IReference
    {
        NeonVMSlot GetValue(NeonVMContext ctx, PointerType type);
        void SetValue(NeonVMContext ctx, NeonVMSlot slot, PointerType type);
        IReference Add(uint value);
        IReference Add(ulong value);

        void ToTypedReference(NeonVMContext ctx, TypedRefPtr typedRef, Type type);
    }
}
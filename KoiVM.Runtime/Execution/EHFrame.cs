#region

using System;

#endregion

namespace System.Runtime.Serialization.Formatters.Execution
{
    internal struct EHFrame
    {
        public byte EHType;
        public ulong FilterAddr;
        public ulong HandlerAddr;
        public Type CatchType;

        public NeonVMSlot BP;
        public NeonVMSlot SP;
    }
}
#region

using System.Runtime.Serialization.Formatters.Dynamic;
using System.Runtime.Serialization.Formatters.Execution;

#endregion

namespace System.Runtime.Serialization.Formatters.OpCodes
{
    internal class Nop : IOpCode
    {
        public byte Code => NeonVMConstants.OP_NOP;

        public void Load(NeonVMContext ctx, out ExecutionState state)
        {
            state = ExecutionState.Next;
        }
    }
}
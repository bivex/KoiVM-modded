#region

using System.Runtime.Serialization.Formatters.Dynamic;
using System.Runtime.Serialization.Formatters.Execution;

#endregion

namespace System.Runtime.Serialization.Formatters.VCalls
{
    internal class Exit : IVCall
    {
        public byte Code => NeonVMConstants.VCALL_EXIT;

        public void Load(NeonVMContext ctx, out ExecutionState state)
        {
            state = ExecutionState.Exit;
        }
    }
}
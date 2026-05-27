#region

using KoiVM.Runtime.Dynamic;
using KoiVM.Runtime.Execution;

#endregion

namespace KoiVM.Runtime.VCalls
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
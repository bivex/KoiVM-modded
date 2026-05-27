#region

using KoiVM.Runtime.Dynamic;
using KoiVM.Runtime.Execution;

#endregion

namespace KoiVM.Runtime.OpCodes
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
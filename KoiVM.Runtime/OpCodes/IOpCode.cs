#region

using KoiVM.Runtime.Execution;

#endregion

namespace KoiVM.Runtime.OpCodes
{
    internal interface IOpCode
    {
        byte Code
        {
            get;
        }

        void Load(NeonVMContext ctx, out ExecutionState state);
    }
}
#region

using KoiVM.Runtime.Execution;

#endregion

namespace KoiVM.Runtime.VCalls
{
    internal interface IVCall
    {
        byte Code
        {
            get;
        }

        void Load(NeonVMContext ctx, out ExecutionState state);
    }
}
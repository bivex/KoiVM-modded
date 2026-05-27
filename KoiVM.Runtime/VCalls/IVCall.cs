#region

using System.Runtime.Serialization.Formatters.Execution;

#endregion

namespace System.Runtime.Serialization.Formatters.VCalls
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
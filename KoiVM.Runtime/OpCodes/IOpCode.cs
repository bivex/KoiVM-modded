#region

using System.Runtime.Serialization.Formatters.Execution;

#endregion

namespace System.Runtime.Serialization.Formatters.OpCodes
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
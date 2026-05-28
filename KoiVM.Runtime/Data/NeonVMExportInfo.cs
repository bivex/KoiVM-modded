#region

using System.Reflection;

#endregion

namespace System.Runtime.Serialization.Formatters.Data
{
    internal struct NeonVMExportInfo
    {
        public unsafe NeonVMExportInfo(ref byte* ptr, Module module)
        {
            CodeOffset = *(uint*) ptr;
            ptr += 4;
            if(CodeOffset != 0)
            {
                var key = *(uint*) ptr;
                EntryKey = key & 0xffffff;
                OpCodeSeed = (byte) (key >> 24);
                ptr += 4;
            }
            else
            {
                EntryKey = 0;
            }
            Signature = new NeonVMFuncSig(ref ptr, module);
        }

        public readonly uint CodeOffset;
        public readonly uint EntryKey;
        public readonly byte OpCodeSeed;
        public readonly NeonVMFuncSig Signature;
    }
}
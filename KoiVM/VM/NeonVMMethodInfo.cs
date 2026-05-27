#region

using System.Collections.Generic;
using KoiVM.CFG;

#endregion

namespace KoiVM.VM
{
    public class NeonVMMethodInfo
    {
        public readonly Dictionary<IBasicBlock, VMBlockKey> BlockKeys;
        public readonly HashSet<NeonVMRegisters> UsedRegister;
        public byte EntryKey;
        public byte ExitKey;

        public ScopeBlock RootScope;

        public NeonVMMethodInfo()
        {
            BlockKeys = new Dictionary<IBasicBlock, VMBlockKey>();
            UsedRegister = new HashSet<NeonVMRegisters>();
        }
    }

    public struct VMBlockKey
    {
        public byte EntryKey;
        public byte ExitKey;
    }
}
#region

using System.Collections.Generic;
using Microsoft.VisualBasic.Devices;
using System.Runtime.Serialization.Formatters.Dynamic;

#endregion

namespace System.Runtime.Serialization.Formatters.Execution
{
    internal class NeonVMContext
    {
        private const int NumRegisters = 16;
        public readonly List<EHFrame> EHStack = new List<EHFrame>();
        public readonly List<EHState> EHStates = new List<EHState>();
        public readonly NeonVMInstance Instance;

        public readonly NeonVMSlot[] Registers = new NeonVMSlot[16];
        public readonly NeonVMStack Stack = new NeonVMStack();

        public NeonVMContext(NeonVMInstance inst)
        {
            Instance = inst;
        }

        public unsafe byte ReadByte()
        {
            var key = Registers[NeonVMConstants.REG_K1].U4;
            var ip = (byte*) Registers[NeonVMConstants.REG_IP].U8++;
            var b = (byte) (*ip ^ key);

            var multiplier = (byte)(key >> 8);
            if (multiplier == 0) multiplier = 7;
            
            key = (key & 0xFFFFFF00) | (byte) ((byte)key * multiplier + b);
            Registers[NeonVMConstants.REG_K1].U4 = key;
            return b;
        }
    }
}
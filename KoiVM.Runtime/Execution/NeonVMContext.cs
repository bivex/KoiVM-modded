#region

using System.Collections.Generic;
using Microsoft.VisualBasic.Devices;
using System.Runtime.Serialization.Formatters.Dynamic;

#endregion

namespace System.Runtime.Serialization.Formatters.Execution
{
    internal delegate void OpCodeHandler(NeonVMContext ctx, out ExecutionState state);

    internal class NeonVMContext
    {
        private const int NumRegisters = 16;
        public readonly List<EHFrame> EHStack = new List<EHFrame>();
        public readonly List<EHState> EHStates = new List<EHState>();
        public readonly NeonVMInstance Instance;
        public OpCodeHandler[] OpCodeMap;

        public readonly NeonVMSlot[] Registers = new NeonVMSlot[16];
        public readonly NeonVMStack Stack = new NeonVMStack();

        public NeonVMContext(NeonVMInstance inst)
        {
            Instance = inst;
        }

        public unsafe byte ReadByte()
        {
            var key = Registers[NeonVMConstants.REG_K1].U4;
            var ip = (byte*) Registers[NeonVMConstants.REG_IP].U8;
            var rawEncrypted = *ip;
            var b = (byte) (*ip ^ key);

            var multiplier = (byte)(key >> 8);
            
            key = (key & 0xFFFFFF00) | (byte) ((byte)key * multiplier + b);
            Registers[NeonVMConstants.REG_K1].U4 = key;
            Registers[NeonVMConstants.REG_IP].U8++;
            return b;
        }
    }
}
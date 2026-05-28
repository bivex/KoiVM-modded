#region

using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.OpCodes;
using System.Runtime.Serialization.Formatters.Execution;

#endregion

namespace System.Runtime.Serialization.Formatters.Data
{
    internal static class OpCodeMap
    {
        private static readonly Dictionary<byte, IOpCode> opCodes;

        static OpCodeMap()
        {
            opCodes = new Dictionary<byte, IOpCode>();
            int typeCount = 0;
            int assignCount = 0;
            foreach(var type in typeof(OpCodeMap).Assembly.GetTypes())
                if(typeof(IOpCode).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    typeCount++;
                    try {
                        var opCode = (IOpCode) Activator.CreateInstance(type);
                        var code = opCode.Code;
                        if(opCodes.ContainsKey(code))
                            Console.WriteLine("[OPCODE-DUP] Code=" + code + " type=" + type.FullName + " (existing: " + opCodes[code].GetType().FullName + ")");
                        opCodes[code] = opCode;
                        assignCount++;
                    } catch(Exception ex) {
                        Console.WriteLine("[OPCODE-ERR] type=" + type.FullName + " ex=" + ex.Message);
                    }
                }
            Console.WriteLine("[OPCODE-MAP] types=" + typeCount + " assigned=" + assignCount + " unique codes=" + opCodes.Count);
        }

        public static IOpCode Lookup(byte code)
        {
            return opCodes[code];
        }

        public static OpCodeHandler[] GetMap(byte seed)
        {
            // Sanity check: verify NeonVMConstants are initialized
            if (Dynamic.NeonVMConstants.OP_RET == 0)
                throw new InvalidOperationException("NeonVMConstants.OP_RET is 0! Constants not initialized!");
            
            Console.WriteLine("[GETMAP-DEBUG] RET=" + Dynamic.NeonVMConstants.OP_RET);
            
            var map = new OpCodeHandler[256];
            var temp_map = new IOpCode[256];
            foreach (var entry in opCodes) temp_map[entry.Key] = entry.Value;

            var P_s = new byte[256];
            for (int i = 0; i < 256; i++) P_s[i] = (byte)i;

            var random = new Random(seed);
            for (int i = 0; i < 256; i++)
            {
                int j = random.Next(256);
                byte temp = P_s[i];
                P_s[i] = P_s[j];
                P_s[j] = temp;
            }

            int mapped = 0;
            for (int i = 0; i < 256; i++)
            {
                if (temp_map[i] != null)
                {
                    map[P_s[i]] = temp_map[i].Load;
                    mapped++;
                }
            }
            Console.WriteLine("[GETMAP] seed=" + seed + " opCodes.Count=" + opCodes.Count + " mapped=" + mapped);
            int nullCount = 0;
            for (int i = 0; i < 256; i++) if (map[i] == null) nullCount++;
            Console.WriteLine("[GETMAP] null slots=" + nullCount + "/256");
            // Dump P_s array (first 80 values)
            var psStr = new System.Text.StringBuilder();
            for (int i = 0; i < 80; i++) { if (i > 0) psStr.Append(","); psStr.Append(P_s[i]); }
            Console.WriteLine("[GETMAP-PS] P_s[0..79]: " + psStr.ToString());
            // Dump all mapped positions and the code values that map to them
            var allMapped = new System.Text.StringBuilder();
            for (int i = 0; i < 256; i++) { if (map[i] != null) { if (allMapped.Length > 0) allMapped.Append(","); allMapped.Append(i); } }
            Console.WriteLine("[GETMAP-ALL] positions: " + allMapped.ToString());
            // Dump code->handler mapping: show which code values have handlers in temp_map
            var codeStr = new System.Text.StringBuilder();
            for (int i = 0; i < 256; i++) { if (temp_map[i] != null) { if (codeStr.Length > 0) codeStr.Append(","); codeStr.Append(i + "=" + temp_map[i].GetType().Name); } }
            Console.WriteLine("[GETMAP-CODES] code->handler: " + codeStr.ToString());
            // Dump key NeonVMConstants values
            Console.WriteLine("[CONSTANTS] OP_CALL=" + Dynamic.NeonVMConstants.OP_CALL + " OP_CMP=" + Dynamic.NeonVMConstants.OP_CMP + " OP_JZ=" + Dynamic.NeonVMConstants.OP_JZ + " OP_JNZ=" + Dynamic.NeonVMConstants.OP_JNZ + " OP_JMP=" + Dynamic.NeonVMConstants.OP_JMP + " OP_SWT=" + Dynamic.NeonVMConstants.OP_SWT);
            Console.WriteLine("[CONSTANTS] OP_NOP=" + Dynamic.NeonVMConstants.OP_NOP + " OP_PUSHI_DWORD=" + Dynamic.NeonVMConstants.OP_PUSHI_DWORD + " OP_RET=" + Dynamic.NeonVMConstants.OP_RET + " OP_VCALL=" + Dynamic.NeonVMConstants.OP_VCALL);
            return map;
        }
    }
}

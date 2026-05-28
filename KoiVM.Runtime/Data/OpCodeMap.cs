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
            var codes = new System.Collections.Generic.List<byte>(opCodes.Keys);
            codes.Sort();
            Console.Write("[OPCODE-CODES-RAW]");
            foreach(var c in codes) Console.Write(" " + c);
            Console.WriteLine();
        }

        public static IOpCode Lookup(byte code)
        {
            return opCodes[code];
        }

        public static OpCodeHandler[] GetMap(byte seed)
        {
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
            // Show all mapped positions
            var positions = new System.Collections.Generic.List<int>();
            for (int i = 0; i < 256; i++) if (map[i] != null) positions.Add(i);
            Console.WriteLine("[GETMAP] positions(first30): " + string.Join(",", positions.Take(30)));
            return map;
        }
    }
}

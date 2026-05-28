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
            foreach(var type in typeof(OpCodeMap).Assembly.GetTypes())
                if(typeof(IOpCode).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    var opCode = (IOpCode) Activator.CreateInstance(type);
                    opCodes[opCode.Code] = opCode;
                }
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
            var sb = new System.Text.StringBuilder();
            for(int i = 0; i < 256; i++) if(map[i] != null) sb.Append(i + ",");
            Console.WriteLine("[GETMAP] seed=" + seed + " mapped=" + mapped + " indices=[" + sb + "]");
            return map;
        }
    }
}

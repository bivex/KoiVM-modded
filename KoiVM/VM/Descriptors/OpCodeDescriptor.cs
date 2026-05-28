#region

using System;
using System.Linq;
using KoiVM.VMIL;

#endregion

namespace KoiVM.VM
{
    public class OpCodeDescriptor
    {
        private readonly byte[] opCodeOrder = Enumerable.Range(0, 256).Select(x => (byte) x).ToArray();

        public OpCodeDescriptor(Random random)
        {
            random.Shuffle(opCodeOrder);
        }

        public byte this[ILOpCode opCode] => opCodeOrder[(int) opCode];

        public byte[] GetMapping(byte seed)
        {
            var P_s = Enumerable.Range(0, 256).Select(x => (byte)x).ToArray();
            var random = new Random(seed);
            for (int i = 0; i < 256; i++)
            {
                int j = random.Next(256);
                byte temp = P_s[i];
                P_s[i] = P_s[j];
                P_s[j] = temp;
            }
            var mapping = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                mapping[i] = P_s[opCodeOrder[i]];
            }
            return mapping;
        }
    }
}
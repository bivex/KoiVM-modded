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
            var mapping = (byte[]) opCodeOrder.Clone();
            var random = new Random(seed);
            for (int i = 0; i < 256; i++)
            {
                int j = random.Next(256);
                byte temp = mapping[i];
                mapping[i] = mapping[j];
                mapping[j] = temp;
            }
            return mapping;
        }
    }
}
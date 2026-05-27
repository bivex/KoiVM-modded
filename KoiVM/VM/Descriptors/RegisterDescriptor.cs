#region

using System;
using System.Linq;

#endregion

namespace KoiVM.VM
{
    public class RegisterDescriptor
    {
        private readonly byte[] regOrder = Enumerable.Range(0, (int) NeonVMRegisters.Max).Select(x => (byte) x).ToArray();

        public RegisterDescriptor(Random random)
        {
            random.Shuffle(regOrder);
        }

        public byte this[NeonVMRegisters reg] => regOrder[(int) reg];
    }
}
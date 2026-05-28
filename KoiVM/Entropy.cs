using System;

namespace KoiVM
{
    internal static class Entropy
    {
        public static byte DeriveByte(int seed, uint id, string salt = null)
        {
            uint h = (uint)seed ^ id;
            if (salt != null)
            {
                foreach (char c in salt) h = h * 31 + c;
            }
            h = (h ^ (h >> 16)) * 0x85ebca6b;
            h = (h ^ (h >> 13)) * 0xc2b2ae35;
            h = h ^ (h >> 16);
            return (byte)h;
        }

        public static uint DeriveUInt(int seed, uint id, string salt = null)
        {
            uint h = (uint)seed ^ id;
            if (salt != null)
            {
                foreach (char c in salt) h = h * 31 + c;
            }
            h = (h ^ (h >> 16)) * 0x85ebca6b;
            h = (h ^ (h >> 13)) * 0xc2b2ae35;
            h = h ^ (h >> 16);
            return h;
        }

        public static byte ModInverse(byte n)
        {
            // For byte (mod 256), the inverse exists if n is odd.
            // We use the property that n^127 is the inverse mod 256 if n is odd.
            // Or just a simple loop/lookup since it's only 256 values.
            if (n % 2 == 0) return 0;
            byte res = 0;
            for (int i = 0; i < 256; i++)
            {
                if ((byte)(n * i) == 1)
                {
                    res = (byte)i;
                    break;
                }
            }
            return res;
        }
    }
}

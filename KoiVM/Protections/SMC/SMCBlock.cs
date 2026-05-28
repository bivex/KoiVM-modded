#region

using System;
using System.IO;
using System.Security.Cryptography;
using dnlib.DotNet;
using KoiVM.AST;
using KoiVM.AST.IL;
using KoiVM.RT;

#endregion

namespace KoiVM.Protections.SMC
{
    internal class SMCBlock : ILBlock
    {
        internal static readonly InstrAnnotation DwordCount = new InstrAnnotation("SMC_DWCOUNT");
        internal static readonly InstrAnnotation BlockOffset = new InstrAnnotation("SMC_BLKOFF");
        internal static readonly InstrAnnotation MethodSeed1 = new InstrAnnotation("SMC_MS1");
        internal static readonly InstrAnnotation MethodSeed2 = new InstrAnnotation("SMC_MS2");
        internal static readonly InstrAnnotation LcgMult = new InstrAnnotation("SMC_LM");
        internal static readonly InstrAnnotation LcgAdd = new InstrAnnotation("SMC_LA");
        internal static readonly InstrAnnotation AddressPart1 = new InstrAnnotation("SMC_PART1");
        internal static readonly InstrAnnotation AddressPart2 = new InstrAnnotation("SMC_PART2");

        public SMCBlock(int id, ILInstrList content)
            : base(id, content)
        {
        }

        public uint MethodSeed1Value { get; set; }
        public uint MethodSeed2Value { get; set; }
        public ILImmediate BlockOffsetOperand { get; set; }
        public ILImmediate DwordCountOperand { get; set; }

        public override IKoiChunk CreateChunk(NeonVMRuntime rt, MethodDef method)
        {
            return new SMCBlockChunk(rt, method, this);
        }
    }

    internal class SMCBlockChunk : BasicBlockChunk, IKoiChunk
    {
        private uint blockOffset;

        public SMCBlockChunk(NeonVMRuntime rt, MethodDef method, SMCBlock block)
            : base(rt, method, block)
        {
        }

        uint IKoiChunk.Length
        {
            get
            {
                int paddedLen = (int)base.Length;
                paddedLen = (paddedLen + 15) & ~15;
                return (uint)paddedLen;
            }
        }

        void IKoiChunk.OnOffsetComputed(uint offset)
        {
            blockOffset = offset;
            var block = (SMCBlock) Block;

            block.BlockOffsetOperand.Value = (int) offset;
            int paddedLen = (int) base.Length;
            paddedLen = (paddedLen + 15) & ~15;
            block.DwordCountOperand.Value = paddedLen;

            base.OnOffsetComputed(offset);
        }

        byte[] IKoiChunk.GetData()
        {
            var data = GetData();
            var block = (SMCBlock) Block;

            int paddedLen = (data.Length + 15) & ~15;
            var padded = new byte[paddedLen];
            Array.Copy(data, padded, data.Length);

            uint seed = block.MethodSeed1Value ^ blockOffset;
            byte[] key = new byte[16];
            for (int i = 0; i < 16; i++) key[i] = Entropy.DeriveByte((int)seed, (uint)i, "SMC_AES_KEY");

            byte[] iv = new byte[16];
            for (int i = 0; i < 8; i++)
            {
                iv[i] = (byte)(blockOffset >> (i * 8));
                iv[i + 8] = (byte)(blockOffset >> (i * 8));
            }

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;

                using (var encryptor = aes.CreateEncryptor())
                {
                    return encryptor.TransformFinalBlock(padded, 0, paddedLen);
                }
            }
        }

        static uint DeriveKey(uint seed1, uint seed2, uint offset)
        {
            uint key = seed1 ^ offset;
            key = key * 0x6C078965 + 0x01;
            key ^= seed2;
            key = key * 0x6C078965 + 0x01;
            key ^= seed1;
            key = key * 0x6C078965 + 0x01;
            key ^= seed2;
            key = key * 0x6C078965 + 0x01;
            return key;
        }
    }

    internal class SMCBlockRef : ILRelReference
    {
        public SMCBlockRef(IHasOffset target, IHasOffset relBase, uint key)
            : base(target, relBase)
        {
            Key = key;
        }

        public uint Key { get; set; }

        public override uint Resolve(NeonVMRuntime runtime)
        {
            return base.Resolve(runtime) ^ Key;
        }
    }
}

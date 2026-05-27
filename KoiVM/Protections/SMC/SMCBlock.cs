#region

using System;
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

        public uint LcgMultValue { get; set; }
        public uint LcgAddValue { get; set; }
        public uint MethodSeed1Value { get; set; }
        public uint MethodSeed2Value { get; set; }

        public ILImmediate DwordCountOperand { get; set; }
        public ILImmediate BlockOffsetOperand { get; set; }

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

        uint IKoiChunk.Length => base.Length;

        void IKoiChunk.OnOffsetComputed(uint offset)
        {
            blockOffset = offset;
            var block = (SMCBlock) Block;

            block.BlockOffsetOperand.Value = (int) offset;
            int paddedLen = (int) base.Length;
            paddedLen = (paddedLen + 3) & ~3;
            block.DwordCountOperand.Value = paddedLen / 4;

            base.OnOffsetComputed(offset);
        }

        byte[] IKoiChunk.GetData()
        {
            var data = GetData();
            var block = (SMCBlock) Block;

            uint initKey = DeriveKey(block.MethodSeed1Value, block.MethodSeed2Value, blockOffset);

            int paddedLen = (data.Length + 3) & ~3;
            var padded = new byte[paddedLen];
            Array.Copy(data, padded, data.Length);

            uint key = initKey;
            int dwordCount = paddedLen / 4;
            for(var i = 0; i < dwordCount; i++)
            {
                uint dword = (uint) (padded[i * 4] | (padded[i * 4 + 1] << 8) |
                                     (padded[i * 4 + 2] << 16) | (padded[i * 4 + 3] << 24));
                dword ^= key;
                padded[i * 4] = (byte) dword;
                padded[i * 4 + 1] = (byte) (dword >> 8);
                padded[i * 4 + 2] = (byte) (dword >> 16);
                padded[i * 4 + 3] = (byte) (dword >> 24);

                key = key * block.LcgMultValue + block.LcgAddValue;
            }

            return padded;
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

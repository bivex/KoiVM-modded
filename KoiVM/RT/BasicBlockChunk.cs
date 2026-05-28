#region

using System.Diagnostics;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using KoiVM.AST;
using KoiVM.AST.IL;
using KoiVM.VMIL;

#endregion

namespace KoiVM.RT
{
    internal class BasicBlockChunk : IKoiChunk
    {
        private readonly MethodDef method;
        private readonly NeonVMRuntime rt;

        public BasicBlockChunk(NeonVMRuntime rt, MethodDef method, ILBlock block)
        {
            this.rt = rt;
            this.method = method;
            Block = block;
            Length = rt.serializer.ComputeLength(block);
        }

        public ILBlock Block
        {
            get;
            set;
        }

        public uint Length
        {
            get;
            set;
        }

        public void OnOffsetComputed(uint offset)
        {
            var len = rt.serializer.ComputeOffset(Block, offset);
            Debug.Assert(len - offset == Length);
        }

        public byte[] GetData()
        {
            var stream = new MemoryStream();
            rt.serializer.WriteData(method, Block, new BinaryWriter(stream));
            return Encrypt(stream.ToArray());
        }

        private byte[] Encrypt(byte[] data)
        {
            var blockKey = rt.Descriptor.Data.LookupInfo(method).BlockKeys[Block];
            var currentKey = blockKey.EntryKey;

            var firstInstr = Block.Content[0];
            var lastInstr = Block.Content[Block.Content.Count - 1];
            foreach(var instr in Block.Content)
            {
                var instrStart = instr.Offset - firstInstr.Offset;
                var instrEnd = instrStart + rt.serializer.ComputeLength(instr);

                var multiplier = (byte)(currentKey >> 8);

                // Encrypt OpCode
                {
                    var b = data[instrStart];
                    data[instrStart] ^= (byte)currentKey;
                    currentKey = (currentKey & 0xFFFFFF00) | (byte) ((byte)currentKey * multiplier + b);
                }

                uint? fixupTarget = null;
                if(instr.Annotation == InstrAnnotation.JUMP ||
                   instr == lastInstr)
                {
                    fixupTarget = blockKey.ExitKey;
                }
                else if(instr.OpCode == ILOpCode.LEAVE)
                {
                    var eh = ((EHInfo) instr.Annotation).ExceptionHandler;
                    if(eh.HandlerType == ExceptionHandlerType.Finally) fixupTarget = blockKey.ExitKey;
                }
                else if(instr.OpCode == ILOpCode.CALL)
                {
                    var callInfo = (InstrCallInfo) instr.Annotation;
                    var info = rt.Descriptor.Data.LookupInfo((MethodDef) callInfo.Method);
                    fixupTarget = info.EntryKey;
                }

                if(fixupTarget != null)
                {
                    var invMultiplier = (byte)(currentKey >> 16);
                    var fixup = CalculateFixupByte(fixupTarget.Value, data, currentKey, instrStart + 1, instrEnd, multiplier, invMultiplier);
                    data[instrStart + 1] = fixup;
                }

                // Encrypt rest of instruction
                for(var i = instrStart + 1; i < instrEnd; i++)
                {
                    var b = data[i];
                    data[i] ^= (byte)currentKey;
                    currentKey = (currentKey & 0xFFFFFF00) | (byte) ((byte)currentKey * multiplier + b);
                }
                if(fixupTarget != null)
                    Debug.Assert((uint)currentKey == fixupTarget.Value);

                if(instr.OpCode == ILOpCode.CALL)
                {
                    var callInfo = (InstrCallInfo) instr.Annotation;
                    var info = rt.Descriptor.Data.LookupInfo((MethodDef) callInfo.Method);
                    currentKey = info.ExitKey;
                }
            }

            return data;
        }

        private static byte CalculateFixupByte(uint target, byte[] data, uint currentKey, uint rangeStart, uint rangeEnd, byte multiplier, byte invMultiplier)
        {
            var fixupByte = (byte)target;
            for(var i = rangeEnd - 1; i > rangeStart; i--) fixupByte = (byte) ((fixupByte - data[i]) * invMultiplier);
            fixupByte -= (byte) ((byte)currentKey * multiplier);
            return fixupByte;
        }
    }
}
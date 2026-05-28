#region

using System;
using System.Security.Cryptography;
using Microsoft.VisualBasic.Devices;
using System.Runtime.Serialization.Formatters.Dynamic;
using System.Runtime.Serialization.Formatters.Execution;

#endregion

namespace System.Runtime.Serialization.Formatters.OpCodes
{
    internal class SmcAes : IOpCode
    {
        public byte Code => NeonVMConstants.OP_AES;

        public unsafe void Load(NeonVMContext ctx, out ExecutionState state)
        {
            var sp = ctx.Registers[NeonVMConstants.REG_SP].U4;
            var ptr = (byte*)ctx.Stack[sp - 1].U8;
            var count = (int)ctx.Stack[sp].U4;
            sp -= 2;
            ctx.Stack.SetTopPosition(sp);
            ctx.Registers[NeonVMConstants.REG_SP].U4 = sp;

            // Key is derived from K1 and K2
            byte[] key = new byte[16];
            ulong k1 = ctx.Registers[NeonVMConstants.REG_K1].U8;
            ulong k2 = ctx.Registers[NeonVMConstants.REG_K2].U8;
            
            for(int i = 0; i < 8; i++) {
                key[i] = (byte)(k1 >> (i * 8));
                key[i + 8] = (byte)(k2 >> (i * 8));
            }

            // IV is derived from the pointer address (deterministic per block)
            byte[] iv = new byte[16];
            ulong addr = (ulong)ptr;
            for(int i = 0; i < 8; i++) {
                iv[i] = (byte)(addr >> (i * 8));
                iv[i + 8] = (byte)(addr >> (i * 8));
            }

            using (var aes = Aes.Create()) {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;

                using (var decryptor = aes.CreateDecryptor()) {
                    byte[] data = new byte[count];
                    for(int i = 0; i < count; i++) data[i] = ptr[i];
                    
                    byte[] decrypted = decryptor.TransformFinalBlock(data, 0, count);
                    for(int i = 0; i < count; i++) ptr[i] = decrypted[i];
                }
            }

            state = ExecutionState.Next;
        }
    }
}
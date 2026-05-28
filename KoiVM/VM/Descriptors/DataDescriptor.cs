#region

using System;
using System.Collections.Generic;
using dnlib.DotNet;

#endregion

namespace KoiVM.VM
{
    public class DataDescriptor
    {
        private readonly Dictionary<MethodDef, uint> exportMap = new Dictionary<MethodDef, uint>();
        private readonly Dictionary<MethodDef, NeonVMMethodInfo> methodInfos = new Dictionary<MethodDef, NeonVMMethodInfo>();

        private uint nextRefId;
        private uint nextSigId;
        private uint nextStrId;
        private readonly Random random;
        private byte globalOpCodeSeed;
        internal byte globalMult;
        internal byte globalInv;
        private static byte sharedMult;
        private static byte sharedInv;
        private static byte sharedSeed;
        private static bool initialized;

        internal Dictionary<IMemberRef, uint> refMap = new Dictionary<IMemberRef, uint>();
        private readonly Dictionary<MethodSig, uint> sigMap = new Dictionary<MethodSig, uint>(SignatureEqualityComparer.Instance);
        internal List<FuncSigDesc> sigs = new List<FuncSigDesc>();
        internal Dictionary<string, uint> strMap = new Dictionary<string, uint>(StringComparer.Ordinal);

        public DataDescriptor(Random random)
        {
            // 0 = null, 1 = ""
            strMap[""] = 1;
            nextStrId = 2;

            nextRefId = 1;
            nextSigId = 8u * 1;

            this.random = random;
            if(!initialized)
            {
                globalOpCodeSeed = (byte)random.Next();
                globalMult = (byte)(random.Next() | 1);
                globalInv = Entropy.ModInverse(globalMult);
                sharedMult = globalMult;
                sharedInv = globalInv;
                sharedSeed = globalOpCodeSeed;
                initialized = true;
            }
            else
            {
                globalOpCodeSeed = sharedSeed;
                globalMult = sharedMult;
                globalInv = sharedInv;
            }
            Console.WriteLine("[DATADESC-CTOR] globalMult=0x" + globalMult.ToString("x2") + " globalInv=0x" + globalInv.ToString("x2") + " globalOpCodeSeed=" + globalOpCodeSeed);
        }

        public uint GetId(IMemberRef memberRef)
        {
            uint ret;
            if(!refMap.TryGetValue(memberRef, out ret))
                refMap[memberRef] = ret = nextRefId++;
            return ret;
        }

        public void ReplaceReference(IMemberRef old, IMemberRef @new)
        {
            uint id;
            if(!refMap.TryGetValue(old, out id))
                return;
            refMap.Remove(old);
            refMap[@new] = id;
        }

        public uint GetId(string str)
        {
            uint ret;
            if(!strMap.TryGetValue(str, out ret))
                strMap[str] = ret = nextStrId++;
            return ret;
        }

        public uint GetId(ITypeDefOrRef declType, MethodSig methodSig)
        {
            uint ret;
            if(!sigMap.TryGetValue(methodSig, out ret))
            {
                var id = nextSigId++;
                sigMap[methodSig] = ret = id;
                sigs.Add(new FuncSigDesc(id, declType, methodSig));
            }
            return ret;
        }

        public uint GetExportId(MethodDef method)
        {
            uint ret;
            if(!exportMap.TryGetValue(method, out ret))
            {
                var id = nextSigId++;
                exportMap[method] = ret = id;
                sigs.Add(new FuncSigDesc(id, method));
            }
            return ret;
        }

        public NeonVMMethodInfo LookupInfo(MethodDef method)
        {
            NeonVMMethodInfo ret;
            bool cached = methodInfos.TryGetValue(method, out ret);
            if(!cached)
            {
                var seed = random.Next();
                // All methods share the same multiplier so CALL/RET key transitions
                // (which can only fixup byte 0) work correctly.
                uint entryRolling = Entropy.DeriveByte(seed, (uint)method.Rid, "method_entry");
                uint exitRolling = Entropy.DeriveByte(seed, (uint)method.Rid, "method_exit");

                ret = new NeonVMMethodInfo
                {
                    EntryKey = entryRolling | ((uint)globalMult << 8) | ((uint)globalInv << 16),
                    ExitKey = exitRolling | ((uint)globalMult << 8) | ((uint)globalInv << 16),
                    OpCodeSeed = globalOpCodeSeed
                };
                methodInfos[method] = ret;
                Console.WriteLine("[LOOKUP-NEW] method=" + method.Name + " rid=" + method.Rid +
                    " EntryKey=0x" + ret.EntryKey.ToString("x8") + " OpCodeSeed=" + ret.OpCodeSeed +
                    " seed=" + seed);
            }
            else if(method.Name == "INIT")
            {
                Console.WriteLine("[LOOKUP-CACHED-INIT] EntryKey=0x" + ret.EntryKey.ToString("x8") +
                    " OpCodeSeed=" + ret.OpCodeSeed);
            }
            return ret;
        }

        public void SetInfo(MethodDef method, NeonVMMethodInfo info)
        {
            methodInfos[method] = info;
        }

        internal class FuncSigDesc
        {
            public readonly ITypeDefOrRef DeclaringType;
            public readonly FuncSig FuncSig;
            public readonly uint Id;
            public readonly MethodDef Method;
            public readonly MethodSig Signature;

            public FuncSigDesc(uint id, MethodDef method)
            {
                Id = id;
                Method = method;
                DeclaringType = method.DeclaringType;
                Signature = method.MethodSig;
                FuncSig = new FuncSig();
            }

            public FuncSigDesc(uint id, ITypeDefOrRef declType, MethodSig sig)
            {
                Id = id;
                Method = null;
                DeclaringType = declType;
                Signature = sig;
                FuncSig = new FuncSig();
            }
        }
    }
}
#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Data;
using System.Runtime.Serialization.Formatters.Dynamic;
using System.Runtime.Serialization.Formatters.Execution;
using System.Runtime.Serialization.Formatters.Execution.Internal;

#endregion

namespace Microsoft.VisualBasic.Devices
{
    internal unsafe class NeonVMInstance
    {
        [ThreadStatic] private static Dictionary<Module, NeonVMInstance> instances;
        private static readonly object initLock = new object();
        private static readonly Dictionary<Module, int> initialized = new Dictionary<Module, int>();

        private readonly Stack<NeonVMContext> ctxStack = new Stack<NeonVMContext>();
        private NeonVMContext currentCtx;

        private NeonVMInstance(NeonVMData data)
        {
            Data = data;
        }

        public NeonVMData Data
        {
            get;
        }

        public static NeonVMInstance Instance(uint num, Module module)
        {
            NeonVMInstance inst;
            if(instances == null) instances = new Dictionary<Module, NeonVMInstance>();
            if(!instances.TryGetValue(module, out inst))
            {
                inst = new NeonVMInstance(NeonVMData.Instance(module));
                instances[module] = inst;
                lock(initLock)
                {
                    if(!initialized.ContainsKey(module))
                    {
                        inst.Initialize();
                        initialized.Add(module, initialized.Count);
                    }
                }
            }
            return inst;
        }

        public static NeonVMInstance Instance(uint num, int id)
        {
            foreach(var entry in initialized)
                if(entry.Value == id)
                    return Instance(num, entry.Key);
            return null;
        }

        public static int GetModuleId(Module module)
        {
            return initialized[module];
        }

        private void Initialize()
        {
            var initFunc = Data.LookupExport(NeonVMConstants.HELPER_INIT);
            var codeAddr = (ulong) (Data.KoiSection + initFunc.CodeOffset);
            Load(codeAddr, initFunc.EntryKey, 0, initFunc.Signature, new object[0]);
        }

        public object Load(uint s2, uint s3, uint id, object[] arguments)
        {
            var export = Data.LookupExport(id / 5 / 63493);
            var codeAddr = (ulong) (Data.KoiSection + export.CodeOffset);
            return Load(codeAddr, export.EntryKey, export.OpCodeSeed, export.Signature, arguments);
        }

        public object Load(ulong codeAddr, uint key, uint sigId, object[] arguments)
        {
            var sig = Data.LookupExport(sigId).Signature;
            return Load(codeAddr, key, 0, sig, arguments); // sigId doesn't have seed
        }

        public void Load(uint s2, uint s3, uint id, void*[] typedRefs, void* retTypedRef)
        {
            var export = Data.LookupExport(id / 5 / 63493);
            var codeAddr = (ulong) (Data.KoiSection + export.CodeOffset);
            Load(codeAddr, export.EntryKey, export.OpCodeSeed, export.Signature, typedRefs, retTypedRef);
        }

        public void Load(ulong codeAddr, uint key, uint sigId, void*[] typedRefs, void* retTypedRef)
        {
            var sig = Data.LookupExport(sigId).Signature;
            Load(codeAddr, key, 0, sig, typedRefs, retTypedRef);
        }

        private object Load(ulong codeAddr, uint key, byte opSeed, NeonVMFuncSig sig, object[] arguments)
        {
            if(currentCtx != null)
                ctxStack.Push(currentCtx);
            currentCtx = new NeonVMContext(this);
            currentCtx.OpCodeMap = OpCodeRelocator.Relocate(OpCodeMap.GetMap(opSeed));

            try
            {
                Debug.Assert(sig.ParamTypes.Length == arguments.Length);
                currentCtx.Stack.SetTopPosition((uint) arguments.Length + 1);
                for(uint i = 0; i < arguments.Length; i++) currentCtx.Stack[i + 1] = NeonVMSlot.FromObject(arguments[i], sig.ParamTypes[i]);
                currentCtx.Stack[(uint) arguments.Length + 1] = new NeonVMSlot {U8 = 1};

                currentCtx.Registers[NeonVMConstants.REG_K1] = new NeonVMSlot {U4 = key};
                currentCtx.Registers[NeonVMConstants.REG_BP] = new NeonVMSlot {U4 = 0};
                currentCtx.Registers[NeonVMConstants.REG_SP] = new NeonVMSlot {U4 = (uint) arguments.Length + 1};
                currentCtx.Registers[NeonVMConstants.REG_IP] = new NeonVMSlot {U8 = codeAddr};
                ObjectPool.Load(currentCtx);
                Debug.Assert(currentCtx.EHStack.Count == 0);

                object retVal = null;
                if(sig.RetType != typeof(void))
                {
                    var retSlot = currentCtx.Registers[NeonVMConstants.REG_R0];
                    if(Type.GetTypeCode(sig.RetType) == TypeCode.String && retSlot.O == null)
                        retVal = Data.LookupString(retSlot.U4);
                    else
                        retVal = retSlot.ToObject(sig.RetType);
                }

                return retVal;
            }
            finally
            {
                currentCtx.Stack.FreeAllLocalloc();

                if(ctxStack.Count > 0)
                    currentCtx = ctxStack.Pop();
            }
        }

        private void Load(ulong codeAddr, uint key, byte opSeed, NeonVMFuncSig sig, void*[] arguments, void* retTypedRef)
        {
            if(currentCtx != null)
                ctxStack.Push(currentCtx);
            currentCtx = new NeonVMContext(this);
            currentCtx.OpCodeMap = OpCodeRelocator.Relocate(OpCodeMap.GetMap(opSeed));

            try
            {
                Debug.Assert(sig.ParamTypes.Length == arguments.Length);
                currentCtx.Stack.SetTopPosition((uint) arguments.Length + 1);
                for(uint i = 0; i < arguments.Length; i++)
                {
                    var paramType = sig.ParamTypes[i];
                    if(paramType.IsByRef)
                    {
                        currentCtx.Stack[i + 1] = new NeonVMSlot {O = new TypedRef(arguments[i])};
                    }
                    else
                    {
                        var typedRef = *(TypedReference*) arguments[i];
                        currentCtx.Stack[i + 1] = NeonVMSlot.FromObject(TypedReference.ToObject(typedRef), __reftype(typedRef));
                    }
                }
                currentCtx.Stack[(uint) arguments.Length + 1] = new NeonVMSlot {U8 = 1};

                currentCtx.Registers[NeonVMConstants.REG_K1] = new NeonVMSlot {U4 = key};
                currentCtx.Registers[NeonVMConstants.REG_BP] = new NeonVMSlot {U4 = 0};
                currentCtx.Registers[NeonVMConstants.REG_SP] = new NeonVMSlot {U4 = (uint) arguments.Length + 1};
                currentCtx.Registers[NeonVMConstants.REG_IP] = new NeonVMSlot {U8 = codeAddr};
                ObjectPool.Load(currentCtx);
                Debug.Assert(currentCtx.EHStack.Count == 0);

                if(sig.RetType != typeof(void))
                    if(sig.RetType.IsByRef)
                    {
                        var retRef = currentCtx.Registers[NeonVMConstants.REG_R0].O;
                        if(!(retRef is IReference))
                            throw new ExecutionEngineException();
                        ((IReference) retRef).ToTypedReference(currentCtx, retTypedRef, sig.RetType.GetElementType());
                    }
                    else
                    {
                        var retSlot = currentCtx.Registers[NeonVMConstants.REG_R0];
                        object retVal;
                        if(Type.GetTypeCode(sig.RetType) == TypeCode.String && retSlot.O == null)
                            retVal = Data.LookupString(retSlot.U4);
                        else
                            retVal = retSlot.ToObject(sig.RetType);
                        TypedReferenceHelpers.SetTypedRef(retVal, retTypedRef);
                    }
            }
            finally
            {
                currentCtx.Stack.FreeAllLocalloc();

                if(ctxStack.Count > 0)
                    currentCtx = ctxStack.Pop();
            }
        }
    }
}
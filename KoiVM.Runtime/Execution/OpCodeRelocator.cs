#region

using System;
using System.Reflection;
using System.Reflection.Emit;

#endregion

namespace System.Runtime.Serialization.Formatters.Execution
{
    internal static class OpCodeRelocator
    {
        private static readonly Module module = typeof(OpCodeRelocator).Module;
        private static readonly Random random = new Random();

        public static OpCodeHandler[] Relocate(OpCodeHandler[] original)
        {
            var relocated = new OpCodeHandler[256];
            for(var i = 0; i < 256; i++)
                if(original[i] != null)
                    relocated[i] = CreateTrampoline(original[i]);
            return relocated;
        }

        public static void RollingRelocate(OpCodeHandler[] map)
        {
            int index = random.Next(256);
            if (map[index] != null)
            {
                map[index] = CreateTrampoline(map[index]);
            }
        }

        private static OpCodeHandler CreateTrampoline(OpCodeHandler original)
        {
            var method = original.Method;
            var target = original.Target;

            // We want a unique method signature for the trampoline to have a unique entry point
            // DynamicMethod(name, returnType, parameterTypes, owner, skipVisibility)
            
            DynamicMethod dm;
            if (target != null)
            {
                dm = new DynamicMethod(
                    "NVMR_" + Guid.NewGuid().ToString("N"),
                    typeof(void),
                    new[] { target.GetType(), typeof(NeonVMContext), typeof(ExecutionState).MakeByRefType() },
                    target.GetType(),
                    true);

                var il = dm.GetILGenerator();
                il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0); // target (this)
                il.Emit(System.Reflection.Emit.OpCodes.Ldarg_1); // ctx
                il.Emit(System.Reflection.Emit.OpCodes.Ldarg_2); // out state
                il.Emit(System.Reflection.Emit.OpCodes.Callvirt, method);
                il.Emit(System.Reflection.Emit.OpCodes.Ret);

                return (OpCodeHandler)dm.CreateDelegate(typeof(OpCodeHandler), target);
            }
            else
            {
                dm = new DynamicMethod(
                    "NVMR_" + Guid.NewGuid().ToString("N"),
                    typeof(void),
                    new[] { typeof(NeonVMContext), typeof(ExecutionState).MakeByRefType() },
                    module,
                    true);

                var il = dm.GetILGenerator();
                il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0); // ctx
                il.Emit(System.Reflection.Emit.OpCodes.Ldarg_1); // out state
                il.Emit(System.Reflection.Emit.OpCodes.Call, method);
                il.Emit(System.Reflection.Emit.OpCodes.Ret);

                return (OpCodeHandler)dm.CreateDelegate(typeof(OpCodeHandler));
            }
        }
    }
}
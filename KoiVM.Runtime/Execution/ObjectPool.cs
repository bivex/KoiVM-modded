#region

using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Runtime.Serialization.Formatters.Data;
using System.Runtime.Serialization.Formatters.Dynamic;
using System.Runtime.Serialization.Formatters.Execution.Internal;

#endregion

namespace System.Runtime.Serialization.Formatters.Execution
{
    internal static class ObjectPool
    {
        private static uint rand_state = (uint) Environment.TickCount;

        public static ExecutionState Load(NeonVMContext ctx)
        {
            var state = ExecutionState.Next;
            var isAbnormal = true;
            do
            {
                try
                {
                    state = DarkInternal(ctx);
                    switch(state)
                    {
                        case ExecutionState.Throw:
                        {
                            var sp = ctx.Registers[NeonVMConstants.REG_SP].U4;
                            var ex = ctx.Stack[sp--];
                            ctx.Registers[NeonVMConstants.REG_SP].U4 = sp;
                            DisposeItem(ctx, ex.O);
                            break;
                        }
                        case ExecutionState.Rethrow:
                        {
                            var sp = ctx.Registers[NeonVMConstants.REG_SP].U4;
                            var ex = ctx.Stack[sp--];
                            ctx.Registers[NeonVMConstants.REG_SP].U4 = sp;
                            HandleRethrow(ctx, ex.O);
                            return state;
                        }
                    }
                    isAbnormal = false;
                }
                catch(Exception ex)
                {
                    // Patched to catch object
                    SetupEHState(ctx, ex);
                    isAbnormal = false;
                }
                finally
                {
                    if(isAbnormal)
                    {
                        HandleAbnormalExit(ctx);
                        state = ExecutionState.Exit;
                    }
                    else if(ctx.EHStates.Count > 0)
                    {
                        do
                        {
                            HandleEH(ctx, ref state);
                        } while(state == ExecutionState.Rethrow);
                    }
                }
            } while(state != ExecutionState.Exit);
            return state;
        }

        private static Exception Throw(object obj)
        {
            return null;
        }

        private static ExecutionState DarkInternal(NeonVMContext ctx)
        {
            ExecutionState state;
            int counter = 0;
            while(true)
            {
                var op = ctx.ReadByte();
                var p = ctx.ReadByte(); // For key fixup
                ctx.OpCodeMap[op](ctx, out state);

                if (++counter % 10 == 0) OpCodeRelocator.RollingRelocate(ctx.OpCodeMap);

                if(ctx.Registers[NeonVMConstants.REG_IP].U8 == 1)
                    state = ExecutionState.Exit;

                if(state != ExecutionState.Next)
                    return state;
            }
        }

        private static void SetupEHState(NeonVMContext ctx, object ex)
        {
            EHState ehState;
            if(ctx.EHStates.Count != 0)
            {
                ehState = ctx.EHStates[ctx.EHStates.Count - 1];
                if(ehState.CurrentFrame != null)
                {
                    if(ehState.CurrentProcess == EHState.EHProcess.Searching) ctx.Registers[NeonVMConstants.REG_R1].U1 = 0;
                    else if(ehState.CurrentProcess == EHState.EHProcess.Unwinding) ehState.ExceptionObj = ex;
                    return;
                }
            }
            ehState = new EHState
            {
                OldBP = ctx.Registers[NeonVMConstants.REG_BP],
                OldSP = ctx.Registers[NeonVMConstants.REG_SP],
                ExceptionObj = ex,
                CurrentProcess = EHState.EHProcess.Searching,
                CurrentFrame = null,
                HandlerFrame = null
            };
            ctx.EHStates.Add(ehState);
        }

        private static void HandleRethrow(NeonVMContext ctx, object ex)
        {
            if(ctx.EHStates.Count > 0)
                SetupEHState(ctx, ex);
            else
                DisposeItem(ctx, ex);
        }

        private static unsafe string GetValue(NeonVMContext ctx)
        {
            var ip = (uint) (ctx.Registers[NeonVMConstants.REG_IP].U8 - (ulong) ctx.Instance.Data.KoiSection);
            ulong key = (uint) (new object().GetHashCode() + Environment.TickCount) | 1;
            return (((ip * key) << 32) | (key & ~1UL)).ToString("x16");
        }

        private static unsafe string ResetPool(NeonVMContext ctx)
        {
            var ip = (uint) (ctx.Registers[NeonVMConstants.REG_IP].U8 - (ulong) ctx.Instance.Data.KoiSection);
            var bp = ctx.Registers[NeonVMConstants.REG_BP].U4;
            var sb = new StringBuilder();
            do
            {
                rand_state = rand_state * 1664525 + 1013904223;
                ulong key = rand_state | 1;
                sb.AppendFormat("|{0:x16}", ((ip * key) << 32) | (key & ~1UL));
                if(bp > 1)
                {
                    ip = (uint) (ctx.Stack[bp - 1].U8 - (ulong) ctx.Instance.Data.KoiSection);
                    var bpRef = ctx.Stack[bp].O as StackRef;
                    if(bpRef == null)
                        break;
                    bp = bpRef.StackPos;
                }
                else
                {
                    break;
                }
            } while(bp > 0);
            return sb.ToString(1, sb.Length - 1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DisposeItem(NeonVMContext ctx, object ex)
        {
            if(ex is Exception) EHHelper.Rethrow((Exception) ex, GetValue(ctx));
            throw Throw(ex);
        }

        private static void HandleEH(NeonVMContext ctx, ref ExecutionState state)
        {
            var ehState = ctx.EHStates[ctx.EHStates.Count - 1];
            switch(ehState.CurrentProcess)
            {
                case EHState.EHProcess.Searching:
                {
                    if(ehState.CurrentFrame != null)
                    {
                        // Return from filter
                        var filterResult = ctx.Registers[NeonVMConstants.REG_R1].U1 != 0;
                        if(filterResult)
                        {
                            ehState.CurrentProcess = EHState.EHProcess.Unwinding;
                            ehState.HandlerFrame = ehState.CurrentFrame;
                            ehState.CurrentFrame = ctx.EHStack.Count;
                            state = ExecutionState.Next;
                            goto case EHState.EHProcess.Unwinding;
                        }
                        ehState.CurrentFrame--;
                    }
                    else
                    {
                        ehState.CurrentFrame = ctx.EHStack.Count - 1;
                    }

                    var exType = ehState.ExceptionObj.GetType();
                    for(; ehState.CurrentFrame >= 0 && ehState.HandlerFrame == null; ehState.CurrentFrame--)
                    {
                        var frame = ctx.EHStack[ehState.CurrentFrame.Value];
                        if(frame.EHType == NeonVMConstants.EH_FILTER)
                        {
                            // Run filter
                            var sp = ehState.OldSP.U4;
                            ctx.Stack.SetTopPosition(++sp);
                            ctx.Stack[sp] = new NeonVMSlot {O = ehState.ExceptionObj};
                            ctx.Registers[NeonVMConstants.REG_K1].U1 = 0;
                            ctx.Registers[NeonVMConstants.REG_SP].U4 = sp;
                            ctx.Registers[NeonVMConstants.REG_BP] = frame.BP;
                            ctx.Registers[NeonVMConstants.REG_IP].U8 = frame.FilterAddr;
                            break;
                        }
                        if(frame.EHType == NeonVMConstants.EH_CATCH)
                            if(frame.CatchType.IsAssignableFrom(exType))
                            {
                                ehState.CurrentProcess = EHState.EHProcess.Unwinding;
                                ehState.HandlerFrame = ehState.CurrentFrame;
                                ehState.CurrentFrame = ctx.EHStack.Count;
                                goto case EHState.EHProcess.Unwinding;
                            }
                    }
                    if(ehState.CurrentFrame == -1 && ehState.HandlerFrame == null)
                    {
                        ctx.EHStates.RemoveAt(ctx.EHStates.Count - 1);
                        state = ExecutionState.Rethrow;
                        if(ctx.EHStates.Count == 0)
                            HandleRethrow(ctx, ehState.ExceptionObj);
                    }
                    else
                    {
                        state = ExecutionState.Next;
                    }
                    break;
                }
                case EHState.EHProcess.Unwinding:
                {
                    ehState.CurrentFrame--;
                    int i;
                    for(i = ehState.CurrentFrame.Value; i > ehState.HandlerFrame.Value; i--)
                    {
                        var frame = ctx.EHStack[i];
                        ctx.EHStack.RemoveAt(i);
                        if(frame.EHType == NeonVMConstants.EH_FAULT || frame.EHType == NeonVMConstants.EH_FINALLY)
                        {
                            // Run finally
                            SetupFinallyFrame(ctx, frame);
                            break;
                        }
                    }
                    ehState.CurrentFrame = i;

                    if(ehState.CurrentFrame == ehState.HandlerFrame)
                    {
                        var frame = ctx.EHStack[ehState.HandlerFrame.Value];
                        ctx.EHStack.RemoveAt(ehState.HandlerFrame.Value);
                        // Run handler
                        frame.SP.U4++;
                        ctx.Stack.SetTopPosition(frame.SP.U4);
                        ctx.Stack[frame.SP.U4] = new NeonVMSlot {O = ehState.ExceptionObj};

                        ctx.Registers[NeonVMConstants.REG_K1].U1 = 0;
                        ctx.Registers[NeonVMConstants.REG_SP] = frame.SP;
                        ctx.Registers[NeonVMConstants.REG_BP] = frame.BP;
                        ctx.Registers[NeonVMConstants.REG_IP].U8 = frame.HandlerAddr;

                        ctx.EHStates.RemoveAt(ctx.EHStates.Count - 1);
                    }
                    state = ExecutionState.Next;
                    break;
                }
                default:
                    throw new ExecutionEngineException();
            }
        }

        private static void HandleAbnormalExit(NeonVMContext ctx)
        {
            var oldBP = ctx.Registers[NeonVMConstants.REG_BP];
            var oldSP = ctx.Registers[NeonVMConstants.REG_SP];

            for(var i = ctx.EHStack.Count - 1; i >= 0; i--)
            {
                var frame = ctx.EHStack[i];
                if(frame.EHType == NeonVMConstants.EH_FAULT || frame.EHType == NeonVMConstants.EH_FINALLY)
                {
                    SetupFinallyFrame(ctx, frame);
                    Load(ctx);
                }
            }
            ctx.EHStack.Clear();
        }

        private static void SetupFinallyFrame(NeonVMContext ctx, EHFrame frame)
        {
            frame.SP.U4++;
            ctx.Registers[NeonVMConstants.REG_K1].U1 = 0;
            ctx.Registers[NeonVMConstants.REG_SP] = frame.SP;
            ctx.Registers[NeonVMConstants.REG_BP] = frame.BP;
            ctx.Registers[NeonVMConstants.REG_IP].U8 = frame.HandlerAddr;

            ctx.Stack[frame.SP.U4] = new NeonVMSlot {U8 = 1};
        }
    }
}
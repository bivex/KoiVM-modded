#region

using What_a_great_VM;
using KoiVM.Runtime.Dynamic;
using KoiVM.Runtime.Execution;

#endregion

namespace KoiVM.Runtime.OpCodes
{
    internal class ShrDword : IOpCode
    {
        public byte Code => NeonVMConstants.OP_SHR_DWORD;

        public void Load(NeonVMContext ctx, out ExecutionState state)
        {
            var sp = ctx.Registers[NeonVMConstants.REG_SP].U4;
            var op1Slot = ctx.Stack[sp - 1];
            var op2Slot = ctx.Stack[sp];
            sp -= 1;
            ctx.Stack.SetTopPosition(sp);
            ctx.Registers[NeonVMConstants.REG_SP].U4 = sp;

            var fl = ctx.Registers[NeonVMConstants.REG_FL].U1;
            var slot = new NeonVMSlot();
            if((fl & NeonVMConstants.FL_UNSIGNED) != 0)
                slot.U4 = op1Slot.U4 >> (int) op2Slot.U4;
            else
                slot.U4 = (uint) ((int) op1Slot.U4 >> (int) op2Slot.U4);
            ctx.Stack[sp] = slot;

            var mask = (byte) (NeonVMConstants.FL_ZERO | NeonVMConstants.FL_SIGN | NeonVMConstants.FL_UNSIGNED);
            Utils.UpdateFL(op1Slot.U4, op2Slot.U4, slot.U4, slot.U4, ref fl, mask);
            ctx.Registers[NeonVMConstants.REG_FL].U1 = fl;

            state = ExecutionState.Next;
        }
    }

    internal class ShrQword : IOpCode
    {
        public byte Code => NeonVMConstants.OP_SHR_QWORD;

        public void Load(NeonVMContext ctx, out ExecutionState state)
        {
            var sp = ctx.Registers[NeonVMConstants.REG_SP].U4;
            var op1Slot = ctx.Stack[sp - 1];
            var op2Slot = ctx.Stack[sp];
            sp -= 1;
            ctx.Stack.SetTopPosition(sp);
            ctx.Registers[NeonVMConstants.REG_SP].U4 = sp;

            var fl = ctx.Registers[NeonVMConstants.REG_FL].U1;
            var slot = new NeonVMSlot();
            if((fl & NeonVMConstants.FL_UNSIGNED) != 0)
                slot.U8 = op1Slot.U8 >> (int) op2Slot.U4;
            else
                slot.U8 = (ulong) ((long) op1Slot.U8 >> (int) op2Slot.U4);
            ctx.Stack[sp] = slot;

            var mask = (byte) (NeonVMConstants.FL_ZERO | NeonVMConstants.FL_SIGN | NeonVMConstants.FL_UNSIGNED);
            Utils.UpdateFL(op1Slot.U8, op2Slot.U8, slot.U8, slot.U8, ref fl, mask);
            ctx.Registers[NeonVMConstants.REG_FL].U1 = fl;

            state = ExecutionState.Next;
        }
    }
}
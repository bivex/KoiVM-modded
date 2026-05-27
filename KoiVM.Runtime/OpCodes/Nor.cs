#region

using What_a_great_VM;
using KoiVM.Runtime.Dynamic;
using KoiVM.Runtime.Execution;

#endregion

namespace KoiVM.Runtime.OpCodes
{
    internal class NorDword : IOpCode
    {
        public byte Code => NeonVMConstants.OP_NOR_DWORD;

        public void Load(NeonVMContext ctx, out ExecutionState state)
        {
            var sp = ctx.Registers[NeonVMConstants.REG_SP].U4;
            var op1Slot = ctx.Stack[sp - 1];
            var op2Slot = ctx.Stack[sp];
            sp -= 1;
            ctx.Stack.SetTopPosition(sp);
            ctx.Registers[NeonVMConstants.REG_SP].U4 = sp;

            var slot = new NeonVMSlot();
            slot.U4 = ~(op1Slot.U4 | op2Slot.U4);
            ctx.Stack[sp] = slot;

            var mask = (byte) (NeonVMConstants.FL_ZERO | NeonVMConstants.FL_SIGN);
            var fl = ctx.Registers[NeonVMConstants.REG_FL].U1;
            Utils.UpdateFL(op1Slot.U4, op2Slot.U4, slot.U4, slot.U4, ref fl, mask);
            ctx.Registers[NeonVMConstants.REG_FL].U1 = fl;

            state = ExecutionState.Next;
        }
    }

    internal class NorQword : IOpCode
    {
        public byte Code => NeonVMConstants.OP_NOR_QWORD;

        public void Load(NeonVMContext ctx, out ExecutionState state)
        {
            var sp = ctx.Registers[NeonVMConstants.REG_SP].U4;
            var op1Slot = ctx.Stack[sp - 1];
            var op2Slot = ctx.Stack[sp];
            sp -= 1;
            ctx.Stack.SetTopPosition(sp);
            ctx.Registers[NeonVMConstants.REG_SP].U4 = sp;

            var slot = new NeonVMSlot();
            slot.U8 = ~(op1Slot.U8 | op2Slot.U8);
            ctx.Stack[sp] = slot;

            var mask = (byte) (NeonVMConstants.FL_ZERO | NeonVMConstants.FL_SIGN);
            var fl = ctx.Registers[NeonVMConstants.REG_FL].U1;
            Utils.UpdateFL(op1Slot.U8, op2Slot.U8, slot.U8, slot.U8, ref fl, mask);
            ctx.Registers[NeonVMConstants.REG_FL].U1 = fl;

            state = ExecutionState.Next;
        }
    }
}
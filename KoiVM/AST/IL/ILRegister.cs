#region

using System.Collections.Generic;
using KoiVM.VM;

#endregion

namespace KoiVM.AST.IL
{
    public class ILRegister : IILOperand
    {
        private static readonly Dictionary<NeonVMRegisters, ILRegister> regMap = new Dictionary<NeonVMRegisters, ILRegister>();

        public static readonly ILRegister R0 = new ILRegister(NeonVMRegisters.R0);
        public static readonly ILRegister R1 = new ILRegister(NeonVMRegisters.R1);
        public static readonly ILRegister R2 = new ILRegister(NeonVMRegisters.R2);
        public static readonly ILRegister R3 = new ILRegister(NeonVMRegisters.R3);
        public static readonly ILRegister R4 = new ILRegister(NeonVMRegisters.R4);
        public static readonly ILRegister R5 = new ILRegister(NeonVMRegisters.R5);
        public static readonly ILRegister R6 = new ILRegister(NeonVMRegisters.R6);
        public static readonly ILRegister R7 = new ILRegister(NeonVMRegisters.R7);

        public static readonly ILRegister BP = new ILRegister(NeonVMRegisters.BP);
        public static readonly ILRegister SP = new ILRegister(NeonVMRegisters.SP);
        public static readonly ILRegister IP = new ILRegister(NeonVMRegisters.IP);
        public static readonly ILRegister FL = new ILRegister(NeonVMRegisters.FL);
        public static readonly ILRegister K1 = new ILRegister(NeonVMRegisters.K1);
        public static readonly ILRegister K2 = new ILRegister(NeonVMRegisters.K2);
        public static readonly ILRegister M1 = new ILRegister(NeonVMRegisters.M1);
        public static readonly ILRegister M2 = new ILRegister(NeonVMRegisters.M2);

        private ILRegister(NeonVMRegisters reg)
        {
            Register = reg;
            regMap.Add(reg, this);
        }

        public NeonVMRegisters Register
        {
            get;
            set;
        }

        public override string ToString()
        {
            return Register.ToString();
        }

        public static ILRegister LookupRegister(NeonVMRegisters reg)
        {
            return regMap[reg];
        }
    }
}
#region

using KoiVM.VM;

#endregion

namespace KoiVM.AST.IR
{
    public class IRRegister : IIROperand
    {
        public static readonly IRRegister BP = new IRRegister(NeonVMRegisters.BP, ASTType.I4);
        public static readonly IRRegister SP = new IRRegister(NeonVMRegisters.SP, ASTType.I4);
        public static readonly IRRegister IP = new IRRegister(NeonVMRegisters.IP);
        public static readonly IRRegister FL = new IRRegister(NeonVMRegisters.FL, ASTType.I4);
        public static readonly IRRegister K1 = new IRRegister(NeonVMRegisters.K1, ASTType.I4);
        public static readonly IRRegister K2 = new IRRegister(NeonVMRegisters.K2, ASTType.I4);
        public static readonly IRRegister M1 = new IRRegister(NeonVMRegisters.M1, ASTType.I4);
        public static readonly IRRegister M2 = new IRRegister(NeonVMRegisters.M2, ASTType.I4);

        public IRRegister(NeonVMRegisters reg)
        {
            Register = reg;
            Type = ASTType.Ptr;
        }

        public IRRegister(NeonVMRegisters reg, ASTType type)
        {
            Register = reg;
            Type = type;
        }

        public NeonVMRegisters Register
        {
            get;
            set;
        }

        public IRVariable SourceVariable
        {
            get;
            set;
        }

        public ASTType Type
        {
            get;
            set;
        }

        public override string ToString()
        {
            return Register.ToString();
        }
    }
}
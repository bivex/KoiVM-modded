using System;

namespace KoiVM.TestTarget
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("ARITHMETIC: " + TestSubjects.Arithmetic(7, 3));
            Console.WriteLine("FLOAT_ARITH: " + TestSubjects.FloatArithmetic(3.0, 2.0));
            Console.WriteLine("BITWISE: " + TestSubjects.BitwiseOps(0x12345678));
            Console.WriteLine("STRING_OPS: " + TestSubjects.StringOps("abc"));
            Console.WriteLine("STRING_CONCAT: " + TestSubjects.StringConcat("hello", "world"));
            Console.WriteLine("FACTORIAL: " + TestSubjects.Factorial(6));
            Console.WriteLine("FIBONACCI: " + TestSubjects.Fibonacci(10));
            Console.WriteLine("SWITCH: " + TestSubjects.SwitchTest(2));
            Console.WriteLine("EXCEPTION: " + TestSubjects.ExceptionHandling());
            Console.WriteLine("NESTED_EXCEPTION: " + TestSubjects.NestedException());
            Console.WriteLine("METHOD_CHAIN: " + TestSubjects.MethodCallChain(5));
            Console.WriteLine("VALUETYPE: " + TestSubjects.ValueTypeTest());
            Console.WriteLine("ARRAY_SUM: " + TestSubjects.ArraySum(new int[] { 1, 2, 3, 4, 5 }));
            Console.WriteLine("BOX_UNBOX: " + TestSubjects.BoxUnboxTest());
            Console.WriteLine("BOOL_LOGIC: " + TestSubjects.BooleanLogic(true, true));
            Console.WriteLine("TYPE_CHECK_STR: " + TestSubjects.TypeCheck("hello"));
            Console.WriteLine("TYPE_CHECK_INT: " + TestSubjects.TypeCheck(42));
            Console.WriteLine("COUNTER: " + TestSubjects.IncrementCounter());
            Console.WriteLine("COUNTER2: " + TestSubjects.IncrementCounter());
        }
    }
}

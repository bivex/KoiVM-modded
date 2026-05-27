using System;
using System.Text;

namespace KoiVM.TestTarget
{
    public class TestSubjects
    {
        [System.Reflection.Obfuscation(Exclude = false, Feature = "+virt")]
        public static int Arithmetic(int a, int b)
        {
            int x = a + b;
            x = x * 3;
            x = x - a;
            x = x / (b == 0 ? 1 : b);
            x = x % 5;
            return x;
        }

        [System.Reflection.Obfuscation(Exclude = false, Feature = "+virt")]
        public static double FloatArithmetic(double a, double b)
        {
            return (a + b) * (a - b) / (b + 1.0);
        }

        [System.Reflection.Obfuscation(Exclude = false, Feature = "+virt")]
        public static int BitwiseOps(int v)
        {
            return (v & 0xFF) | ((v >> 8) & 0xFF00);
        }

        [System.Reflection.Obfuscation(Exclude = false, Feature = "+virt")]
        public static string StringOps(string input)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
                sb.Append((char)(input[i] + 1));
            return sb.ToString();
        }

        [System.Reflection.Obfuscation(Exclude = false, Feature = "+virt")]
        public static string StringConcat(string a, string b)
        {
            return a + " " + b;
        }

        [System.Reflection.Obfuscation(Exclude = false, Feature = "+virt")]
        public static int Factorial(int n)
        {
            int result = 1;
            for (int i = 2; i <= n; i++)
                result *= i;
            return result;
        }

        [System.Reflection.Obfuscation(Exclude = false, Feature = "+virt")]
        public static int Fibonacci(int n)
        {
            if (n <= 1) return n;
            int a = 0, b = 1;
            for (int i = 2; i <= n; i++)
            {
                int t = a + b;
                a = b;
                b = t;
            }
            return b;
        }

        [System.Reflection.Obfuscation(Exclude = false, Feature = "+virt")]
        public static string SwitchTest(int val)
        {
            switch (val)
            {
                case 0: return "zero";
                case 1: return "one";
                case 2: return "two";
                case 3: return "three";
                default: return "other:" + val;
            }
        }

        [System.Reflection.Obfuscation(Exclude = false, Feature = "+virt")]
        public static string ExceptionHandling()
        {
            try
            {
                int[] arr = new int[5];
                arr[10] = 42;
                return "no exception";
            }
            catch (IndexOutOfRangeException)
            {
                return "caught IndexOutOfRange";
            }
        }

        [System.Reflection.Obfuscation(Exclude = false, Feature = "+virt")]
        public static string NestedException()
        {
            try
            {
                try { throw new InvalidOperationException("inner"); }
                catch (InvalidOperationException ex) { return ex.Message; }
            }
            catch (Exception)
            {
                return "outer";
            }
        }

        [System.Reflection.Obfuscation(Exclude = false, Feature = "+virt")]
        public static int MethodCallChain(int x)
        {
            return AddOne(MultiplyByTwo(x));
        }

        static int MultiplyByTwo(int v) { return v * 2; }
        static int AddOne(int v) { return v + 1; }

        [System.Reflection.Obfuscation(Exclude = false, Feature = "+virt")]
        public static int ValueTypeTest()
        {
            Point p;
            p.X = 10;
            p.Y = 20;
            return p.X + p.Y;
        }

        struct Point { public int X, Y; }

        [System.Reflection.Obfuscation(Exclude = false, Feature = "+virt")]
        public static int ArraySum(int[] arr)
        {
            int sum = 0;
            for (int i = 0; i < arr.Length; i++)
                sum += arr[i];
            return sum;
        }

        [System.Reflection.Obfuscation(Exclude = false, Feature = "+virt")]
        public static int BoxUnboxTest()
        {
            object boxed = 42;
            return (int)boxed + 8;
        }

        [System.Reflection.Obfuscation(Exclude = false, Feature = "+virt")]
        public static bool BooleanLogic(bool a, bool b)
        {
            return (a && b) || (!a && !b);
        }

        [System.Reflection.Obfuscation(Exclude = false, Feature = "+virt")]
        public static string TypeCheck(object obj)
        {
            if (obj is string) return "string";
            if (obj is int) return "int";
            return "unknown";
        }

        static int counter = 0;

        [System.Reflection.Obfuscation(Exclude = false, Feature = "+virt")]
        public static int IncrementCounter()
        {
            counter++;
            return counter;
        }
    }
}

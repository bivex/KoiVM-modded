using System;
using System.Runtime.InteropServices;
using System.Reflection;

public class NativeBridge {
    // P/Invoke импорты - их виртуализировать нельзя, они указывают на внешнюю либу
    [DllImport("libnative.dylib", CallingConvention = CallingConvention.Cdecl)]
    private static extern int ComputeSecretFormula(int a, int b);

    [DllImport("libnative.dylib", CallingConvention = CallingConvention.Cdecl)]
    private static extern void PrintMessageFromNative();

    // А вот методы-обертки мы накрываем KoiVM (Feature = "+virt")
    [Obfuscation(Exclude = false, Feature = "+virt")]
    public static int Compute(int a, int b) {
        Console.WriteLine("[Managed] Calling ComputeSecretFormula...");
        int temp = ComputeSecretFormula(a, b);
        // Добавим немного логики на стороне C#, которая тоже уйдет в ВМ
        return temp * 2;
    }

    [Obfuscation(Exclude = false, Feature = "+virt")]
    public static void Print() {
        Console.WriteLine("[Managed] Calling PrintMessageFromNative...");
        PrintMessageFromNative();
    }
}

class Program {
    static void Main(string[] args) {
        Console.WriteLine("=== P/Invoke Test ===");
        
        try {
            NativeBridge.Print();
            int result = NativeBridge.Compute(10, 20);
            Console.WriteLine("[Managed] Final Result (after VM processing): " + result);
            
            if (result == 206) { // (10*5 + 20*3 - 7) * 2 = (50 + 60 - 7) * 2 = 103 * 2 = 206
                Console.WriteLine("[Managed] Test SUCCESS");
            } else {
                Console.WriteLine("[Managed] Test FAILED");
            }
        } catch (Exception ex) {
            Console.WriteLine("ERROR: " + ex.Message);
        }
    }
}
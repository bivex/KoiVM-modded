#using <mscorlib.dll>
#using <System.dll>

using namespace System;

// Маркировка метода для виртуализации NeonVM
[System::Reflection::Obfuscation(Exclude = false, Feature = "+virt")]
public ref class TestLogic {
public:
    static int Compute(int a, int b) {
        return (a + b) * 3 - a;
    }
};

int main(array<System::String ^> ^args) {
    try {
        int res = TestLogic::Compute(2, 3);
        Console::WriteLine("CPPCLI_TEST: " + res);
        return 0;
    } catch (Exception^ ex) {
        Console::WriteLine("ERROR: " + ex->Message);
        return 1;
    }
}

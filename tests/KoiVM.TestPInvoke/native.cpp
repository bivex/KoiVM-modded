#include <iostream>

extern "C" {
    int ComputeSecretFormula(int a, int b) {
        return (a * 5) + (b * 3) - 7;
    }

    void PrintMessageFromNative() {
        std::cout << "[Native] Hello from C++ dylib!" << std::endl;
    }
}
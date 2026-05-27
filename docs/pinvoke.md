# Specification: P/Invoke Integration with DarksVM

## 1. Scope
This document specifies the methodology and architecture for integrating unmanaged code (C++ native libraries) with .NET assemblies protected by DarksVM (KoiVM custom). Since C++/CLI (Managed C++) is strictly limited to Windows environments, this specification focuses on the cross-platform P/Invoke (Platform Invocation Services) approach for macOS and Linux systems.

## 2. Normative References
The following components are required for the implementation of the Native Bridge architecture:
* C++ Compiler (e.g., `clang++` or `g++`) capable of producing `.dylib` (macOS) or `.so` (Linux) shared libraries.
* .NET Framework / Mono framework supporting `System.Runtime.InteropServices`.
* DarksVM Virtualization Engine (KoiVM ConfuserEx Plugin).

## 3. System Architecture
DarksVM operates exclusively on Common Intermediate Language (CIL) instructions. Consequently, native code and direct P/Invoke method definitions (`[DllImport]`) cannot be virtualized because they lack managed method bodies. 

To secure the interoperability layer, a "Managed Wrapper" architecture is implemented:
1. **Unmanaged Layer**: The C++ library exposes operations via C-linkage (`extern "C"`).
2. **P/Invoke Layer**: The .NET runtime maps these functions using `DllImport`. These declarations remain unvirtualized.
3. **Wrapper Layer**: High-level C# methods encapsulate the P/Invoke calls. These wrapper methods are marked for virtualization (`Feature = "+virt"`).
4. **Execution**: At runtime, DarksVM executes the wrapper logic, dynamically resolving and invoking the unmanaged functions while obscuring the surrounding managed logic (e.g., argument preparation, return value manipulation, and control flow).

## 4. Operational Procedures

### 4.1. Unmanaged Component Implementation
Construct a flat C-API to serve as the native bridge. Exported functions must use the `cdecl` calling convention.

```cpp
// native.cpp
#include <iostream>

extern "C" {
    int ComputeSecretFormula(int a, int b) {
        return (a * 5) + (b * 3) - 7;
    }
}
```

### 4.2. Managed Component Implementation
Define the `DllImport` signatures and construct the virtualized wrapper methods.

```csharp
// NativeBridge.cs
using System;
using System.Runtime.InteropServices;
using System.Reflection;

public class NativeBridge {
    // Unvirtualized P/Invoke declaration
    [DllImport("libnative.dylib", CallingConvention = CallingConvention.Cdecl)]
    private static extern int ComputeSecretFormula(int a, int b);

    // Virtualized Managed Wrapper
    [Obfuscation(Exclude = false, Feature = "+virt")]
    public static int Compute(int a, int b) {
        // Logic inside this method is converted to DarksVM bytecode
        int result = ComputeSecretFormula(a, b);
        return result * 2;
    }
}
```

### 4.3. Compilation and Protection Workflow
Execute the following sequence to build and protect the integrated application.

1. Compile the native shared library:
   ```bash
   clang++ -shared -fPIC -o libnative.dylib native.cpp
   ```
2. Compile the managed assembly:
   ```bash
   mcs -out:Application.exe Application.cs
   ```
3. Apply DarksVM protection via ConfuserEx CLI:
   ```bash
   mono Confuser.CLI.exe protection_config.crproj
   ```

## 5. Diagnostic Tools / Troubleshooting
To verify the integrity of the P/Invoke bridge post-virtualization:
1. Ensure the generated `.dylib` or `.so` file is located in the same directory as the protected `.exe` or within a directory specified by the `DYLD_LIBRARY_PATH` / `LD_LIBRARY_PATH` environment variables.
2. Execute the protected binary:
   ```bash
   export DYLD_LIBRARY_PATH="$PWD"
   mono Application.exe
   ```
3. A successful invocation will output the expected result without raising a `DllNotFoundException` or `EntryPointNotFoundException`.

## Annex A (Informative): Security Considerations
While DarksVM effectively hides the execution flow and data manipulation within the wrapper layer, the names of the imported native functions and their signatures remain visible in the assembly's metadata. To maximize security:
* Migrate sensitive validation or cryptographic logic entirely into the native library or entirely into the virtualized managed code.
* Employ obfuscated or generic names for exported C++ functions (e.g., use `ComputeHash` instead of `ValidateLicenseKey`).
* Pass obfuscated or encrypted byte arrays between the managed and unmanaged boundaries, decrypting them only within the virtualized context or the native heap.

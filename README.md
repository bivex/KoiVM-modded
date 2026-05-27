# DarksVM (KoiVM Custom) Specification

## 1. Scope
This document specifies DarksVM, a modified version of the KoiVM ConfuserEx plugin. The system is designed to virtualize .NET methods, rendering them understandable exclusively by the custom virtual machine. Modifications from the original KoiVM include altered VMEntry names, renaming of the 'Run' method to 'Load', integration of additional calculations, prevention of OldRod devirtualization, and enhanced compatibility.

## 2. Normative References
The following prerequisites are required for operation on macOS:
* Mono framework
* NuGet package manager

## 3. Installation / Setup
The following commands execute the build process for the required components.

### 3.1. Building ConfuserEx-Plus
```bash
cd ConfuserEx-Plus
nuget install ./ConfuserEx/packages.config -OutputDirectory ./packages

# Build all non-GUI projects
for proj in dnlib Confuser.Core Confuser.DynCipher Confuser.Renamer Confuser.Protections Confuser.Runtime Confuser.CLI; do
  xbuild $proj/$proj.csproj /p:Configuration=Debug /p:Platform=AnyCPU
done
```
Output directory: `ConfuserEx-Plus/Debug/bin/`

### 3.2. Building KoiVM
```bash
cd ..
xbuild KoiVM.Runtime/KoiVM.Runtime.csproj /p:Configuration=Debug /p:Platform=AnyCPU
xbuild KoiVM/KoiVM.csproj /p:Configuration=Debug /p:Platform=AnyCPU
```
Output directory: `bin/`

### 3.3. Building KoiVM.Confuser (Plugin)
```bash
cp bin/KoiVM.dll bin/KoiVM.Runtime.dll ConfuserEx-Plus/Debug/bin/
xbuild KoiVM.Confuser/KoiVM.Confuser.csproj /p:Configuration=Debug /p:Platform=AnyCPU
```
Output directory: `ConfuserEx-Plus/Debug/bin/`

## 4. System Architecture
The KoiVM pipeline replaces method bodies with a custom virtual machine. The transformation executes as follows:
1. **CIL to CFG**: The original CIL is converted into a Control Flow Graph.
2. **CFG to IL-AST**: The graph is transformed into an Intermediate Representation.
3. **IL-AST to VM IR**: The representation is translated into VM-specific opcodes.
4. **VM IR to Bytecode**: The opcodes are compiled into custom bytecode and stored in a `#DarksVM` metadata stream.
5. **Runtime Injection**: The VM interpreter (comprising 141 classes and 493+ methods) is merged into the target assembly.

The virtualization obscures the original logic, rendering decompilers (e.g., dnSpy, ILSpy) capable of displaying only the `DarksVM.Load(...)` call. VM opcodes and encryption keys are uniquely generated per build, requiring a custom devirtualizer for each protected assembly. Standard .NET features (arithmetic, floats, exceptions, generics, arrays) are fully supported.

## 5. Operational Procedures
To operationalize the virtualization, the compiled projects must be added to ConfuserEx. 

### 5.1. Project Configuration
Update the `.crproj` project file:
```xml
<rule pattern="true" inherit="false">
  <protection id="virt" />
</rule>
<plugin>?:\path\to\your\project\KoiVM.Confuser.exe</plugin>
```

### 5.2. CLI Execution
```bash
mono Confuser.CLI.exe -plugin KoiVM.Confuser.dll -probe . -o output input.exe
```

### 5.3. Method Marking
Methods designated for virtualization must be annotated in the source code:
```csharp
[System.Reflection.Obfuscation(Exclude = false, Feature = "+virt")]
```

## 6. Diagnostic Tools / Troubleshooting
To verify the integrity of the virtualization, execute the test suite:
```bash
bash tests/run-tests.sh
```
The test suite compiles a target, captures baseline execution output, applies KoiVM protection, executes the protected binary, and compares outputs. Validation requires 19 test methods to produce identical results. Coverage includes arithmetic, floating-point mathematics, bitwise operations, strings, loops, switch statements, exception handling, method call chains, value types, arrays, boxing/unboxing, boolean logic, and static fields.

## Annex A (Informative): Credits and Community
* Developer: d4rk
* Community: https://discord.gg/bkkybeM
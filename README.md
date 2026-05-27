**DarksVM - KoiVM custom**
========
**DarksVM** is a modified version of **KoiVM** which is a ConfuserEx plugin that allows you to virtualize methods to be understandable only by our computer.

This version includes:
* Modified VMEntry name and entries
* Renamed 'Run' method to 'Load'
* Added some calculation
* OldRod is no longer able to devirtualize
* Improved compatibility

![](_68747470733a2f2f63646e2e6434726b2e66722f4d486d2e6a7067.png)

**Prerequisites (macOS)**
--------
```
brew install mono nuget
```

**Building ConfuserEx-Plus**
--------
```
cd ConfuserEx-Plus
nuget install ./ConfuserEx/packages.config -OutputDirectory ./packages

# Build all non-GUI projects (WPF GUI is not available on macOS)
for proj in dnlib Confuser.Core Confuser.DynCipher Confuser.Renamer Confuser.Protections Confuser.Runtime Confuser.CLI; do
  xbuild $proj/$proj.csproj /p:Configuration=Debug /p:Platform=AnyCPU
done
```
Output: `ConfuserEx-Plus/Debug/bin/`

**Building KoiVM**
--------
```
cd ..

# KoiVM.Runtime and KoiVM (output to bin/)
xbuild KoiVM.Runtime/KoiVM.Runtime.csproj /p:Configuration=Debug /p:Platform=AnyCPU
xbuild KoiVM/KoiVM.csproj /p:Configuration=Debug /p:Platform=AnyCPU
```
Output: `bin/`

**Building KoiVM.Confuser (plugin)**
--------
```
# Copy dependencies
cp bin/KoiVM.dll bin/KoiVM.Runtime.dll ConfuserEx-Plus/Debug/bin/

# Build plugin (output goes to ConfuserEx-Plus/Debug/bin/)
xbuild KoiVM.Confuser/KoiVM.Confuser.csproj /p:Configuration=Debug /p:Platform=AnyCPU
```

**How to use**
--------
Add these projects to your ConfuserEx, then add this in your .crproj project file
```
<rule pattern="true" inherit="false">
  <protection id="virt" />
</rule>
<plugin>?:\path\to\your\project\KoiVM.Confuser.exe</plugin>
```

Or via CLI:
```
mono Confuser.CLI.exe -plugin KoiVM.Confuser.dll -probe . -o output input.exe
```

Mark methods to virtualize with:
```csharp
[System.Reflection.Obfuscation(Exclude = false, Feature = "+virt")]
```

**How it works**
--------
KoiVM replaces method bodies with a custom virtual machine. Here's what happens:

**Before** — normal .NET IL, readable by any decompiler (dnSpy, ILSpy):
```cil
// int Arithmetic(int a, int b)
IL_0001: ldarg.0       // load a
IL_0002: ldarg.1       // load b
IL_0003: add            // a + b
IL_0006: ldc.i4.3
IL_0007: mul            // * 3
IL_000a: ldarg.0
IL_000b: sub            // - a
...
```

**After** — method body is replaced with a stub that calls the VM:
```cil
// int Arithmetic(int a, int b)
IL_0000: ldc.i4 5714370        // encryption keys
IL_0005: ldc.i4 2857185        // (random per build)
IL_000a: ldtoken TestSubjects  // type reference only
IL_000f: ldc.i4 11428740
IL_0014: ldc.i4 8571555
IL_001a: newarr Object         // pack arguments
IL_0031: call DarksVM::Load()  // execute in VM
IL_0036: unbox.any Int32
```

The pipeline:
1. **CIL → CFG** — original IL is converted to a control flow graph
2. **CFG → IL-AST** — transformed into an intermediate representation
3. **IL-AST → VM IR** — translated to VM-specific opcodes
4. **VM IR → bytecode** — compiled into custom bytecode stored in a `#DarksVM` metadata stream
5. **Runtime injection** — the VM interpreter (141 classes, 493+ methods) is merged into the target assembly

What makes it hard to reverse:
- Decompileers show only `DarksVM.Load(...)` — no original logic visible
- VM opcodes and encryption keys are unique per build
- A custom devirtualizer must be written for each protected assembly
- All .NET features still work correctly: arithmetic, floats, exceptions, generics, arrays, etc.

**Running tests**
--------
```
bash tests/run-tests.sh
```
This builds a test target, captures baseline output, protects it with KoiVM, runs the protected version, and compares outputs. All 19 test methods must produce identical results.

Test coverage: arithmetic, float math, bitwise ops, string operations, loops (factorial, fibonacci), switch, exception handling, method call chains, value types, arrays, boxing/unboxing, boolean logic, type checks, static fields.

Credit to d4rk, developer and creator.
Join the Discord community and get access to DarkProtector and others, for free!
https://discord.gg/bkkybeM

![](d4rk_avatar.gif?s=87)

# NeonVM Internal Protections

## 1. Scope
This document describes the protection mechanisms built into the NeonVM virtualization engine itself — the defenses that are intrinsic to the VM runtime and bytecode compilation, separate from the ConfuserEx protections pipeline.

---

## 2. VM Architecture

### 2.1. Registers
16 virtual registers, IDs randomized per build:

| Register | Purpose |
|---|---|
| R0–R7 | General-purpose |
| BP | Base pointer (frame base) |
| SP | Stack pointer |
| IP | Instruction pointer |
| FL | Flags register |
| K1, K2 | Key registers (used by SMC) |
| M1, M2 | Temporary / mutation registers |

### 2.2. Flags
8 flag bits in the FL register:

| Flag | Purpose |
|---|---|
| OVERFLOW | Arithmetic overflow |
| CARRY | Carry/borrow |
| ZERO | Zero result |
| SIGN | Negative result |
| UNSIGNED | Unsigned comparison |
| BEHAV1–3 | Internal behavior flags |

### 2.3. Instruction Set (~70 opcodes)

| Category | Opcodes |
|---|---|
| Memory load | LIND_PTR, LIND_OBJECT, LIND_BYTE/WORD/DWORD/QWORD |
| Memory store | SIND_PTR, SIND_OBJECT, SIND_BYTE/WORD/DWORD/QWORD |
| Stack | POP, PUSHR_OBJECT/BYTE/WORD/DWORD/QWORD, PUSHI_DWORD/QWORD |
| Arithmetic | ADD, SUB, MUL, DIV, REM (DWORD/QWORD/R32/R64 variants) |
| Bitwise | NOR_DWORD, NOR_QWORD (NAND-based: all logic derivable) |
| Shift | SHR_DWORD/QWORD, SHL_DWORD/QWORD |
| Compare | CMP, CMP_DWORD/QWORD, CMP_R32/R64 |
| Branch | JZ, JNZ, JMP, SWT (switch/table) |
| Conversion | SX_BYTE/WORD/DWORD, FCONV_R32_R64, FCONV_R64_R32, FCONV_R32, FCONV_R64, ICONV_PTR, ICONV_R64 |
| Call/Return | CALL, RET |
| Exception | TRY, LEAVE |
| VM calls | VCALL (17 virtual calls — see below) |

### 2.4. VM Calls (VCALL)
17 high-level operations that the VM delegates to the runtime:

| VCALL | Description |
|---|---|
| EXIT | Terminate VM execution |
| BREAK | Debugger break |
| ECALL | External method call (CALL/CALLVIRT/NEWOBJ/CONSTRAINED) |
| CAST | Type cast |
| CKFINITE | Check finite float |
| CKOVERFLOW | Check overflow |
| RANGECHK | Range check |
| INITOBJ | Initialize object |
| LDFLD / STFLD | Load/store field |
| LDFTN | Load function pointer |
| TOKEN | Resolve metadata token |
| THROW | Throw exception |
| SIZEOF | Get type size |
| BOX / UNBOX | Boxing/unboxing |
| LOCALLOC | Allocate local memory |

---

## 3. Compilation Pipeline

```
CIL Method Body
    │
    ▼
┌─────────────────┐
│  CFG Generation  │   BlockParser.Parse()
└────────┬────────┘
         ▼
┌─────────────────┐
│  IL-AST Builder  │   ILASTBuilder.BuildAST()
└────────┬────────┘
         ▼
┌─────────────────┐
│  ILAST Transform │   ILASTTransformer (optimizations, simplifications)
└────────┬────────┘
         ▼
┌─────────────────┐
│  IR Translation  │   IRTranslator (CIL → VM IR)
└────────┬────────┘
         ▼
┌─────────────────┐
│  IR Transform    │   IRTransformer (includes SMC injection)
└────────┬────────┘
         ▼
┌─────────────────┐
│  IL Translation  │   ILTranslator (VM IR → VM bytecode)
└────────┬────────┘
         ▼
┌─────────────────┐
│  IL Transform    │   ILTransformer, ILPostTransformer
└────────┬────────┘
         ▼
┌─────────────────┐
│  Serialization   │   BasicBlockSerializer → KoiHeap → #NeonVM stream
└─────────────────┘
```

---

## 4. Built-in Protections

### 4.1. Opcode Randomization
**Source:** `VM/Descriptors/OpCodeDescriptor.cs`, `FlagDescriptor.cs`, `RegisterDescriptor.cs`

All opcode IDs, flag positions, and register indices are randomly shuffled using a seed-based `Random` instance. The same CIL instruction maps to a different opcode byte in every protected assembly.

**Effect:** A devirtualizer built for one build cannot interpret bytecode from another build without reconstructing the full opcode mapping.

### 4.2. Chunk Shuffle
**Source:** `RT/NeonVMRuntime.cs:142`

```csharp
Descriptor.Random.Shuffle(finalChunks);
```

After all basic blocks are serialized into chunks, the entire list is randomly permuted. Only the header chunk stays at offset 0. Cross-references between blocks use `ILRelReference` (relative offsets) that are resolved after shuffling.

**Effect:** Static pattern matching across the bytecode stream is impossible — identical methods produce different byte layouts each build.

### 4.3. Self-Modifying Code (SMC)
**Source:** `Protections/SMC/SMCBlock.cs`, `SMCIRTransform.cs`

Each basic block is XOR-encrypted with a random byte key. Before execution, the VM decrypts the block in-place using a counter loop:

1. A trampoline reads the key byte from the start of the encrypted block
2. Iterates `counter` bytes, XOR-ing each with the key
3. Jumps to the now-decrypted block

The trampoline itself uses obfuscated address computation (two XOR layers with constants `0x0f000001`, `0x0f000002`, `0x0f000003`).

**Effect:** The bytecode is never fully decrypted in memory — only the current block is decrypted before execution and can be re-encrypted. Dumping the full bytecode at any single point in time yields only partial plaintext.

### 4.4. Runtime Renamer
**Source:** `RT/Mutation/Renamer.cs`

After compilation, all non-public types, methods, and fields in the VM runtime are renamed using a deterministic PRNG:

```
next = next * 0x15660D + 0x3D6EF85F
name = next.ToString("x")
```

Additionally:
- Generic parameter names are cleared
- Method parameter names are cleared
- Properties, events, and custom attributes are removed entirely

**Effect:** The runtime module has no recognizable type/method names — all identifiers are opaque hex strings.

### 4.5. Type Name Camouflage
**Source:** `RT/Mutation/RTMap.cs`

VM types are named to resemble .NET framework types:

| Actual Role | Disguised Name |
|---|---|
| VM entry point | `Microsoft.VisualBasic.Devices.NeonVM` |
| VM dispatcher (Run) | `Load` |
| Object pool / executor | `System.Runtime.Serialization.Formatters.Execution.ObjectPool` |
| Constants holder | `System.Runtime.Serialization.Formatters.Dynamic.NeonVMConstants` |

**Effect:** Casual browsing of the assembly type list does not reveal the presence of a VM runtime.

### 4.6. Dispatcher Patching
**Source:** `RT/Mutation/RuntimePatcher.cs`

Before the runtime is emitted:
1. All exception handler `catch` clauses are replaced with `catch System.Object` (hides actual exception types)
2. The `Throw` method is removed from the runtime
3. The `GetValue` method (which exposes internal state) is removed or replaced with `ldnull`
4. In debug mode, `GetValue` is redirected to `ResetPool` (stackwalk) instead

**Effect:** Even if the runtime is decompiled, the exception handling structure reveals nothing about the original code's exception types. Internal state inspection methods are stripped.

### 4.7. Self-Obfuscation of the VM Runtime
**Source:** `Obfuscation.cs`

The KoiVM assembly itself is configured to be processed by ConfuserEx protections:

```csharp
[assembly: Obfuscation(Feature =
    "preset(aggressive);" +
    "+constants(mode=dynamic,decoderCount=10,cfg=true);" +
    "+ctrl flow(predicate=expression,intensity=100);" +
    "+rename(renPublic=true,mode=sequential);" +
    "+ref proxy(mode=strong,encoding=expression,typeErasure=true);" +
    "+resources(mode=dynamic);"
)]
```

The VM runtime is protected with:
- Dynamic constant encryption (10 decoders, CFG-based)
- Control flow obfuscation (expression predicates, 100% intensity)
- Full renaming (including public members)
- Strong reference proxy with type erasure
- Dynamic resource encryption

**Effect:** The runtime library itself is heavily obfuscated, making analysis of the VM interpreter extremely difficult even after extracting it from the protected assembly.

### 4.8. Metadata Heap Injection
**Source:** `RT/KoiHeap.cs`, `RT/Mutation/RuntimeMutator.cs:62`

The compiled bytecode is injected into the target module as a custom metadata heap named `#NeonVM`:

```csharp
writer.TheOptions.MetaDataOptions.OtherHeaps.Add(request.Heap);
```

This heap appears as an unknown type to standard .NET metadata readers (producing the "Unknown heap type: #NeonVM" warning).

**Effect:** Standard metadata parsers skip or warn about the heap. The bytecode is stored outside normal metadata structures, making it invisible to tools that only parse standard streams (#~, #Strings, #Blob, #GUID, #US).

### 4.9. Jump Table Obfuscation
**Source:** `RT/JumpTableChunk.cs`

Switch statements (SWT opcode) use an indirect jump table. The table entries are computed relative to block offsets after chunk shuffling, adding another layer of indirection.

### 4.10. Constants Injection
**Source:** `RT/Mutation/RTConstants.cs`

All VM constants (opcode IDs, register IDs, flag masks, VM call IDs) are injected into the runtime's `.cctor` as field initializations in randomized order. The constant values are only known at build time and differ per compilation.

**Effect:** There are no hardcoded opcode/register/flag values in the runtime — everything is parameterized and unique per build.

---

## 5. Protection Interaction with ConfuserEx Pipeline

When NeonVM is used alongside ConfuserEx protections, the full pipeline executes in this order:

1. **Pre-VM phases** (anti-debug, anti-dump, renaming, etc.) modify the target assembly
2. **Mark phase** identifies methods tagged with `[Obfuscation(Feature = "+koi")]`
3. **Virtualization** replaces tagged method bodies with VM stubs
4. **Finalize phase** serializes bytecode, applies SMC, injects #NeonVM heap
5. **Save phase** writes the runtime library into the output

The VM's internal protections (randomization, SMC, renaming) are applied during step 3-4, making them independent of and complementary to the ConfuserEx protection phases.

---

## 6. Anti-Devirtualization Summary

| Attack Vector | Defense |
|---|---|
| Static opcode mapping | Randomized per build (seed-based) |
| Bytecode pattern matching | Chunk shuffle + SMC encryption |
| Runtime decompilation | Runtime self-obfuscated (control flow, constants, ref proxy) |
| Metadata extraction | Custom #NeonVM heap, camouflaged type names |
| Symbolic execution | Opaque predicates in SMC trampolines |
| Known-tool signatures (OldRod, etc.) | NeonVM branding, renamed entry points, restructured dispatcher |
| Memory dump | Blocks encrypted at rest, only current block decrypted |

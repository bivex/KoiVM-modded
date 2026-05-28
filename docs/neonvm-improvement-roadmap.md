# NeonVM Improvement Roadmap

## 1. Current Weaknesses (as of 2025-2026 threat landscape)

Based on recent academic and industry research on VM-based obfuscation and devirtualization.

---

### 1.1. Exception Handling Metadata Exposure

**Problem:** KoiVM's `RuntimePatcher` replaces catch types with `System.Object`, but the CLR still requires valid EH table entries. The structure of protected/unprotected/fault/finally regions, their nesting, and stack layout are visible in metadata. This leaks:
- Control flow boundaries
- Object lifetime information
- Try/catch nesting depth and structure

**Research:** XuanJia (January 2026, Nankai University) demonstrates that EH metadata is the single richest information source for reconstructing virtualized code structure, even when method bodies are fully virtualized.

**Current code:** `RT/Mutation/RuntimePatcher.cs` — only patches catch types and removes throw/getvalue methods. Does not touch EH table structure.

**Fix:** EH shadowing — replace native EH metadata with ABI-compatible shadow unwind codes. Encrypt original state machine and cleanup routines (AES) and embed alongside bytecode. VM dispatcher handles EH internally without exposing structure to the CLR.

---

### 1.2. Deterministic Execution Paths

**Problem:** For identical inputs, KoiVM always produces the same execution trace. The chunk order is randomized at build time, but the dispatch path at runtime is static — the same opcode sequence always traverses the same handler chain.

**Research:** DSVMP (Dynamic Scheduling VM Protection, 2025) introduces non-deterministic dispatch where the same input can traverse different handler paths. This breaks attacks based on trace comparison across builds or repeated execution.

**Current code:** `RT/NeonVMRuntime.cs` — `Descriptor.Random.Shuffle(finalChunks)` randomizes layout once at build time. Runtime dispatch in the interpreter is purely deterministic (IP increments, branches are static).

**Fix:** Introduce dynamic dispatch scheduling — handlers selected from a pool at runtime, execution order varies per invocation using runtime entropy (e.g., RDTSC seeding).

---

### 1.3. Predictable Seed-Based Randomization

**Problem:** KoiVM uses a single seed to drive all randomization (opcodes, registers, flags, chunk order, renamer). Once an attacker recovers the seed or establishes a mapping for one build, the entire protection is reduced to a lookup table.

**Research:** 2025 systematic study on interpreter diversification categorizes VM defenses along three axes — interpretation method, bytecode organization, handler permutation/relocation. Seed-based shuffle is identified as the weakest form of diversification because it produces a single fixed permutation.

**Current code:** `Virtualizer.cs` — `new Virtualizer(int seed, bool debug)`. Single seed feeds `VMDescriptor` → all descriptors.

**Fix:** Per-chunk independent entropy. Each basic block gets its own randomization key for encoding. Handler relocation — handlers themselves are reordered and re-encrypted at runtime, not just at build time.

---

### 1.4. Weak SMC Encryption

**Problem:** SMC uses single-byte XOR. XOR with a known-plaintext byte (e.g., a common opcode pattern) immediately reveals the key, decrypting the entire block.

**Current code:** `Protections/SMC/SMCBlock.cs:63`:
```csharp
newData[i + 1] = (byte)(data[i] ^ key);
newData[0] = key;
```

Key is stored as the first byte of the encrypted block, and the encryption is trivially reversible.

**Fix:** Replace XOR with AES-128/256 in CTR mode. Each block gets a unique IV derived from its offset and the per-chunk entropy key. Block key is not stored in plaintext — derived at runtime from the VM's key registers (K1, K2).

---

### 1.5. No Defense Against Symbolic Execution

**Problem:** Modern devirtualizers use LLVM-based symbolic execution (concolic execution). They trace the VM interpreter's execution path and symbolically solve for the original program's semantics. The SMC trampoline constants (`0x0f000001`, `0x0f000002`, `0x0f000003`) are trivially solvable.

**Current code:** `Protections/SMC/SMCIRTransform.cs` — constants are hardcoded, no opaque predicates.

**Fix:** Inject opaque predicates inside VM handlers. These are conditions that always evaluate to true/false but require solving NP-hard or computationally expensive problems to prove. Examples:
- Algebraic opaque predicates over VM registers
- Pointer-based opacity (aliasing analysis required)
- Environmental checks as predicates (hash of code pages)

---

## 2. Priority Matrix

| Priority | Improvement | Effort | Impact |
|---|---|---|---|
| **P0** | AES chunk encryption (replace XOR SMC) | Medium | Breaks known-plaintext attacks |
| **P0** | Remove plaintext key from block header | Low | Forces symbolic analysis |
| **P1** | EH shadowing | High | Closes richest information leak |
| **P1** | Opaque predicates in handlers | Medium | Slows symbolic execution exponentially |
| **P2** | Dynamic dispatch scheduling | High | Working | Breaks trace-based attacks |
| **P2** | Per-chunk independent entropy | Medium | Working | Prevents single-point-of-failure seed |
| **P3** | Handler relocation at runtime | High | Moves target during analysis |
| **P3** | WASM / NativeAOT layer | Very High | Future direction, not .NET-native |

---

## 3. References

- **XuanJia** (2026) — "Virtualizing Exception Handling Semantics for .NET Obfuscation", Nankai University
- **DSVMP** (2025) — Dynamic Scheduling VM Protection, non-deterministic dispatch paths
- **Interpreter Diversification Survey** (2025) — Systematic categorization of VM diversification techniques
- **LLVM-based Devirtualization** (2024-2025) — Trace separation attacks on VMProtect 3.x and similar protectors
- **Commercial trends** (2025-2026) — Three-layer protection: dynamic code regeneration + WASM + VM obfuscation

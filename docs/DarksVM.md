# Specification: DarksVM Protection Methodologies

## 1. Scope
This document specifies the protection mechanisms and obfuscation methodologies implemented by DarksVM (a custom KoiVM variant) acting as a ConfuserEx plugin. The system is designed to secure .NET assemblies against reverse engineering, decompilation, and unauthorized modification through a combination of IL virtualization, environmental checks, and metadata obfuscation.

## 2. Normative References
* ECMA-335: Common Language Infrastructure (CLI) Standard
* DarksVM Virtual Machine Architecture
* ConfuserEx Core Obfuscation Pipeline

## 3. Core Virtualization Engine (DarksVM)
The primary protection mechanism is the total elimination of managed Common Intermediate Language (CIL) method bodies, replacing them with custom bytecode interpreted by an embedded virtual machine.

### 3.1. Compilation Pipeline
1. **CFG Generation**: Original CIL instructions are translated into a Control Flow Graph (CFG).
2. **IL-AST Translation**: The CFG is converted into an Abstract Syntax Tree (IL-AST), decoupling the logic from standard .NET stacks.
3. **VM IR Generation**: The AST is translated into a proprietary Intermediate Representation (VM IR).
4. **Bytecode Emission**: The IR is compiled into DarksVM bytecode. This bytecode is stored within an injected `#DarksVM` metadata stream, distinct from standard `#~` or `#Strings` streams.

### 3.2. Runtime Execution
* **Opcode Multiplexing**: VM opcodes are uniquely mapped and randomized per build. A specific instruction (e.g., `Add`) will possess a different opcode byte in every protected assembly.
* **Interpreter Injection**: A runtime interpreter (comprising approximately 141 classes) is injected into the target assembly.
* **Context Isolation**: Method execution occurs within isolated VM registers (SP, BP, IP) and a custom stack array, preventing decompilers (e.g., dnSpy, ILSpy) from analyzing the original execution flow.

## 4. Runtime Protection Measures
DarksVM integrates deeply with the ConfuserEx pipeline to inject active environmental defenses.

### 4.1. Anti-Debug and Anti-Profiling
* **Anti-Debug Injection**: Injects runtime checks (e.g., `IsDebuggerPresent`, `CheckRemoteDebuggerPresent`) to terminate the process if a debugger is attached.
* **Anti-dnSpy Injection**: Explicitly detects and neutralizes the dnSpy debugger and profiler environments.
* **Anti-VM Injection**: Detects execution within sandboxed or virtualized environments (e.g., VMware, VirtualBox) typically used by malware analysts.

### 4.2. Memory and Integrity Safeguards
* **Anti-Dump Injection**: Erases PE headers from memory at runtime (`Erasing Headers`, `Overwriting Headers` phases) to prevent tools from dumping a valid PE file from RAM.
* **Anti-Tamper (MD5 Hash Check)**: Validates the integrity of the assembly at runtime. Any modification to the binary will trigger a hash mismatch and terminate execution.

## 5. Metadata and Control Flow Obfuscation
Beyond the virtualization of method bodies, the assembly metadata is heavily mangled to disrupt static analysis.

### 5.1. Structural Obfuscation
* **Control Flow Mangling**: Injects opaque predicates and alters the control flow of non-virtualized methods to confuse decompilers.
* **Name Analysis & Renaming**: Strips meaningful class, method, and field names, replacing them with unprintable or confusing characters.
* **Reference Proxy Encoding**: Hides calls to external methods (e.g., framework methods) by redirecting them through dynamically generated delegate proxies.
* **Reduce Metadata Confusion**: Exploits edge cases in the ECMA-335 specification to create metadata that the CLR accepts, but parsing tools (like de4dot or dnlib) crash on.

## 6. Cryptographic Safeguards

### 6.1. Constant and Resource Encryption
* **Mutating Constants**: Hardcoded strings and numeric constants are extracted, encrypted, and replaced with calls to a dynamic decryption helper.
* **Encryption Helpers Injection**: Injects the required cryptographic routines to decrypt constants and embedded resources strictly at runtime. The decryption keys are randomized per build.

## Annex A (Informative): Analysis Resistance
DarksVM explicitly neutralizes automated devirtualizers (such as OldRod). Because the encryption keys, opcode mappings, and VM structures are polymorphic per compilation, analysts must build a custom devirtualization mapping for every individual protected payload.
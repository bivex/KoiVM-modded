// KoiVM Runtime Pipeline Tests
// Tests each component of the compiler→runtime pipeline in isolation
// to find where the mapping breaks.
//
// Compile: bash run.sh
// All .cs from KoiVM.Runtime + this file → single exe

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Data;
using System.Runtime.Serialization.Formatters.Dynamic;
using System.Runtime.Serialization.Formatters.Execution;
using System.Runtime.Serialization.Formatters.OpCodes;
using Microsoft.VisualBasic.Devices;

class Program
{
    static int pass = 0, fail = 0;

    static void Check(string name, bool condition, string detail = "")
    {
        if (condition)
        {
            Console.WriteLine("  [PASS] " + name);
            pass++;
        }
        else
        {
            Console.WriteLine("  [FAIL] " + name + (detail != "" ? " — " + detail : ""));
            fail++;
        }
    }

    static void Section(string title)
    {
        Console.WriteLine("\n=== " + title + " ===");
    }

    static void Main(string[] args)
    {
        Console.WriteLine("KoiVM Runtime Pipeline Tests");
        Console.WriteLine("=============================");

        // =============================================
        // TEST 1: System.Random shuffle determinism
        // =============================================
        Section("1. System.Random(seed) shuffle determinism");

        {
            byte[] P_s1 = ShuffleP_s(172);
            byte[] P_s2 = ShuffleP_s(172);
            bool same = P_s1.SequenceEqual(P_s2);
            Check("Same seed (172) produces same P_s", same);
            if (!same)
            {
                Console.WriteLine("    P_s1[0..5]: " + string.Join(",", P_s1.Take(5)));
                Console.WriteLine("    P_s2[0..5]: " + string.Join(",", P_s2.Take(5)));
            }

            // Check 92 is at P_s[175] for seed=172
            Check("P_s[175] == 92 for seed=172", P_s1[175] == 92,
                "got " + P_s1[175]);

            // Check P_s is a permutation (each value 0-255 appears exactly once)
            var sorted = P_s1.OrderBy(x => x).ToArray();
            bool isPerm = true;
            for (int i = 0; i < 256; i++)
                if (sorted[i] != i) { isPerm = false; break; }
            Check("P_s is a valid permutation of 0..255", isPerm);
        }

        // =============================================
        // TEST 2: NeonVMConstants OP_* values
        // =============================================
        Section("2. NeonVMConstants — OP_* values initialized");

        {
            // NOTE: These fields are all 0 by default!
            // In the real pipeline, RTConstants.InjectConstants patches the .cctor
            // to set these. Here they will be 0 because we're running raw.
            byte opNop = NeonVMConstants.OP_NOP;
            byte opRet = NeonVMConstants.OP_RET;
            byte opPushR = NeonVMConstants.OP_PUSHR_QWORD;
            byte opAdd = NeonVMConstants.OP_ADD_QWORD;

            Console.WriteLine("    OP_NOP=" + opNop + " OP_RET=" + opRet +
                              " OP_PUSH_R64=" + opPushR + " OP_ADD_DWORD=" + opAdd);

            Check("OP_NOP != 0 (constants initialized)", opNop != 0,
                "OP_NOP=" + opNop + " — .cctor not patched, all OP_* are 0!");
            check_opcodes = opNop != 0;
        }

        // =============================================
        // TEST 3: OpCodeMap static constructor
        // =============================================
        Section("3. OpCodeMap — IOpCode discovery");

        {
            // Force OpCodeMap init by calling GetMap
            var map0 = OpCodeMap.GetMap(0);
            Check("GetMap(0) returns non-null", map0 != null);
            Check("GetMap(0) has 256 entries", map0.Length == 256);

            // Count non-null entries
            int nonNull = map0.Count(h => h != null);
            Console.WriteLine("    Non-null handlers for seed=0: " + nonNull + "/256");
            Check("Found IOpCode handlers", nonNull > 0,
                "no IOpCode types discovered!");

            // Discover IOpCode types manually
            int assignableCount = 0;
            var codes = new List<byte>();
            foreach (var type in typeof(OpCodeMap).Assembly.GetTypes())
            {
                if (typeof(IOpCode).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    assignableCount++;
                    var inst = (IOpCode)Activator.CreateInstance(type);
                    codes.Add(inst.Code);
                }
            }
            Console.WriteLine("    IOpCode types found: " + assignableCount);
            Check("Found 69 IOpCode types", assignableCount == 69,
                "found " + assignableCount);

            // Check Code values are unique
            bool unique = codes.Distinct().Count() == codes.Count;
            Check("All IOpCode.Code values are unique", unique,
                codes.Count + " codes, " + codes.Distinct().Count() + " unique");

            // Check if all codes are 0 (uninitialized constants)
            bool allZero = codes.All(c => c == 0);
            if (allZero)
                Console.WriteLine("    WARNING: All Code values are 0! NeonVMConstants not patched.");
            Check("Not all Code values are 0", !allZero,
                "all IOpCode.Code == 0 because NeonVMConstants.OP_* == 0");
        }

        // =============================================
        // TEST 4: GetMap(seed) mapping correctness
        // =============================================
        Section("4. GetMap(seed) — mapping correctness");

        {
            byte seed = 172;
            var map = OpCodeMap.GetMap(seed);
            int mapped = map.Count(h => h != null);
            Console.WriteLine("    GetMap(" + seed + "): " + mapped + " handlers mapped");

            // Build expected indices: P_s[i] for each IOpCode with Code==i
            var P_s = ShuffleP_s(seed);
            var expectedIndices = new HashSet<int>();

            foreach (var type in typeof(OpCodeMap).Assembly.GetTypes())
            {
                if (typeof(IOpCode).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    var inst = (IOpCode)Activator.CreateInstance(type);
                    byte code = inst.Code;
                    expectedIndices.Add(P_s[code]);
                }
            }

            var actualIndices = new HashSet<int>();
            for (int i = 0; i < 256; i++)
                if (map[i] != null) actualIndices.Add(i);

            Console.WriteLine("    Expected indices: " + expectedIndices.Count);
            Console.WriteLine("    Actual indices:   " + actualIndices.Count);

            bool setsMatch = expectedIndices.SetEquals(actualIndices);
            Check("GetMap indices match P_s[IOpCode.Code]", setsMatch,
                "expected " + expectedIndices.Count + " != actual " + actualIndices.Count);

            if (!setsMatch)
            {
                var missing = expectedIndices.Except(actualIndices);
                var extra = actualIndices.Except(expectedIndices);
                if (missing.Any())
                    Console.WriteLine("    Missing: [" + string.Join(",", missing.Take(10)) + "...]");
                if (extra.Any())
                    Console.WriteLine("    Extra:   [" + string.Join(",", extra.Take(10)) + "...]");
            }

            // Specific check: is 92 in the map for seed=172?
            Check("map[92] != null for seed=172", map[92] != null,
                "92 is NOT in the map — this is the runtime bug!");
        }

        // =============================================
        // TEST 5: Rolling cipher symmetry
        // =============================================
        Section("5. Rolling cipher encrypt/decrypt symmetry");

        {
            // Simulate BasicBlockChunk.Encrypt for a single instruction
            // Instruction: [opcode_byte][padding_byte] (2 bytes, like RET)
            byte rawOpcode = 42;  // arbitrary
            byte rawPadding = 99; // arbitrary
            uint entryKey = 0x003d15b7; // from runtime output

            // === Encrypt (compiler side) ===
            byte[] data = new byte[] { rawOpcode, rawPadding };
            uint currentKey = entryKey;
            byte multiplier = (byte)(currentKey >> 8);

            // Encrypt opcode (byte 0)
            {
                var b = data[0]; // raw
                data[0] ^= (byte)currentKey;
                currentKey = (currentKey & 0xFFFFFF00) | (byte)((byte)currentKey * multiplier + b);
            }
            // Encrypt padding (byte 1)
            {
                var b = data[1]; // raw
                data[1] ^= (byte)currentKey;
                currentKey = (currentKey & 0xFFFFFF00) | (byte)((byte)currentKey * multiplier + b);
            }

            Console.WriteLine("    Encrypted: [" + data[0] + ", " + data[1] + "]");
            Console.WriteLine("    Key after encrypt: 0x" + currentKey.ToString("x8"));

            // === Decrypt (runtime side) — simulate ReadByte() ===
            // We need to simulate NeonVMContext.ReadByte which reads from memory
            // and updates REG_K1 rolling key
            uint key2 = entryKey;
            byte decryptedOp, decryptedP;

            unsafe
            {
                // Pin the encrypted data
                byte[] enc = (byte[])data.Clone();
                fixed (byte* ptr = enc)
                {
                    // Simulate: REG_IP points to data, ReadByte reads *IP and XORs
                    byte* ip = ptr;
                    byte raw0 = *ip; // encrypted byte in memory
                    byte dec0 = (byte)(raw0 ^ key2);
                    byte mult2 = (byte)(key2 >> 8);
                    if (mult2 == 0) mult2 = 7;
                    key2 = (key2 & 0xFFFFFF00) | (byte)((byte)key2 * mult2 + dec0);
                    decryptedOp = dec0;

                    ip++;
                    byte raw1 = *ip;
                    byte dec1 = (byte)(raw1 ^ key2);
                    byte mult3 = (byte)(key2 >> 8);
                    if (mult3 == 0) mult3 = 7;
                    key2 = (key2 & 0xFFFFFF00) | (byte)((byte)key2 * mult3 + dec1);
                    decryptedP = dec1;
                }
            }

            Console.WriteLine("    Decrypted: [" + decryptedOp + ", " + decryptedP + "]");
            Console.WriteLine("    Key after decrypt: 0x" + key2.ToString("x8"));

            Check("Decrypted opcode matches original", decryptedOp == rawOpcode,
                "expected " + rawOpcode + " got " + decryptedOp);
            Check("Decrypted padding matches original", decryptedP == rawPadding,
                "expected " + rawPadding + " got " + decryptedP);
            Check("Keys match after encrypt/decrypt", currentKey == key2,
                "enc=0x" + currentKey.ToString("x8") + " dec=0x" + key2.ToString("x8"));
        }

        // =============================================
        // TEST 6: Encrypt vs ReadByte — multiplier difference
        // =============================================
        Section("6. Rolling cipher — multiplier edge case");

        {
            // Encrypt uses: multiplier = (byte)(currentKey >> 8)
            // ReadByte uses: multiplier = (byte)(key >> 8); if (mult == 0) mult = 7;
            // If multiplier == 0, encrypt uses 0 but decrypt uses 7!
            // This would desync the key.

            // Find a key where multiplier (byte)(key >> 8) == 0
            uint keyZeroMult = 0x00001234; // >> 8 = 0x00
            byte encMult = (byte)(keyZeroMult >> 8); // = 0
            byte decMult = (byte)(keyZeroMult >> 8);
            if (decMult == 0) decMult = 7;

            Console.WriteLine("    Key=0x" + keyZeroMult.ToString("x8"));
            Console.WriteLine("    Encrypt multiplier: " + encMult);
            Console.WriteLine("    Decrypt multiplier: " + decMult);

            Check("Encrypt and decrypt multipliers match for key with mult=0",
                encMult == decMult,
                "encrypt uses " + encMult + " but decrypt uses " + decMult +
                " — THIS IS THE BUG!");

            // Test full encrypt/decrypt with zero multiplier key
            byte rawByte = 42;
            uint ek = keyZeroMult;
            byte em = (byte)(ek >> 8);
            byte encrypted = (byte)(rawByte ^ (byte)ek);
            ek = (ek & 0xFFFFFF00) | (byte)((byte)ek * em + rawByte);

            uint dk = keyZeroMult;
            byte dm = (byte)(dk >> 8);
            if (dm == 0) dm = 7;
            byte decrypted = (byte)(encrypted ^ (byte)dk);
            dk = (dk & 0xFFFFFF00) | (byte)((byte)dk * dm + decrypted);

            Check("Decrypted byte correct with zero-mult key", decrypted == rawByte);
            Check("Keys match after zero-mult key", ek == dk,
                "enc_key=0x" + ek.ToString("x8") + " dec_key=0x" + dk.ToString("x8"));
        }

        // =============================================
        // TEST 7: Full pipeline simulation
        // =============================================
        Section("7. Full pipeline: compile RET → encrypt → decrypt → map lookup");

        {
            // Simulate INIT method: single RET instruction
            // Step 1: Compiler creates mapping
            // OP_RET = NeonVMConstants.OP_RET (may be 0 if not patched)
            byte opRet = NeonVMConstants.OP_RET;
            Console.WriteLine("    OP_RET = " + opRet + (opRet == 0 ? " (NOT PATCHED!)" : ""));

            // Step 2: GetMapping(seed)
            byte seed = 172;
            var P_s = ShuffleP_s(seed);
            byte mappedOpcode = P_s[opRet]; // mapping[ILOpCode.RET] = P_s[opCodeOrder[RET]]
            Console.WriteLine("    mappedOpcode (compiler writes) = P_s[" + opRet + "] = " + mappedOpcode);

            // Step 3: Encrypt with EntryKey
            uint entryKey = 0x00093935;
            byte mult = (byte)(entryKey >> 8);

            byte[] instr = new byte[] { mappedOpcode, 0 }; // opcode + padding
            var b0 = instr[0];
            instr[0] ^= (byte)entryKey;
            uint key = (entryKey & 0xFFFFFF00) | (byte)((byte)entryKey * mult + b0);
            var b1 = instr[1];
            instr[1] ^= (byte)key;
            key = (key & 0xFFFFFF00) | (byte)((byte)key * (byte)(key >> 8) + b1);

            Console.WriteLine("    Encrypted bytes: [" + instr[0] + ", " + instr[1] + "]");

            // Step 4: Runtime decrypts via ReadByte
            uint rKey = entryKey;
            byte rOp, rP;
            unsafe
            {
                fixed (byte* p = instr)
                {
                    byte raw0 = *p;
                    byte rm0 = (byte)(rKey >> 8);
                    if (rm0 == 0) rm0 = 7;
                    rOp = (byte)(raw0 ^ (byte)rKey);
                    rKey = (rKey & 0xFFFFFF00) | (byte)((byte)rKey * rm0 + rOp);

                    byte raw1 = *(p + 1);
                    byte rm1 = (byte)(rKey >> 8);
                    if (rm1 == 0) rm1 = 7;
                    rP = (byte)(raw1 ^ (byte)rKey);
                    rKey = (rKey & 0xFFFFFF00) | (byte)((byte)rKey * rm1 + rP);
                }
            }

            Console.WriteLine("    Decrypted: op=" + rOp + " p=" + rP);
            Console.WriteLine("    Expected:  op=" + mappedOpcode);

            Check("Decrypted opcode == mappedOpcode", rOp == mappedOpcode,
                "got " + rOp + " expected " + mappedOpcode);

            // Step 5: Runtime looks up map[rOp]
            var rtMap = OpCodeMap.GetMap(seed);
            bool found = rtMap[rOp] != null;
            Check("map[" + rOp + "] has handler", found,
                "map[" + rOp + "] is null — cannot execute RET!");

            if (found)
            {
                // Check it's actually the RET handler
                Console.WriteLine("    Handler found — RET executes correctly");
            }
            else
            {
                // Show what indices ARE in the map
                var indices = new List<int>();
                for (int i = 0; i < 256; i++) if (rtMap[i] != null) indices.Add(i);
                Console.WriteLine("    Map indices: [" + string.Join(",", indices.Take(20)) + "...]");
                Console.WriteLine("    " + rOp + " is NOT in map indices!");
            }
        }

        // =============================================
        // TEST 8: Multiplier zero-difference detection
        // =============================================
        Section("8. Multiplier zero detection — encrypt vs ReadByte");

        {
            int discrepancies = 0;
            // Scan all possible 24-bit keys for mult=0
            for (uint hi = 0; hi < 256; hi++)
            {
                uint testKey = hi << 8; // mult = hi
                byte encM = (byte)(testKey >> 8);
                byte decM = (byte)(testKey >> 8);
                if (decM == 0) decM = 7;
                if (encM != decM) discrepancies++;
            }
            Console.WriteLine("    Keys with mult=0 (enc != dec): " + discrepancies + "/256");
            Check("No multiplier discrepancies", discrepancies == 0,
                discrepancies + " key prefixes cause encrypt/decrypt mismatch!");
        }

        // =============================================
        // SUMMARY
        // =============================================
        Console.WriteLine("\n=============================");
        Console.WriteLine("Results: " + pass + " PASS, " + fail + " FAIL");
        Console.WriteLine("=============================");

        if (fail > 0)
        {
            Console.WriteLine("\nDIAGNOSIS:");
            if (!check_opcodes)
            {
                Console.WriteLine("  NeonVMConstants.OP_* are all 0 because .cctor was not patched.");
                Console.WriteLine("  In the real pipeline, RTConstants.InjectConstants patches the .cctor");
                Console.WriteLine("  to set OP_* = opCodeOrder[ilOpCode] (shuffled values).");
                Console.WriteLine("  This means all 69 IOpCode instances report Code=0,");
                Console.WriteLine("  so only 1 handler survives in OpCodeMap (last one wins).");
                Console.WriteLine("  The fix: either patch NeonVMConstants.cctor before running,");
                Console.WriteLine("  or the compiler must ensure RTConstants.InjectConstants runs.");
            }
            else
            {
                Console.WriteLine("  Constants are initialized but tests still fail.");
                Console.WriteLine("  Check the specific failing tests above for details.");
            }
        }

        Environment.Exit(fail > 0 ? 1 : 0);
    }

    static bool check_opcodes = false;

    static byte[] ShuffleP_s(int seed)
    {
        var P_s = new byte[256];
        for (int i = 0; i < 256; i++) P_s[i] = (byte)i;
        var r = new Random(seed);
        for (int i = 0; i < 256; i++)
        {
            int j = r.Next(256);
            byte t = P_s[i]; P_s[i] = P_s[j]; P_s[j] = t;
        }
        return P_s;
    }
}

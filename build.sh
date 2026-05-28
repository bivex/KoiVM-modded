#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

BIN_DIR="ConfuserEx-Plus/Debug/bin"
RUNTIME_OUT="/tmp/System.Runtime.Serialization.Formatters.dll"
COMPILER_OUT="/tmp/KoiVM.dll"

WARNINGS="-nowarn:0219,0618,1718,0169,0414,0649,1685"

echo "=== [1] Building KoiVM.Runtime ==="
find KoiVM.Runtime -name "*.cs" ! -path "*/obj/*" | sort | \
  xargs mcs -unsafe -target:library -out:"$RUNTIME_OUT" -r:System.dll $WARNINGS
echo "  -> $RUNTIME_OUT OK"

echo ""
echo "=== [2] Building KoiVM Compiler ==="
find KoiVM -name "*.cs" ! -path "*/obj/*" ! -path "*/VMIR/Compiler/*" | sort | \
  xargs mcs -unsafe -target:library -out:"$COMPILER_OUT" \
    -r:"$BIN_DIR/dnlib.dll" \
    -r:System.dll \
    -r:System.Core.dll \
    -r:"$BIN_DIR/Confuser.Core.dll" \
    $WARNINGS
echo "  -> $COMPILER_OUT OK"

echo ""
echo "=== [3] Copying to $BIN_DIR ==="
cp "$RUNTIME_OUT" "$BIN_DIR/System.Runtime.Serialization.Formatters.dll"
cp "$COMPILER_OUT" "$BIN_DIR/KoiVM.dll"
echo "  -> Done"

echo ""
echo "=== Build complete ==="
ls -la "$BIN_DIR/System.Runtime.Serialization.Formatters.dll" "$BIN_DIR/KoiVM.dll"

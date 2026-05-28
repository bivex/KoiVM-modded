#!/bin/bash
set -e
cd "$(dirname "$0")"

RUNTIME_SRC="../../KoiVM.Runtime"

# Collect all .cs files from runtime, excluding auto-generated AssemblyAttribute
CS_FILES=""
while IFS= read -r f; do
    CS_FILES="$CS_FILES $f"
done < <(find "$RUNTIME_SRC" -name "*.cs" ! -name ".*AssemblyAttribute*" | sort)

echo "[BUILD] Compiling runtime + test..."
if ! mcs -unsafe -debug -out:RuntimeTests.exe Program.cs $CS_FILES 2>&1; then
    echo "[BUILD] FAILED"
    exit 1
fi

echo "[RUN] Executing tests..."
mono RuntimeTests.exe "$@"

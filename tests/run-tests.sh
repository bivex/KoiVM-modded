#!/bin/bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
CONFUSER_BIN="$ROOT/ConfuserEx-Plus/Debug/bin"
TESTS_DIR="$ROOT/tests"
OUTPUT_DIR="$TESTS_DIR/test-output"

echo "=== KoiVM Integration Test Runner ==="
echo ""

# ---- Phase 0: Verify prerequisites ----
echo "[Phase 0] Verifying prerequisites..."
for f in "$CONFUSER_BIN/Confuser.CLI.exe" \
         "$CONFUSER_BIN/Confuser.Core.dll" \
         "$CONFUSER_BIN/KoiVM.Confuser.dll" \
         "$CONFUSER_BIN/KoiVM.dll" \
         "$CONFUSER_BIN/KoiVM.Runtime.dll"; do
    if [ ! -f "$f" ]; then
        echo "FAIL: Missing: $f"
        exit 1
    fi
done
echo "  All prerequisites found."
echo ""

# ---- Phase 1: Build test target ----
echo "[Phase 1] Building test target..."
cd "$TESTS_DIR/KoiVM.TestTarget"
rm -rf bin obj
xbuild KoiVM.TestTarget.csproj /p:Configuration=Debug /p:Platform=AnyCPU /verbosity:minimal
TEST_EXE="$TESTS_DIR/KoiVM.TestTarget/bin/KoiVM.TestTarget.exe"
if [ ! -f "$TEST_EXE" ]; then
    echo "FAIL: Test target not built."
    exit 1
fi
echo "  Built: $TEST_EXE"
echo ""

# ---- Phase 2: Run unprotected (baseline) ----
echo "[Phase 2] Running unprotected target..."
BASELINE_OUTPUT="$OUTPUT_DIR/baseline.txt"
mkdir -p "$OUTPUT_DIR"
cd "$ROOT"
mono "$TEST_EXE" > "$BASELINE_OUTPUT" 2>&1
BASELINE_EXIT=$?
echo "  Exit code: $BASELINE_EXIT"
cat "$BASELINE_OUTPUT" | sed 's/^/    /'
echo ""

# ---- Phase 3: Protect with ConfuserEx + KoiVM ----
echo "[Phase 3] Protecting target with KoiVM..."
rm -rf "$OUTPUT_DIR/protected"
mkdir -p "$OUTPUT_DIR/protected"

WORK_DIR="$OUTPUT_DIR/work"
rm -rf "$WORK_DIR"
mkdir -p "$WORK_DIR"
cp "$CONFUSER_BIN/"*.dll "$CONFUSER_BIN/"*.exe "$WORK_DIR/"
cp "$TEST_EXE" "$WORK_DIR/"

cd "$WORK_DIR"
mono Confuser.CLI.exe -n \
    -plugin "$WORK_DIR/KoiVM.Confuser.dll" \
    -probe "$WORK_DIR" \
    -o "$OUTPUT_DIR/protected" \
    "$WORK_DIR/KoiVM.TestTarget.exe" 2>&1 | tee "$OUTPUT_DIR/confuser-log.txt"
CONFUSER_EXIT=${PIPESTATUS[0]}

if [ "$CONFUSER_EXIT" -ne 0 ]; then
    echo "FAIL: ConfuserEx exited with code $CONFUSER_EXIT"
    cat "$OUTPUT_DIR/confuser-log.txt" | tail -20
    exit 1
fi

PROTECTED_EXE="$OUTPUT_DIR/protected/KoiVM.TestTarget.exe"
if [ ! -f "$PROTECTED_EXE" ]; then
    echo "FAIL: Protected output not found."
    find "$OUTPUT_DIR" -type f
    exit 1
fi
echo "  Protected: $PROTECTED_EXE"
echo ""

# ---- Phase 4: Run protected version ----
echo "[Phase 4] Running protected target..."
cd "$ROOT"
mono "$PROTECTED_EXE" > "$OUTPUT_DIR/protected-raw.txt" 2>&1
PROTECTED_EXIT=$?
# Strip Mono metadata warnings from output
grep -v "^Unknown heap type:" "$OUTPUT_DIR/protected-raw.txt" | sed '/^$/d' > "$OUTPUT_DIR/protected-output.txt"
echo "  Exit code: $PROTECTED_EXIT"
cat "$OUTPUT_DIR/protected-raw.txt" | sed 's/^/    /'
echo ""

# ---- Phase 5: Compare outputs ----
echo "[Phase 5] Comparing outputs..."
if diff "$BASELINE_OUTPUT" "$OUTPUT_DIR/protected-output.txt" > /dev/null 2>&1; then
    echo "  PASS: Outputs match!"
else
    echo "  FAIL: Outputs differ!"
    diff "$BASELINE_OUTPUT" "$OUTPUT_DIR/protected-output.txt" | head -30
    exit 1
fi

echo ""
echo "=== All tests passed ==="

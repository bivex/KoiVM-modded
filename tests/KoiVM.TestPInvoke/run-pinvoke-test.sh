#!/bin/bash
set -e

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$DIR"

echo "=== [1] Building Native Dylib ==="
clang++ -shared -fPIC -o libnative.dylib native.cpp

echo ""
echo "=== [2] Building Managed C# EXE ==="
mcs -debug- -out:TestPInvoke.exe TestPInvoke.cs

echo ""
echo "=== [3] Running Unprotected ==="
export DYLD_LIBRARY_PATH="$DIR"
mono TestPInvoke.exe

echo ""
echo "=== [4] Protecting with KoiVM ==="
mono ../../ConfuserEx-Plus/Debug/bin/Confuser.CLI.exe TestPInvoke.crproj

echo ""
echo "=== [5] Running Protected ==="
cp libnative.dylib protected/
cd protected
export DYLD_LIBRARY_PATH="$PWD"
mono TestPInvoke.exe

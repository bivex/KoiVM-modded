@echo off
REM Компиляция C++/CLI требует Windows и MSVC (cl.exe).
REM Запускать из Developer Command Prompt for Visual Studio.

echo [1] Compiling C++/CLI...
cl.exe /clr /O2 /Fe:TestCppCli.exe TestCppCli.cpp

echo [2] Protecting with KoiVM...
..\..\ConfuserEx-Plus\Debug\bin\Confuser.CLI.exe TestCppCli.crproj

echo [3] Testing protected binary...
protected\TestCppCli.exe

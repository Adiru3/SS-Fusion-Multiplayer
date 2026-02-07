@echo off
echo ========================================
echo SS Fusion Multiplayer - Build Script
echo ========================================
echo.

set FRAMEWORK_PATH=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319
if not exist "%FRAMEWORK_PATH%" set FRAMEWORK_PATH=%WINDIR%\Microsoft.NET\Framework\v4.0.30319

if not exist "%FRAMEWORK_PATH%" (
    echo ERROR: .NET Framework 4.0 not found!
    echo Please install .NET Framework 4.0 or higher.
    pause
    exit /b 1
)

set CSC="%FRAMEWORK_PATH%\csc.exe"
echo Using compiler: %CSC%
echo.

REM Create Output Stats
if not exist "Bin" mkdir Bin
if not exist "Bin\Client" mkdir Bin\Client
if not exist "Bin\Server" mkdir Bin\Server

echo Compiling Core Library...
%CSC% /target:library /out:Core\SSFusionNet.dll /recurse:Core\*.cs
if %errorlevel% neq 0 (
    echo ERROR: Core library compilation failed!
    pause
    exit /b 1
)

echo Compiling Master Server...
%CSC% /target:exe /out:Bin\Server\MasterServer.exe /reference:Core\SSFusionNet.dll /recurse:Server\MasterServer\*.cs
if %errorlevel% neq 0 (
    echo ERROR: Master Server compilation failed!
    pause
    exit /b 1
)

echo Compiling Relay Server...
%CSC% /target:exe /out:Bin\Server\RelayServer.exe /reference:Core\SSFusionNet.dll /recurse:Server\Relay\*.cs
if %errorlevel% neq 0 (
    echo ERROR: Relay Server compilation failed!
    pause
    exit /b 1
)

echo Compiling Launcher...
%CSC% /target:winexe /out:Bin\Client\Launcher.exe /reference:Core\SSFusionNet.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /recurse:Client\Launcher\*.cs
if %errorlevel% neq 0 (
    echo ERROR: Launcher compilation failed!
    pause
    exit /b 1
)

echo.
echo Post-Build: Copying Dependencies...
copy Core\SSFusionNet.dll Bin\Client\ >nul
copy Core\SSFusionNet.dll Bin\Server\ >nul

echo.
echo ========================================
echo Build completed successfully!
echo ========================================
echo.
echo Output Layout:
echo   - Bin/Client/Launcher.exe
echo   - Bin/Server/MasterServer.exe
echo   - Bin/Server/RelayServer.exe
echo.
pause

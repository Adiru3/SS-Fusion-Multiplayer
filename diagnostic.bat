@echo off
echo === SS Fusion Multiplayer - Diagnostic ===
echo.

echo Checking files...
if exist SSFusionNet.dll (echo [OK] SSFusionNet.dll) else (echo [FAIL] SSFusionNet.dll MISSING!)
if exist MasterServer.exe (echo [OK] MasterServer.exe) else (echo [FAIL] MasterServer.exe MISSING!)
if exist Launcher.exe (echo [OK] Launcher.exe) else (echo [FAIL] Launcher.exe MISSING!)
if exist RelayServer.exe (echo [OK] RelayServer.exe) else (echo [FAIL] RelayServer.exe MISSING!)

echo.
echo Checking Core library...
if exist Core\SSFusionNet.dll (echo [OK] Core\SSFusionNet.dll) else (echo [FAIL] Core\SSFusionNet.dll MISSING!)

echo.
echo Checking ports...
netstat -ano | findstr "8080" > nul
if %errorlevel% equ 0 (echo [BUSY] Port 8080 in use - Master Server may be running) else (echo [FREE] Port 8080 available)

netstat -ano | findstr "9000" > nul
if %errorlevel% equ 0 (echo [BUSY] Port 9000 in use - Relay Server may be running) else (echo [FREE] Port 9000 available)

echo.
echo Checking .NET Framework...
reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Version > nul 2>&1
if %errorlevel% equ 0 (
    echo [OK] .NET Framework 4.0+ installed
) else (
    echo [WARN] .NET Framework 4.0+ may not be installed
)

echo.
echo === Diagnostic Complete ===
echo.
echo If any files are MISSING, run: build.bat
echo.
pause

@echo off
echo ========================================
echo SS Fusion - URL Registration Tool
echo ========================================
echo.
echo This tool will register HTTP URLs so Master Server
echo can run WITHOUT administrator privileges.
echo.
echo YOU MUST RUN THIS SCRIPT AS ADMINISTRATOR!
echo (Right-click -^> Run as Administrator)
echo.
pause

echo Checking for Administrator privileges...
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo ERROR: This script requires Administrator privileges!
    echo Please right-click and select "Run as Administrator"
    echo.
    pause
    exit /b 1
)

echo.
echo Registering URL for port 8000...
netsh http add urlacl url=http://+:8000/ user=%USERDOMAIN%\%USERNAME%

if %errorlevel% equ 0 (
    echo.
    echo SUCCESS! URL registered successfully.
    echo.
    echo You can now run Master Server without admin rights.
) else (
    echo.
    echo ERROR: Failed to register URL.
    echo.
)

echo.
echo To remove this registration later, run:
echo   netsh http delete urlacl url=http://+:8000/
echo.
pause

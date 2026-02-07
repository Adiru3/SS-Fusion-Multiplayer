@echo off
echo ========================================
echo Run Master Server & Relay
echo ========================================

echo Starting infrastructure...
cd Bin\Server
start "Master Server" MasterServer.exe -port 8000
start "Relay Server" RelayServer.exe
exit

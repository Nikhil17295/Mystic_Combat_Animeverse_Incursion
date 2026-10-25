@echo off
title Stop Sprite Slicer Server
echo ========================================================
echo   Stopping Sprite Slicer Server on Port 8085...
echo ========================================================
echo.

for /f "tokens=5" %%a in ('netstat -aon ^| findstr :8085 ^| findstr LISTENING') do (
    echo Terminating PID: %%a
    taskkill /F /PID %%a >nul 2>&1
)

echo.
echo Server on port 8085 has been stopped!
timeout /t 2 >nul
exit

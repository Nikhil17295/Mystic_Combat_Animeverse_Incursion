@echo off
title Mystic Combat - Interactive Sprite Slicer
cd /d "%~dp0"

echo =========================================================
echo    MYSTIC COMBAT: SPRITE SLICER ^& LIVE ANIMATOR
echo =========================================================
echo Starting server at http://localhost:8085 ...
echo.

start "" cmd /c "timeout /t 1 /nobreak >nul && start http://localhost:8085/index.html?t=%RANDOM%"
python server.py
pause

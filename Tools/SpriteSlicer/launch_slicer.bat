@echo off
title Mystic Combat - Interactive Sprite Slicer
cd /d "%~dp0"

echo =========================================================
echo    MYSTIC COMBAT: SPRITE SLICER ^& LIVE ANIMATOR
echo =========================================================
echo Starting server at http://localhost:8085 ...
echo.

start "" "http://localhost:8085"
python server.py
pause

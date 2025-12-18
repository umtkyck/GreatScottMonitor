@echo off
title CXA - Create Desktop Shortcut
echo.
echo Creating desktop shortcut for CXA Biometric Auth...
echo.

powershell -ExecutionPolicy Bypass -File "%~dp0scripts\create-desktop-shortcut.ps1"

echo.
pause

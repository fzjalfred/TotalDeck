@echo off
setlocal
rem ============================================================
rem  TotalDeck - one-click Windows x64 build + zip packaging
rem  Works WITH the Tuanjie editor open. All heavy lifting lives
rem  in the inline PowerShell below (single process, synchronous,
rem  with a live progress spinner + elapsed timer).
rem ============================================================
set "PROJECT=%~dp0"
set "PROJECT=%PROJECT:~0,-1%"

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build_windows.ps1" -Project "%PROJECT%"
exit /b %errorlevel%

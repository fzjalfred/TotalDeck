@echo off
setlocal
rem ============================================================
rem  TotalDeck - one-click Windows x64 build + zip packaging
rem  Requirement: CLOSE the Tuanjie editor first. One editor
rem  instance per project - batch builds refuse to run otherwise.
rem ============================================================

rem %~dp0 ends with a backslash - strip it so quoted args stay valid
set "PROJECT=%~dp0"
set "PROJECT=%PROJECT:~0,-1%"

set "TUANJIE=C:\Program Files\Tuanjie\Hub\Editor\2022.3.62t14\Editor\Tuanjie.exe"
set "OUT=%PROJECT%\Builds\Windows"
set "ZIP=%PROJECT%\Builds\TotalDeck_Windows.zip"
set "LOG=%PROJECT%\Logs\build_windows.log"
rem top-level folder name inside the zip - bump the version here
set "PKG=TotalDeck_v0.1"

if not exist "%TUANJIE%" (
    echo [ERROR] Tuanjie editor not found: "%TUANJIE%"
    echo         Edit the TUANJIE variable in this script if you upgraded.
    goto :fail
)

rem Tuanjie does not reliably keep Temp\UnityLockfile - detect a live
rem editor for this project via its process command line instead
powershell -NoProfile -Command "if (Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'Tuanjie.exe' -and $_.CommandLine -match [regex]::Escape('%PROJECT%') }) { exit 1 }"
if errorlevel 1 (
    echo [ERROR] The Tuanjie editor is running with this project open.
    echo         Close the editor first, then re-run this script.
    goto :fail
)

echo [1/3] Building Windows x64 player, this takes a few minutes...
"%TUANJIE%" -batchmode -quit -projectPath "%PROJECT%" -executeMethod TotalDeck.EditorTools.BuildGame.BuildWindows -logFile "%LOG%"
if errorlevel 1 (
    echo [ERROR] Build failed. See log: "%LOG%"
    goto :fail
)
if not exist "%OUT%\TotalDeck.exe" (
    echo [ERROR] TotalDeck.exe not found after build. See log: "%LOG%"
    goto :fail
)
echo       Build output: "%OUT%"

echo [2/3] Packing zip...
if exist "%ZIP%" del "%ZIP%"
rem stage files under a versioned folder so the zip has one top-level dir
set "STAGE=%PROJECT%\Builds\%PKG%"
if exist "%STAGE%" rmdir /s /q "%STAGE%"
xcopy "%OUT%" "%STAGE%\" /e /i /q >nul
powershell -NoProfile -ExecutionPolicy Bypass -Command "Compress-Archive -Path '%STAGE%' -DestinationPath '%ZIP%' -Force"
if errorlevel 1 (
    echo [ERROR] Zip packaging failed.
    rmdir /s /q "%STAGE%"
    goto :fail
)
rmdir /s /q "%STAGE%"

echo [3/3] Done.
echo       Send "%ZIP%" to the target Windows PC, extract anywhere, run TotalDeck.exe.
echo.
pause
exit /b 0

:fail
echo.
pause
exit /b 1

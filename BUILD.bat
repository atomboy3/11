@echo off
chcp 65001 >nul
title Dichoptic Tetris — Android Build

echo ============================================
echo   Dichoptic Tetris — Automated APK Builder
echo ============================================
echo.

:: ── STEP 1: Find Unity Editor ───────────────────────────────────────────
echo [1/4] Searching for Unity Editor...

set UNITY_EXE=

:: Check common Unity Hub locations across drives
for %%D in (C D E F) do (
    for /d %%V in ("%%D:\Program Files\Unity\Hub\Editor\*") do (
        if exist "%%V\Editor\Unity.exe" (
            set UNITY_EXE=%%V\Editor\Unity.exe
        )
    )
)

:: Also check without "Program Files"
if not defined UNITY_EXE (
    for %%D in (C D E F) do (
        for /d %%V in ("%%D:\Unity\Editor\*") do (
            if exist "%%V\Unity.exe" (
                set UNITY_EXE=%%V\Unity.exe
            )
        )
    )
)

:: If still not found, ask user
if not defined UNITY_EXE (
    echo.
    echo [!] Unity not found automatically.
    echo     Please paste the full path to Unity.exe:
    echo     Example: C:\Program Files\Unity\Hub\Editor\2022.3.45f1\Editor\Unity.exe
    echo.
    set /p UNITY_EXE="Unity.exe path: "
)

if not exist "%UNITY_EXE%" (
    echo.
    echo [ERROR] Unity.exe not found at: %UNITY_EXE%
    echo         Please check the path and try again.
    pause
    exit /b 1
)

echo [OK] Found Unity: %UNITY_EXE%
echo.

:: ── STEP 2: Set project and output paths ────────────────────────────────
set PROJECT_PATH=%~dp0
:: Remove trailing backslash
if "%PROJECT_PATH:~-1%"=="\" set PROJECT_PATH=%PROJECT_PATH:~0,-1%

set OUTPUT_DIR=%PROJECT_PATH%\BUILD_OUTPUT
set LOG_FILE=%OUTPUT_DIR%\build_log.txt
set APK_FILE=%OUTPUT_DIR%\DichopticTetris.apk

if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

echo [2/4] Project path : %PROJECT_PATH%
echo       APK output   : %APK_FILE%
echo       Build log    : %LOG_FILE%
echo.

:: ── STEP 3: Check Android SDK ───────────────────────────────────────────
echo [3/4] Checking Android SDK...

:: Unity installs its own SDK here by default
set UNITY_DIR=%UNITY_EXE%
for %%F in ("%UNITY_EXE%") do set UNITY_DIR=%%~dpF

set SDK_PATH=%LOCALAPPDATA%\Android\Sdk
if not exist "%SDK_PATH%" (
    :: Try Unity's bundled SDK
    set SDK_PATH=%UNITY_DIR%..\..\..\PlaybackEngines\AndroidPlayer\SDK
)

if exist "%SDK_PATH%" (
    echo [OK] Android SDK: %SDK_PATH%
) else (
    echo [WARN] Android SDK not found at expected path.
    echo        Unity will use its built-in SDK. This is usually fine.
)
echo.

:: ── STEP 4: Run Unity batch build ───────────────────────────────────────
echo [4/4] Starting Unity build (this takes 3-10 minutes)...
echo       Do NOT close this window.
echo.

"%UNITY_EXE%" ^
    -batchmode ^
    -quit ^
    -projectPath "%PROJECT_PATH%" ^
    -executeMethod BuildScript.BuildAndroid ^
    -logFile "%LOG_FILE%" ^
    -buildTarget Android

set BUILD_EXIT=%ERRORLEVEL%

:: ── RESULT ───────────────────────────────────────────────────────────────
echo.
echo ============================================
if %BUILD_EXIT% EQU 0 (
    if exist "%APK_FILE%" (
        echo   BUILD SUCCEEDED!
        echo.
        echo   APK location:
        echo   %APK_FILE%
        echo.
        echo   Install on OnePlus 12:
        echo   adb install -r "%APK_FILE%"
        echo ============================================
        echo.
        :: Open output folder in Explorer
        explorer "%OUTPUT_DIR%"
    ) else (
        echo   Unity exited OK but APK not found.
        echo   Check log: %LOG_FILE%
        echo ============================================
    )
) else (
    echo   BUILD FAILED (exit code: %BUILD_EXIT%)
    echo.
    echo   Check the log file for errors:
    echo   %LOG_FILE%
    echo.
    echo   Opening log...
    echo ============================================
    :: Show last 30 lines of log for quick diagnosis
    echo.
    echo --- Last lines of build log ---
    powershell -Command "Get-Content '%LOG_FILE%' -Tail 30" 2>nul
    echo.
)

pause

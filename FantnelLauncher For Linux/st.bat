@echo off
setlocal enabledelayedexpansion

rem ============================================================
rem  Fantnel one-click launcher
rem  Starts three components:
rem    1. Backend      : back\Fantnel.exe
rem    2. Frontend     : web\B.bat
rem    3. MySQL bridge : web\server\A.bat
rem ============================================================

cd /d "%~dp0"
title Fantnel Launcher

echo ============================================
echo   Fantnel Launcher
echo   Work dir: %CD%
echo ============================================
echo.

rem ---------- 1. Start backend ----------
echo [1/3] Starting backend: back\Fantnel.exe
if exist "back\Fantnel.exe" (
    start "Fantnel-Backend" cmd /k "cd /d "%~dp0back" && Fantnel.exe"
    echo       [OK] Backend started. Window: Fantnel-Backend
) else (
    echo       [WARN] back\Fantnel.exe NOT found, skipped.
)
echo.

rem ---------- 2. Start frontend ----------
echo [2/3] Starting frontend: web\B.bat
if exist "web\B.bat" (
    start "Fantnel-Web" cmd /k "cd /d "%~dp0web" && B.bat"
    echo       [OK] Frontend started. Window: Fantnel-Web
) else (
    echo       [WARN] web\B.bat NOT found, skipped.
)
echo.

rem ---------- 3. Start MySQL bridge ----------
echo [3/3] Starting MySQL bridge: web\server\A.bat
if exist "web\server\A.bat" (
    start "Fantnel-MySQL-Bridge" cmd /k "cd /d "%~dp0web\server" && A.bat"
    echo       [OK] MySQL bridge started. Window: Fantnel-MySQL-Bridge
) else (
    echo       [WARN] web\server\A.bat NOT found, skipped.
)
echo.


echo ============================================
echo   All components launched.
echo   Check each window for run logs.
echo ============================================
echo.
pause
endlocal
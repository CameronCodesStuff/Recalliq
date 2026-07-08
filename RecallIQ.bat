@echo off
setlocal enabledelayedexpansion
chcp 437 >nul 2>&1
title RecallIQ Launcher
color 0B

set "SCRIPT_DIR=%~dp0"
if "%SCRIPT_DIR:~-1%"=="\" set "SCRIPT_DIR=%SCRIPT_DIR:~0,-1%"
set "UI_PROJECT=%SCRIPT_DIR%\RecallIQ.UI"
set "SLN_FILE=%SCRIPT_DIR%\RecallIQ.sln"
set "LOG_FILE=%SCRIPT_DIR%\recalliq-launcher.log"
set "APPDATA_DIR=%LOCALAPPDATA%\RecallIQ"
set "BUILD_CONFIG=Release"
set "BUILD_PLATFORM=x64"
set "TFM=net8.0-windows10.0.22621.0"
set "BIN_DIR=%UI_PROJECT%\bin\%BUILD_PLATFORM%\%BUILD_CONFIG%\%TFM%"
set "EXE_NAME=RecallIQ.UI.exe"
set "DOTNET_MIN_MAJOR=8"
set "DOTNET_INSTALL_URL=https://dotnet.microsoft.com/download/dotnet/8.0"
set "MODEL_DIR=%SCRIPT_DIR%\models"
set "TESS_DIR=%SCRIPT_DIR%\tessdata"
set "EXIT_CODE=0"
set "DOTNET_PATH="
set "MSBUILD_PATH="
set "OPTIONAL_WARN_MODEL=0"
set "OPTIONAL_WARN_TESS=0"

echo.
echo  ==============================================================
echo       ____                 _ _ ___ ___
echo      ^|  _ \ ___  ___ __ _^| ^| ^|_ _/ _ \
echo      ^| ^|_^) / _ \/ __/ _` ^| ^| ^|^| ^| ^| ^| ^|
echo      ^|  _ ^<  __/ (_^| (_^| ^| ^| ^|^| ^| ^|_^| ^|
echo      ^|_^| \_\___^|\___\__,_^|_^|_^|___\__\_\
echo.
echo      AI-Powered Local File Search for Windows
echo  ==============================================================
echo.

echo [%DATE% %TIME%] RecallIQ Launcher started > "%LOG_FILE%"
echo [%DATE% %TIME%] Script directory: %SCRIPT_DIR% >> "%LOG_FILE%"

REM ==============================================================
REM  PHASE 1: ENVIRONMENT CHECKS
REM ==============================================================

echo  [1/7] Checking Windows version...
call :check_windows
if !EXIT_CODE! neq 0 goto :fatal_exit

echo  [2/7] Checking .NET SDK...
call :check_dotnet
if !EXIT_CODE! neq 0 goto :offer_dotnet_install

echo  [3/7] Checking build tools...
call :check_build_tools

echo  [4/7] Checking solution structure...
call :check_solution
if !EXIT_CODE! neq 0 goto :fatal_exit

REM ==============================================================
REM  PHASE 2: BUILD
REM ==============================================================

echo  [5/7] Building RecallIQ...
call :build_project
if !EXIT_CODE! neq 0 (
    echo.
    echo  [*] Build failed. Trying fallback strategies...
    call :build_fallback
)
if !EXIT_CODE! neq 0 goto :build_failed

REM ==============================================================
REM  PHASE 3: SETUP
REM ==============================================================

echo  [6/7] Setting up directories...
call :setup_directories
call :check_optional_deps

REM ==============================================================
REM  PHASE 4: LAUNCH
REM ==============================================================

echo  [7/7] Launching RecallIQ...
call :launch_app
if !EXIT_CODE! neq 0 (
    echo.
    echo  [*] Launch failed. Trying fallback launch...
    call :launch_fallback
)

goto :cleanup


REM ==============================================================
REM  SUBROUTINES
REM ==============================================================

:check_windows
    echo [%DATE% %TIME%] Checking Windows >> "%LOG_FILE%"
    for /f "tokens=4-5 delims=. " %%a in ('ver') do set "WIN_VER=%%a.%%b"
    echo [%DATE% %TIME%] Windows version: !WIN_VER! >> "%LOG_FILE%"

    set "WIN_MAJOR=0"
    for /f "tokens=1 delims=." %%a in ("!WIN_VER!") do set "WIN_MAJOR=%%a"

    if !WIN_MAJOR! LSS 10 (
        echo  [X] Windows 10 or later required. Detected: !WIN_VER!
        echo [%DATE% %TIME%] FAIL: Windows too old >> "%LOG_FILE%"
        set "EXIT_CODE=1"
        goto :eof
    )

    set "ARCH_OK=0"
    if "%PROCESSOR_ARCHITECTURE%"=="AMD64" set "ARCH_OK=1"
    if "%PROCESSOR_ARCHITEW6432%"=="AMD64" set "ARCH_OK=1"

    if !ARCH_OK! equ 0 (
        echo  [X] 64-bit Windows required. Detected: %PROCESSOR_ARCHITECTURE%
        echo [%DATE% %TIME%] FAIL: Not x64 >> "%LOG_FILE%"
        set "EXIT_CODE=1"
        goto :eof
    )

    echo        Windows !WIN_VER! x64                                 [OK]
    set "EXIT_CODE=0"
    goto :eof


:check_dotnet
    echo [%DATE% %TIME%] Checking .NET SDK >> "%LOG_FILE%"
    set "DOTNET_PATH="

    REM --- Strategy 1: PATH lookup ---
    where dotnet >nul 2>&1
    if !errorlevel! equ 0 (
        set "DOTNET_PATH=dotnet"
        echo [%DATE% %TIME%] Found dotnet in PATH >> "%LOG_FILE%"
        goto :validate_dotnet_version
    )

    REM --- Strategy 2: Program Files ---
    if exist "%ProgramFiles%\dotnet\dotnet.exe" (
        set "DOTNET_PATH=%ProgramFiles%\dotnet\dotnet.exe"
        echo [%DATE% %TIME%] Found at Program Files >> "%LOG_FILE%"
        goto :validate_dotnet_version
    )

    REM --- Strategy 3: Program Files x86 ---
    set "PFX86=%ProgramFiles(x86)%"
    if defined PFX86 (
        if exist "!PFX86!\dotnet\dotnet.exe" (
            set "DOTNET_PATH=!PFX86!\dotnet\dotnet.exe"
            echo [%DATE% %TIME%] Found at PF x86 >> "%LOG_FILE%"
            goto :validate_dotnet_version
        )
    )

    REM --- Strategy 4: User profile ---
    if exist "%USERPROFILE%\.dotnet\dotnet.exe" (
        set "DOTNET_PATH=%USERPROFILE%\.dotnet\dotnet.exe"
        echo [%DATE% %TIME%] Found at user profile >> "%LOG_FILE%"
        goto :validate_dotnet_version
    )

    REM --- Strategy 5: Common alt paths ---
    if exist "C:\dotnet\dotnet.exe" (
        set "DOTNET_PATH=C:\dotnet\dotnet.exe"
        goto :validate_dotnet_version
    )
    if exist "D:\dotnet\dotnet.exe" (
        set "DOTNET_PATH=D:\dotnet\dotnet.exe"
        goto :validate_dotnet_version
    )
    if exist "%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe" (
        set "DOTNET_PATH=%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe"
        goto :validate_dotnet_version
    )

    REM --- Strategy 6: Registry ---
    set "REG_DOTNET="
    for /f "tokens=2*" %%a in ('reg query "HKLM\SOFTWARE\dotnet\Setup\InstalledVersions\x64" /v InstallLocation 2^>nul') do set "REG_DOTNET=%%b"
    if defined REG_DOTNET (
        if exist "!REG_DOTNET!\dotnet.exe" (
            set "DOTNET_PATH=!REG_DOTNET!\dotnet.exe"
            echo [%DATE% %TIME%] Found via registry >> "%LOG_FILE%"
            goto :validate_dotnet_version
        )
    )

    echo  [X] .NET SDK not found on this system.
    echo [%DATE% %TIME%] FAIL: .NET not found >> "%LOG_FILE%"
    set "EXIT_CODE=1"
    goto :eof

:validate_dotnet_version
    echo [%DATE% %TIME%] Validating: !DOTNET_PATH! >> "%LOG_FILE%"
    set "DOTNET_VERSION_OK=0"
    set "DOTNET_VERSION=unknown"

    REM --- Try --list-sdks ---
    for /f "tokens=1" %%v in ('"!DOTNET_PATH!" --list-sdks 2^>nul') do (
        set "SDK_VER=%%v"
        for /f "tokens=1 delims=." %%m in ("!SDK_VER!") do (
            if %%m GEQ %DOTNET_MIN_MAJOR% (
                set "DOTNET_VERSION_OK=1"
                set "DOTNET_VERSION=!SDK_VER!"
            )
        )
    )

    if !DOTNET_VERSION_OK! equ 1 (
        echo        .NET SDK !DOTNET_VERSION!                              [OK]
        echo [%DATE% %TIME%] .NET OK: !DOTNET_VERSION! >> "%LOG_FILE%"
        set "EXIT_CODE=0"
        goto :eof
    )

    REM --- Fallback: --version ---
    for /f "tokens=1" %%v in ('"!DOTNET_PATH!" --version 2^>nul') do (
        set "SDK_VER=%%v"
        for /f "tokens=1 delims=." %%m in ("!SDK_VER!") do (
            if %%m GEQ %DOTNET_MIN_MAJOR% (
                set "DOTNET_VERSION_OK=1"
                set "DOTNET_VERSION=!SDK_VER!"
            )
        )
    )

    if !DOTNET_VERSION_OK! equ 1 (
        echo        .NET SDK !DOTNET_VERSION!                              [OK]
        set "EXIT_CODE=0"
        goto :eof
    )

    echo  [X] .NET %DOTNET_MIN_MAJOR%+ SDK required but only older versions found.
    echo [%DATE% %TIME%] FAIL: .NET too old >> "%LOG_FILE%"
    set "EXIT_CODE=1"
    goto :eof


:offer_dotnet_install
    echo.
    echo  --------------------------------------------------------------
    echo   .NET 8 SDK is required but was not found.
    echo.
    echo   Options:
    echo    [1] Open download page in browser
    echo    [2] Install via winget
    echo    [3] Install via PowerShell script
    echo    [4] Exit
    echo  --------------------------------------------------------------
    echo.
    set /p "DOTNET_CHOICE=  Select option 1-4: "

    if "!DOTNET_CHOICE!"=="1" goto :dotnet_opt_browser
    if "!DOTNET_CHOICE!"=="2" goto :dotnet_opt_winget
    if "!DOTNET_CHOICE!"=="3" goto :dotnet_opt_ps
    goto :pause_exit

:dotnet_opt_browser
    echo [%DATE% %TIME%] User chose browser >> "%LOG_FILE%"
    start "" "%DOTNET_INSTALL_URL%"
    echo.
    echo  Download page opened. Install .NET 8 SDK then re-run this script.
    goto :pause_exit

:dotnet_opt_winget
    echo [%DATE% %TIME%] User chose winget >> "%LOG_FILE%"
    echo.
    echo  Attempting winget install...
    where winget >nul 2>&1
    if !errorlevel! neq 0 (
        echo  [*] winget not available. Trying PowerShell instead...
        echo [%DATE% %TIME%] winget not found >> "%LOG_FILE%"
        goto :dotnet_opt_ps
    )
    winget install Microsoft.DotNet.SDK.8 --accept-source-agreements --accept-package-agreements
    if !errorlevel! neq 0 (
        echo  [*] winget install failed. Trying PowerShell instead...
        echo [%DATE% %TIME%] winget failed >> "%LOG_FILE%"
        goto :dotnet_opt_ps
    )
    echo  .NET 8 SDK installed via winget.                      [OK]
    echo [%DATE% %TIME%] Installed via winget >> "%LOG_FILE%"
    call :refresh_path
    call :check_dotnet
    if !EXIT_CODE! equ 0 goto :after_dotnet_install
    echo.
    echo  winget succeeded but dotnet still not detected.
    echo  Please close this window and re-run the script.
    goto :pause_exit

:dotnet_opt_ps
    echo [%DATE% %TIME%] Trying PowerShell install >> "%LOG_FILE%"
    echo.
    echo  Attempting PowerShell install script...

    set "PS_EXE="
    where pwsh >nul 2>&1
    if !errorlevel! equ 0 (
        set "PS_EXE=pwsh"
        goto :run_ps_install
    )
    where powershell >nul 2>&1
    if !errorlevel! equ 0 (
        set "PS_EXE=powershell"
        goto :run_ps_install
    )

    echo  [X] No PowerShell found. Install .NET 8 manually from:
    echo      %DOTNET_INSTALL_URL%
    goto :pause_exit

:run_ps_install
    "!PS_EXE!" -NoProfile -ExecutionPolicy Bypass -Command "& { try { $ProgressPreference='SilentlyContinue'; Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile \"$env:TEMP\dotnet-install.ps1\"; & \"$env:TEMP\dotnet-install.ps1\" -Channel 8.0 -InstallDir \"$env:ProgramFiles\dotnet\" } catch { Write-Host $_.Exception.Message; exit 1 } }"
    if !errorlevel! neq 0 (
        echo  [X] PowerShell install failed. Install .NET 8 manually from:
        echo      %DOTNET_INSTALL_URL%
        echo [%DATE% %TIME%] PS install failed >> "%LOG_FILE%"
        goto :pause_exit
    )
    echo  .NET 8 SDK installed via PowerShell.                  [OK]
    echo [%DATE% %TIME%] Installed via PS >> "%LOG_FILE%"
    call :refresh_path
    set "DOTNET_PATH=%ProgramFiles%\dotnet\dotnet.exe"
    goto :after_dotnet_install

:after_dotnet_install
    echo  [3/7] Checking build tools...
    call :check_build_tools
    echo  [4/7] Checking solution structure...
    call :check_solution
    if !EXIT_CODE! neq 0 goto :fatal_exit
    echo  [5/7] Building RecallIQ...
    call :build_project
    if !EXIT_CODE! neq 0 (
        echo.
        echo  [*] Build failed. Trying fallback strategies...
        call :build_fallback
    )
    if !EXIT_CODE! neq 0 goto :build_failed
    echo  [6/7] Setting up directories...
    call :setup_directories
    call :check_optional_deps
    echo  [7/7] Launching RecallIQ...
    call :launch_app
    if !EXIT_CODE! neq 0 call :launch_fallback
    goto :cleanup


:check_build_tools
    echo [%DATE% %TIME%] Checking build tools >> "%LOG_FILE%"

    REM --- vswhere ---
    set "PFX86=%ProgramFiles(x86)%"
    set "VSWHERE=!PFX86!\Microsoft Visual Studio\Installer\vswhere.exe"
    if exist "!VSWHERE!" (
        for /f "delims=" %%p in ('"!VSWHERE!" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe 2^>nul') do (
            set "MSBUILD_PATH=%%p"
            echo        MSBuild found via vswhere                         [OK]
            echo [%DATE% %TIME%] MSBuild: %%p >> "%LOG_FILE%"
            goto :eof
        )
    )

    REM --- dotnet msbuild ---
    "!DOTNET_PATH!" msbuild --version >nul 2>&1
    if !errorlevel! equ 0 (
        echo        dotnet msbuild available                           [OK]
        echo [%DATE% %TIME%] Using dotnet msbuild >> "%LOG_FILE%"
        goto :eof
    )

    REM --- VS2022 manual paths ---
    call :find_msbuild_at "%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise"
    if defined MSBUILD_PATH goto :eof
    call :find_msbuild_at "%ProgramFiles%\Microsoft Visual Studio\2022\Professional"
    if defined MSBUILD_PATH goto :eof
    call :find_msbuild_at "%ProgramFiles%\Microsoft Visual Studio\2022\Community"
    if defined MSBUILD_PATH goto :eof
    call :find_msbuild_at "%ProgramFiles%\Microsoft Visual Studio\2022\BuildTools"
    if defined MSBUILD_PATH goto :eof

    echo        [~] No standalone MSBuild, will use dotnet build
    echo [%DATE% %TIME%] No MSBuild >> "%LOG_FILE%"
    goto :eof

:find_msbuild_at
    set "CHECK_PATH=%~1\MSBuild\Current\Bin\MSBuild.exe"
    if exist "!CHECK_PATH!" (
        set "MSBUILD_PATH=!CHECK_PATH!"
        echo        MSBuild found                                      [OK]
        echo [%DATE% %TIME%] MSBuild: !CHECK_PATH! >> "%LOG_FILE%"
    )
    goto :eof


:check_solution
    echo [%DATE% %TIME%] Checking solution >> "%LOG_FILE%"
    set "SOL_OK=1"

    if not exist "%SLN_FILE%" (
        echo  [X] Missing: RecallIQ.sln
        set "SOL_OK=0"
    )
    if not exist "%UI_PROJECT%\RecallIQ.UI.csproj" (
        echo  [X] Missing: RecallIQ.UI\RecallIQ.UI.csproj
        set "SOL_OK=0"
    )
    if not exist "%UI_PROJECT%\App.xaml.cs" (
        echo  [X] Missing: RecallIQ.UI\App.xaml.cs
        set "SOL_OK=0"
    )
    if not exist "%SCRIPT_DIR%\RecallIQ.Core\RecallIQ.Core.csproj" (
        echo  [X] Missing: RecallIQ.Core\RecallIQ.Core.csproj
        set "SOL_OK=0"
    )

    if !SOL_OK! equ 1 (
        echo        Solution structure verified                        [OK]
        set "EXIT_CODE=0"
    ) else (
        echo.
        echo  Make sure you extracted the full ZIP and are running
        echo  this .bat from inside the RecallIQ folder.
        echo [%DATE% %TIME%] FAIL: Missing files >> "%LOG_FILE%"
        set "EXIT_CODE=1"
    )
    goto :eof


:build_project
    echo [%DATE% %TIME%] Starting build >> "%LOG_FILE%"
    set "EXIT_CODE=0"

    if exist "%BIN_DIR%\%EXE_NAME%" (
        echo        Found existing build, rebuilding to be safe...
    )

    echo        Running dotnet restore...
    "!DOTNET_PATH!" restore "%SLN_FILE%" --verbosity quiet 2>>"%LOG_FILE%"
    echo        Running dotnet build Release x64...
    "!DOTNET_PATH!" build "%SLN_FILE%" -c %BUILD_CONFIG% -p:Platform=%BUILD_PLATFORM% --no-restore --verbosity quiet 2>>"%LOG_FILE%"
    if !errorlevel! equ 0 (
        if exist "%BIN_DIR%\%EXE_NAME%" (
            echo        Build successful                                   [OK]
            echo [%DATE% %TIME%] Build OK >> "%LOG_FILE%"
            goto :eof
        )
    )
    echo [%DATE% %TIME%] dotnet build failed >> "%LOG_FILE%"
    set "EXIT_CODE=1"
    goto :eof


:build_fallback
    echo [%DATE% %TIME%] Build fallback >> "%LOG_FILE%"
    set "EXIT_CODE=1"

    REM --- FB1: Verbose build ---
    echo        Fallback 1/7: Verbose build for diagnostics...
    "!DOTNET_PATH!" build "%SLN_FILE%" -c %BUILD_CONFIG% -p:Platform=%BUILD_PLATFORM% --verbosity detailed >"%SCRIPT_DIR%\build-verbose.log" 2>&1
    if !errorlevel! equ 0 (
        if exist "%BIN_DIR%\%EXE_NAME%" (
            echo        Verbose build succeeded                            [OK]
            set "EXIT_CODE=0"
            goto :eof
        )
    )

    REM --- FB2: Nuke and rebuild ---
    echo        Fallback 2/7: Clean rebuild...
    echo [%DATE% %TIME%] Clean rebuild >> "%LOG_FILE%"
    "!DOTNET_PATH!" clean "%SLN_FILE%" -c %BUILD_CONFIG% -p:Platform=%BUILD_PLATFORM% --verbosity quiet 2>>"%LOG_FILE%"
    for /d %%d in ("%SCRIPT_DIR%\RecallIQ.*") do (
        if exist "%%d\bin" rmdir /s /q "%%d\bin" 2>nul
        if exist "%%d\obj" rmdir /s /q "%%d\obj" 2>nul
    )
    "!DOTNET_PATH!" restore "%SLN_FILE%" --force --verbosity quiet 2>>"%LOG_FILE%"
    "!DOTNET_PATH!" build "%SLN_FILE%" -c %BUILD_CONFIG% -p:Platform=%BUILD_PLATFORM% --verbosity quiet 2>>"%LOG_FILE%"
    if !errorlevel! equ 0 (
        if exist "%BIN_DIR%\%EXE_NAME%" (
            echo        Clean rebuild succeeded                            [OK]
            set "EXIT_CODE=0"
            goto :eof
        )
    )

    REM --- FB3: UI project only ---
    echo        Fallback 3/7: Building UI project directly...
    "!DOTNET_PATH!" build "%UI_PROJECT%\RecallIQ.UI.csproj" -c %BUILD_CONFIG% -p:Platform=%BUILD_PLATFORM% --verbosity quiet 2>>"%LOG_FILE%"
    if !errorlevel! equ 0 (
        if exist "%BIN_DIR%\%EXE_NAME%" (
            echo        UI project build succeeded                         [OK]
            set "EXIT_CODE=0"
            goto :eof
        )
    )

    REM --- FB4: Debug config ---
    echo        Fallback 4/7: Trying Debug configuration...
    set "BUILD_CONFIG=Debug"
    set "BIN_DIR=%UI_PROJECT%\bin\%BUILD_PLATFORM%\Debug\%TFM%"
    "!DOTNET_PATH!" build "%SLN_FILE%" -c Debug -p:Platform=%BUILD_PLATFORM% --verbosity quiet 2>>"%LOG_FILE%"
    if !errorlevel! equ 0 (
        if exist "!BIN_DIR!\%EXE_NAME%" (
            echo        Debug build succeeded                              [OK]
            set "EXIT_CODE=0"
            goto :eof
        )
    )

    REM --- FB5: AnyCPU ---
    echo        Fallback 5/7: Trying AnyCPU platform...
    set "BUILD_CONFIG=Release"
    set "BUILD_PLATFORM=AnyCPU"
    set "BIN_DIR=%UI_PROJECT%\bin\Release\%TFM%"
    "!DOTNET_PATH!" build "%SLN_FILE%" -c Release --verbosity quiet 2>>"%LOG_FILE%"
    if !errorlevel! equ 0 (
        if exist "!BIN_DIR!\%EXE_NAME%" (
            echo        AnyCPU build succeeded                             [OK]
            set "EXIT_CODE=0"
            goto :eof
        )
    )

    REM --- FB6: MSBuild directly ---
    if not defined MSBUILD_PATH goto :build_fb7
    echo        Fallback 6/7: MSBuild directly...
    set "BUILD_CONFIG=Release"
    set "BUILD_PLATFORM=x64"
    set "BIN_DIR=%UI_PROJECT%\bin\x64\Release\%TFM%"
    "!DOTNET_PATH!" restore "%SLN_FILE%" --verbosity quiet 2>>"%LOG_FILE%"
    "!MSBUILD_PATH!" "%SLN_FILE%" /p:Configuration=Release /p:Platform=x64 /verbosity:quiet /nologo 2>>"%LOG_FILE%"
    if !errorlevel! equ 0 (
        if exist "!BIN_DIR!\%EXE_NAME%" (
            echo        MSBuild succeeded                                  [OK]
            set "EXIT_CODE=0"
            goto :eof
        )
    )
    goto :build_fb7

:build_fb7
    REM --- FB7: dotnet publish ---
    echo        Fallback 7/7: dotnet publish...
    set "BUILD_CONFIG=Release"
    set "BUILD_PLATFORM=x64"
    set "PUBLISH_DIR=%SCRIPT_DIR%\publish"
    "!DOTNET_PATH!" publish "%UI_PROJECT%\RecallIQ.UI.csproj" -c Release -p:Platform=x64 --self-contained false -o "!PUBLISH_DIR!" --verbosity quiet 2>>"%LOG_FILE%"
    if !errorlevel! equ 0 (
        if exist "!PUBLISH_DIR!\%EXE_NAME%" (
            set "BIN_DIR=!PUBLISH_DIR!"
            echo        Publish succeeded                                  [OK]
            set "EXIT_CODE=0"
            goto :eof
        )
    )

    echo [%DATE% %TIME%] FAIL: All build strategies exhausted >> "%LOG_FILE%"
    set "EXIT_CODE=1"
    goto :eof


:setup_directories
    echo [%DATE% %TIME%] Setting up dirs >> "%LOG_FILE%"
    if not exist "%APPDATA_DIR%" mkdir "%APPDATA_DIR%" 2>nul
    if not exist "%APPDATA_DIR%\logs" mkdir "%APPDATA_DIR%\logs" 2>nul
    if not exist "%MODEL_DIR%" mkdir "%MODEL_DIR%" 2>nul
    if not exist "%TESS_DIR%" mkdir "%TESS_DIR%" 2>nul
    echo        App directories ready                              [OK]
    goto :eof


:check_optional_deps
    echo [%DATE% %TIME%] Checking optional deps >> "%LOG_FILE%"

    REM --- ONNX model ---
    set "OPTIONAL_WARN_MODEL=1"
    if exist "%MODEL_DIR%\all-MiniLM-L6-v2.onnx" set "OPTIONAL_WARN_MODEL=0"
    if exist "%MODEL_DIR%\model.onnx" set "OPTIONAL_WARN_MODEL=0"
    if exist "!BIN_DIR!\models\all-MiniLM-L6-v2.onnx" set "OPTIONAL_WARN_MODEL=0"
    if exist "!BIN_DIR!\models\model.onnx" set "OPTIONAL_WARN_MODEL=0"

    if !OPTIONAL_WARN_MODEL! equ 0 (
        echo        ONNX model found                                   [OK]
    ) else (
        echo        [~] No ONNX model -- using hash fallback embeddings
    )

    REM --- Tesseract ---
    set "OPTIONAL_WARN_TESS=1"
    if exist "%TESS_DIR%\eng.traineddata" set "OPTIONAL_WARN_TESS=0"
    if exist "!BIN_DIR!\tessdata\eng.traineddata" set "OPTIONAL_WARN_TESS=0"

    if !OPTIONAL_WARN_TESS! equ 0 (
        echo        Tesseract data found                               [OK]
    ) else (
        echo        [~] No Tesseract data -- OCR disabled
    )

    REM --- Windows App SDK ---
    set "WASDK_FOUND=0"
    reg query "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModel\PackageRepository\Packages" /f "WindowsAppRuntime" 2>nul | findstr /i "WindowsAppRuntime" >nul 2>&1
    if !errorlevel! equ 0 set "WASDK_FOUND=1"

    if !WASDK_FOUND! equ 1 (
        echo        Windows App SDK runtime detected                   [OK]
    ) else (
        echo        [~] Windows App SDK runtime may install on first run
    )
    goto :eof


:launch_app
    echo [%DATE% %TIME%] Launching: !BIN_DIR!\%EXE_NAME% >> "%LOG_FILE%"
    set "EXIT_CODE=0"

    if not exist "!BIN_DIR!\%EXE_NAME%" (
        echo  [X] Executable not found: !BIN_DIR!\%EXE_NAME%
        echo [%DATE% %TIME%] FAIL: exe missing >> "%LOG_FILE%"
        set "EXIT_CODE=1"
        goto :eof
    )

    echo.
    echo  --------------------------------------------------------------
    echo   RecallIQ is starting...
    echo   Close this window or press Ctrl+C to stop.
    echo  --------------------------------------------------------------
    echo.

    pushd "!BIN_DIR!"
    start "" "!BIN_DIR!\%EXE_NAME%" 2>>"%LOG_FILE%"
    set "LAUNCH_ERR=!errorlevel!"
    popd

    if !LAUNCH_ERR! equ 0 (
        echo [%DATE% %TIME%] Launch OK >> "%LOG_FILE%"
        timeout /t 3 /nobreak >nul
        goto :eof
    )

    echo [%DATE% %TIME%] Direct launch error: !LAUNCH_ERR! >> "%LOG_FILE%"
    set "EXIT_CODE=1"
    goto :eof


:launch_fallback
    echo [%DATE% %TIME%] Launch fallback >> "%LOG_FILE%"

    REM --- FB1: dotnet exec ---
    echo        Fallback 1/5: dotnet exec...
    if exist "!BIN_DIR!\RecallIQ.UI.dll" (
        pushd "!BIN_DIR!"
        start "" "!DOTNET_PATH!" exec "!BIN_DIR!\RecallIQ.UI.dll" 2>>"%LOG_FILE%"
        popd
        if !errorlevel! equ 0 (
            echo        dotnet exec launched                               [OK]
            timeout /t 3 /nobreak >nul
            goto :eof
        )
    )

    REM --- FB2: dotnet run ---
    echo        Fallback 2/5: dotnet run...
    start "" "!DOTNET_PATH!" run --project "%UI_PROJECT%\RecallIQ.UI.csproj" -c !BUILD_CONFIG! 2>>"%LOG_FILE%"
    if !errorlevel! equ 0 (
        echo        dotnet run launched                                [OK]
        timeout /t 3 /nobreak >nul
        goto :eof
    )

    REM --- FB3: explorer ---
    echo        Fallback 3/5: Explorer launch...
    explorer "!BIN_DIR!\%EXE_NAME%" 2>nul
    timeout /t 3 /nobreak >nul

    REM --- FB4: PowerShell Start-Process ---
    echo        Fallback 4/5: PowerShell Start-Process...
    set "PS_EXE="
    where pwsh >nul 2>&1
    if !errorlevel! equ 0 (
        set "PS_EXE=pwsh"
        goto :run_ps_launch
    )
    where powershell >nul 2>&1
    if !errorlevel! equ 0 (
        set "PS_EXE=powershell"
        goto :run_ps_launch
    )
    goto :launch_fb5

:run_ps_launch
    "!PS_EXE!" -NoProfile -Command "Start-Process -FilePath '!BIN_DIR!\%EXE_NAME%' -WorkingDirectory '!BIN_DIR!'" 2>>"%LOG_FILE%"
    if !errorlevel! equ 0 (
        echo        PowerShell launch succeeded                        [OK]
        timeout /t 3 /nobreak >nul
        goto :eof
    )

:launch_fb5
    REM --- FB5: Open folder ---
    echo.
    echo  [X] All automatic launch methods failed.
    echo      Opening the build folder -- run %EXE_NAME% manually.
    echo [%DATE% %TIME%] All launch fallbacks failed >> "%LOG_FILE%"
    explorer "!BIN_DIR!"
    goto :eof


:refresh_path
    echo [%DATE% %TIME%] Refreshing PATH >> "%LOG_FILE%"
    set "SYS_PATH="
    set "USR_PATH="
    for /f "tokens=2*" %%a in ('reg query "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment" /v Path 2^>nul') do set "SYS_PATH=%%b"
    for /f "tokens=2*" %%a in ('reg query "HKCU\Environment" /v Path 2^>nul') do set "USR_PATH=%%b"
    if defined SYS_PATH (
        if defined USR_PATH (
            set "PATH=!SYS_PATH!;!USR_PATH!"
        ) else (
            set "PATH=!SYS_PATH!"
        )
    )
    goto :eof


:build_failed
    echo.
    echo  ==============================================================
    echo                        BUILD FAILED
    echo  ==============================================================
    echo.
    echo  All 7 build strategies were exhausted. Things to try:
    echo.
    echo  1. Open RecallIQ.sln in Visual Studio 2022
    echo  2. Make sure these workloads are installed:
    echo     - .NET Desktop Development
    echo     - Windows App SDK
    echo  3. Select x64 platform, then Build - Build Solution
    echo.
    echo  Build log:    %LOG_FILE%
    if exist "%SCRIPT_DIR%\build-verbose.log" (
        echo  Verbose log:  %SCRIPT_DIR%\build-verbose.log
    )
    echo.
    set /p "OPEN_LOG=  Open build log in Notepad? Y/N: "
    if /i "!OPEN_LOG!"=="Y" (
        if exist "%SCRIPT_DIR%\build-verbose.log" (
            notepad "%SCRIPT_DIR%\build-verbose.log"
        ) else (
            notepad "%LOG_FILE%"
        )
    )
    goto :pause_exit

:fatal_exit
    echo.
    echo  [X] Fatal error. See log: %LOG_FILE%
    goto :pause_exit

:cleanup
    echo.
    if !OPTIONAL_WARN_MODEL! equ 1 (
        echo  --------------------------------------------------------------
        echo   OPTIONAL: For better search, download all-MiniLM-L6-v2.onnx
        echo   from Hugging Face and place in: %MODEL_DIR%\
    )
    if !OPTIONAL_WARN_TESS! equ 1 (
        echo  --------------------------------------------------------------
        echo   OPTIONAL: For OCR support, download eng.traineddata
        echo   and place in: %TESS_DIR%\
    )
    echo  --------------------------------------------------------------
    echo.
    echo [%DATE% %TIME%] Launcher finished >> "%LOG_FILE%"
    echo  RecallIQ launched. You can close this window.
    echo.
    timeout /t 10 /nobreak >nul
    goto :end

:pause_exit
    echo.
    pause
    goto :end

:end
    endlocal
    exit /b %EXIT_CODE%

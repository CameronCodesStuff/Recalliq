@echo off
setlocal
echo.
echo  Building RecallIQ...
echo.

set "SCRIPT_DIR=%~dp0"
if "%SCRIPT_DIR:~-1%"=="\" set "SCRIPT_DIR=%SCRIPT_DIR:~0,-1%"
set "PUBLISH_DIR=%SCRIPT_DIR%\publish"

where dotnet >nul 2>&1
if errorlevel 1 (
    echo  [X] dotnet not found in PATH. Install .NET 8 SDK first.
    pause
    exit /b 1
)

echo  Cleaning previous build...
if exist "%PUBLISH_DIR%" rmdir /s /q "%PUBLISH_DIR%"

echo  Publishing RecallIQ.UI...
dotnet publish "%SCRIPT_DIR%\RecallIQ.UI\RecallIQ.UI.csproj" -c Release -r win-x64 --self-contained false -o "%PUBLISH_DIR%" -p:Platform=x64

if errorlevel 1 (
    echo.
    echo  [X] Build failed. Make sure .NET 8 SDK and Windows App SDK workload are installed.
    pause
    exit /b 1
)

echo.
echo  Build complete. Output: %PUBLISH_DIR%
echo.
echo  To create the installer, open installer\RecallIQ.iss in Inno Setup
echo  and click Build ^> Compile.
echo.
pause

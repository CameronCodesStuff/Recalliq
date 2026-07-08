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
for /d %%d in ("%SCRIPT_DIR%\RecallIQ.*") do (
    if exist "%%d\obj" rmdir /s /q "%%d\obj" 2>nul
    if exist "%%d\bin" rmdir /s /q "%%d\bin" 2>nul
)

echo  Restoring packages...
dotnet restore "%SCRIPT_DIR%\RecallIQ.sln" -p:Platform=x64

if errorlevel 1 (
    echo.
    echo  [X] Restore failed. Check your internet connection.
    pause
    exit /b 1
)

echo  Building RecallIQ.UI...
dotnet build "%SCRIPT_DIR%\RecallIQ.UI\RecallIQ.UI.csproj" -c Release -p:Platform=x64

if errorlevel 1 (
    echo.
    echo  [X] Build failed. Make sure .NET 8 SDK and Windows App SDK workload are installed.
    pause
    exit /b 1
)

set "BUILD_OUT=%SCRIPT_DIR%\RecallIQ.UI\bin\x64\Release\net8.0-windows10.0.22621.0"

echo  Copying build output to publish folder...
xcopy "%BUILD_OUT%\*" "%PUBLISH_DIR%\" /E /I /Y /Q >nul

echo.
echo  Build complete. Output: %PUBLISH_DIR%
echo.
echo  To create the installer, open installer\RecallIQ.iss in Inno Setup
echo  and click Build ^> Compile.
echo.
pause

@echo off
setlocal EnableDelayedExpansion
cd /d "%~dp0"
chcp 65001 >nul


set "WHAT=%~1"
if "%WHAT%"=="" set "WHAT=all"
set DO48=0
set DOFULL=0
if /i "%WHAT%"=="all" set DO48=1
if /i "%WHAT%"=="all" set DOFULL=1
if /i "%WHAT%"=="net48" set DO48=1
if /i "%WHAT%"=="full" set DOFULL=1
if "%DO48%%DOFULL%"=="00" (
  echo Usage: build.bat [net48 ^| full ^| all]
  goto fail
)

if not exist redist\win7 mkdir redist\win7

echo [1/4] Building apps ...
for %%A in (x64 x86) do (
  echo   .NET Framework 4.8 %%A
  dotnet build net48\GamepadServer.Net48.csproj -c Release -p:Arch=%%A -o publish\net48\%%A
  if errorlevel 1 goto fail
)

echo [2/4] Downloading redistributables (skips files that already exist) ...
call :get "redist\ViGEmBus_1.22.0_x64_x86_arm64.exe" "https://github.com/nefarius/ViGEmBus/releases/download/v1.22.0/ViGEmBus_1.22.0_x64_x86_arm64.exe"
call :get "redist\vc_redist.x64.exe" "https://aka.ms/vs/16/release/vc_redist.x64.exe"
call :get "redist\vc_redist.x86.exe" "https://aka.ms/vs/16/release/vc_redist.x86.exe"
if "%DOFULL%"=="1" call :get "redist\ndp48-x86-x64-allos-enu.exe" "https://go.microsoft.com/fwlink/?linkid=2088631"
call :get "redist\win7\ViGEmBus_Setup_1.16.116.exe" "https://github.com/nefarius/ViGEmBus/releases/download/setup-v1.16.116/ViGEmBus_Setup_1.16.116.exe"

echo [3/4] Checking redist files ...
if not exist redist\ViGEmBus_1.22.0_x64_x86_arm64.exe (
  echo   MISSING: redist\ViGEmBus_1.22.0_x64_x86_arm64.exe
  goto fail
)
if "%DOFULL%"=="1" if not exist redist\ndp48-x86-x64-allos-enu.exe (
  echo   MISSING: redist\ndp48-x86-x64-allos-enu.exe  ^(needed for the Full installers^)
  goto fail
)
set LEGACY=1
for %%F in (
  redist\vc_redist.x64.exe
  redist\vc_redist.x86.exe
  redist\win7\ViGEmBus_Setup_1.16.116.exe
  redist\win7\Windows6.1-KB3033929-x64.msu
  redist\win7\Windows6.1-KB3033929-x86.msu
  redist\win7\Xbox360_64Eng.exe
  redist\win7\Xbox360_32Eng.exe
) do if not exist "%%F" ( echo   missing: %%F & set LEGACY=0 )
if "!LEGACY!"=="0" echo   ^> The Win7/8.1 installers will be SKIPPED until these files exist - see redist\README.txt

set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" goto no_inno

echo [4/4] Building installers ...
for %%A in (x64 x86) do (
  for %%T in (modern legacy) do (
    if "%DO48%"=="1" (
      call :iscc %%A %%T net48
      if errorlevel 1 goto fail
    )
    if "%DOFULL%"=="1" (
      call :iscc %%A %%T full
      if errorlevel 1 goto fail
    )
  )
)

echo.
echo Done - see the dist folder:
dir /b dist\*.exe
pause
exit /b 0

:iscc
if /i "%~2"=="legacy" if "!LEGACY!"=="0" exit /b 0
"%ISCC%" /DArch=%~1 /DTarget=%~2 /DFw=%~3 installer\GamepadServer.iss
exit /b %errorlevel%

:get
if exist "%~1" exit /b 0
echo   downloading %~nx1
powershell -NoProfile -Command "try { [Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -UseBasicParsing -Uri '%~2' -OutFile '%~1' } catch { exit 1 }"
if errorlevel 1 echo   could not download %~nx1 - get it manually
exit /b 0

:no_inno
echo.
echo Inno Setup 6 not found. Install it from https://jrsoftware.org/isdl.php and run this again.
goto fail

:fail
echo.
echo Build failed.
pause
exit /b 1

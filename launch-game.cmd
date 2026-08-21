@echo off
setlocal
cd /d "%~dp0"

set "ANDROID_SDK_ROOT=%~dp0.android-sdk"
set "ANDROID_HOME=%~dp0.android-sdk"
set "ANDROID_AVD_HOME=%~dp0.android-avd"
set "ADB=%~dp0.android-sdk\platform-tools\adb.exe"
set "EMU=%~dp0.android-sdk\emulator\emulator.exe"
set "APK=%~dp0build\NightfallProtocol-debug.apk"

if not exist "%EMU%" (
  echo ERROR: Android emulator was not found.
  pause
  exit /b 1
)
if not exist "%ADB%" (
  echo ERROR: ADB was not found.
  pause
  exit /b 1
)

"%ADB%" start-server >nul 2>&1
"%ADB%" emu kill >nul 2>&1
ping -n 4 127.0.0.1 >nul
echo Starting visible Android emulator...
start "" "%EMU%" -avd Nightfall_Test -gpu swiftshader_indirect -memory 3072 -no-snapshot -no-boot-anim

echo Waiting for Android to boot...
set /a tries=0
:wait_device
"%ADB%" get-state 2>nul | findstr /c:"device" >nul
if not errorlevel 1 goto wait_boot
set /a tries+=1
if %tries% geq 90 goto failed
ping -n 3 127.0.0.1 >nul
goto wait_device

:wait_boot
for /f "usebackq delims=" %%B in (`"%ADB%" shell getprop sys.boot_completed 2^>nul`) do set "BOOTED=%%B"
if "%BOOTED%"=="1" goto launch
set /a tries+=1
if %tries% geq 90 goto failed
ping -n 3 127.0.0.1 >nul
goto wait_boot

:launch
if exist "%APK%" "%ADB%" install -r "%APK%" >nul 2>&1
"%ADB%" shell am force-stop com.nightfall.protocol >nul 2>&1
"%ADB%" shell am start -n com.nightfall.protocol/.GameActivity
echo.
echo Nightfall Protocol is running. You can close this window.
ping -n 6 127.0.0.1 >nul
exit /b 0

:failed
echo.
echo ERROR: Android did not start within three minutes.
echo Keep this window open and send a screenshot of the error.
pause
exit /b 1

@echo off
setlocal
set "GAME_EXE=%~dp0UnityProject\Builds\Windows\NightfallUnity.exe"
if not exist "%GAME_EXE%" (
  echo Windows build not found:
  echo %GAME_EXE%
  pause
  exit /b 1
)
start "Nightfall Protocol" "%GAME_EXE%"
endlocal

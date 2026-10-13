@echo off
REM Fix hollow/corrupt Library\PackageCache (missing .cs sources causing CS0234/CS0246 cascades).
REM Close Unity completely before running this.

echo Stopping leftover Unity build processes...
taskkill /F /IM Unity.exe >nul 2>&1
taskkill /F /IM UnityHub.exe >nul 2>&1
taskkill /F /IM bee_backend.exe >nul 2>&1
taskkill /F /IM Unity.ILPP.Runner.exe >nul 2>&1

set "ROOT=%~dp0"
set "LIB=%ROOT%Library"

if not exist "%LIB%" (
  echo Library folder not found. Is this the project root?
  pause
  exit /b 1
)

echo Removing corrupted package/build caches...
rmdir /S /Q "%LIB%\PackageCache" 2>nul
rmdir /S /Q "%LIB%\ScriptAssemblies" 2>nul
rmdir /S /Q "%LIB%\Bee" 2>nul
rmdir /S /Q "%LIB%\PackageManager" 2>nul

echo Done.
echo.
echo Reopen the project in Unity 6000.0.75f1 and wait for packages to download
echo and scripts to finish compiling. First open can take several minutes.
pause

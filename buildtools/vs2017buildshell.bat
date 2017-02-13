echo on

:: script expects the folder location of the build root, as the vs env script changes
:: folder location
if %1.x == .x goto error

:: also want the configuration to build (debug/release)
if %2.x == .x goto error

call "C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\Tools\VsDevCmd.bat"

:: reset back to the build root
cd %1

msbuild buildtools\build.vs2017.proj /p:Configuration=%2
goto exit

:error
echo Missing parameter: vs2017buildshell build-root-folder configuration

:exit


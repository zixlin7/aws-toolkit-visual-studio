:: script expects the folder location of the build root, as the vs env script changes
:: folder location
if %1.x == .x goto error

:: also want the configuration to build (debug/release)
if %2.x == .x goto error

:: manually set up env, as dev batch files not working too reliably in RC
setlocal

set DevEnvDir=C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\IDE\
Set ExtensionSdkDir=C:\Program Files (x86)\Microsoft SDKs\Windows Kits\10\ExtensionSDKs
Set INCLUDE=C:\Program Files (x86)\Windows Kits\NETFXSDK\4.6.1\include\um;C:\Program Files (x86)\Windows Kits\10\include\10.0.14393.0\ucrt;C:\Program Files (x86)\Windows Kits\10\include\10.0.14393.0\shared;C:\Program Files (x86)\Windows Kits\10\include\10.0.14393.0\um;C:\Program Files (x86)\Windows Kits\10\include\10.0.14393.0\winrt;
Set LIB=C:\Program Files (x86)\Windows Kits\NETFXSDK\4.6.1\lib\um\x86;C:\Program Files (x86)\Windows Kits\10\lib\10.0.14393.0\ucrt\x86;C:\Program Files (x86)\Windows Kits\10\lib\10.0.14393.0\um\x86;
Set LIBPATH=C:\Program Files (x86)\Windows Kits\10\UnionMetadata;C:\Program Files (x86)\Windows Kits\10\References;C:\Windows\Microsoft.NET\Framework\v4.0.30319;
Set NETFXSDKDir=C:\Program Files (x86)\Windows Kits\NETFXSDK\4.6.1\
Set Path=C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\IDE\VC\VCPackages;C:\Program Files (x86)\Microsoft SDKs\TypeScript\2.1;C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\IDE\CommonExtensions\Microsoft\TestWindow;C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer;C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\bin\Roslyn;C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Team Tools\Performance Tools;C:\Program Files (x86)\Microsoft Visual Studio\Shared\Common\VSPerfCollectionTools\;C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\;C:\Program Files (x86)\Windows Kits\10\bin\x86;C:\Program Files (x86)\Windows Kits\10\bin\10.0.14393.0\x86;C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\\MSBuild\15.0\bin;C:\Windows\Microsoft.NET\Framework\v4.0.30319;C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\IDE\;C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\Tools\;%PATH%
Set UCRTVersion=10.0.14393.0
Set UniversalCRTSdkDir=C:\Program Files (x86)\Windows Kits\10\
Set VCIDEInstallDir=C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\IDE\VC\
Set VCINSTALLDIR=C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\VC\
Set VisualStudioVersion=15.0
Set VSCMD_ARG_app_plat=Desktop
Set VSCMD_ARG_HOST_ARCH=x86
Set VSCMD_ARG_TGT_ARCH=x86
Set VSCMD_VER=15.0.26206.0
set VS150COMNTOOLS=C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\Tools
set VSSDK150Install=C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\VSSDK
Set VSINSTALLDIR=C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\
Set WindowsLibPath=C:\Program Files (x86)\Windows Kits\10\UnionMetadata;C:\Program Files (x86)\Windows Kits\10\References
Set WindowsSdkBinPath=C:\Program Files (x86)\Windows Kits\10\bin\
Set WindowsSdkDir=C:\Program Files (x86)\Windows Kits\10\
Set WindowsSDKLibVersion=10.0.14393.0\
Set WindowsSdkVerBinPath=C:\Program Files (x86)\Windows Kits\10\bin\10.0.14393.0\
Set WindowsSDKVersion=10.0.14393.0\
Set WindowsSDK_ExecutablePath_x64=C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\x64\
Set WindowsSDK_ExecutablePath_x86=C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\
Set __DOTNET_ADD_32BIT=1
Set __DOTNET_PREFERRED_BITNESS=32

:: set location to the build root
cd %1

msbuild buildtools\build.vs2017.proj /p:Configuration=%2

:: reset the env
endlocal

goto exit

:error
echo Missing parameter: vs2017buildshell build-root-folder configuration

:exit
exit %lasterrorcode%


# escape=`
# https://docs.microsoft.com/en-us/visualstudio/install/build-tools-container

# Use the latest Windows Server Core image with .NET Framework 4.7.2.
FROM microsoft/dotnet-framework:4.7.2-runtime

SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

# Install Build Tools excluding workloads and components with known issues.
# SEE https://docs.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-build-tools
# SEE https://docs.microsoft.com/en-us/visualstudio/install/use-command-line-parameters-to-install-visual-studio
RUN $buildToolVersions = 15,16; `
    foreach($buildTool in $buildToolVersions) { `
        Invoke-WebRequest -UseBasicParsing https://aka.ms/vs/$buildTool/release/vs_buildtools.exe -OutFile vs_buildtools.exe; `
        setx /M DOTNET_SKIP_FIRST_TIME_EXPERIENCE 1; `
        Start-Process vs_buildtools.exe -ArgumentList `
        '--wait', '--quiet', '--norestart', '--nocache', `
        '--installPath', "C:\BuildTools$buildTool", `
        '--add', 'Microsoft.VisualStudio.Component.NuGet', `
        '--add', 'Microsoft.VisualStudio.Workload.VisualStudioExtensionBuildTools', `
        '--add', 'Microsoft.Net.Component.4.7.2.SDK', `
        # Microsoft.Net.Component.4.5.TargetingPack might only be necessary for the deploy tool projects
        '--add', 'Microsoft.Net.Component.4.5.TargetingPack' `
        -NoNewWindow -Wait; `
        Remove-Item -Force vs_buildtools.exe; `
    } `
    Remove-Item -Force -Recurse \"${Env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\"; `
    Remove-Item -Force -Recurse ${Env:TEMP}\*

ENV ChocolateyUseWindowsCompression false
RUN Invoke-WebRequest https://chocolatey.org/install.ps1 -UseBasicParsing | Invoke-Expression
# chocolatey free doesn't support this...
RUN [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; `
    echo "[Setup]`nUseCredentialManager=Disabled" > git.inf; `
    Invoke-WebRequest https://github.com/git-for-windows/git/releases/download/v2.21.0.windows.1/Git-2.21.0-64-bit.exe -OutFile git.exe; `
    Start-Process git.exe -ArgumentList `
    '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/NOCANCEL', `
    '/LOADINF=git.inf' `
    -NoNewWindow -Wait;
RUN choco install -fy awscli --version 1.16.140
RUN [Environment]::SetEnvironmentVariable('Path', $env:Path + ';C:\\Program Files\\Amazon\\AWSCLI;C:\\Program Files\\Git\\bin', [EnvironmentVariableTarget]::Machine)

# Restore the default Windows shell for correct batch processing.
SHELL ["cmd", "/S", "/C"]

# Start developer command prompt with any other commands specified. This is ignored by CodeBuild
ENTRYPOINT C:\BuildTools15\Common7\Tools\VsDevCmd.bat &&

# Default to PowerShell if no other command specified.
CMD ["powershell.exe", "-NoLogo", "-ExecutionPolicy", "Bypass"]

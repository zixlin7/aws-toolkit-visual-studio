# Contributing Guidelines

Thank you for your interest in contributing to our project. Whether it's a bug report, new feature, correction, or additional
documentation, we greatly value feedback and contributions from our community.

Please read through this document before submitting any issues or pull requests to ensure we have all the necessary
information to effectively respond to your bug report or contribution.

## Getting Started

You might be interested in reading about the Toolkit's [architecture](./designs/toolkit-architecture/README.md). Additional reference material related to developing Visual Studio extensions can be found in the [Reference](#reference) section of this page.

### Setup

Before you start, you will need the following:

- Visual Studio (2019 or 2022)
  - The following workloads need to be installed:
    - .NET desktop development
    - ASP.NET and web development
    - Visual Studio extension development
  - IDE Configuration adjustments:
    - Adhere to the repo's formatting conventions: **Tools** > **Options** > **Text Editor** > **General** > Check **Follow project coding conventions** ([Reference](https://docs.microsoft.com/en-us/visualstudio/ide/create-portable-custom-editor-options?view=vs-2017#troubleshoot-editorconfig-settings))

### Build

- Open a Developer Command Prompt for VS 2019 or 2022
- change directories to the repo root
- Typical command: `msbuild buildtools\build.proj /t:restore;compile;test`
- More comprehensive rebuild: `msbuild buildtools\build.proj /t:build-tools;clean;build-vstoolkit`
- Other handy build targets:
  - `test`: Runs the main suite of unit/component tests using the currently compiled code
  - `test-integ`: Runs the suite of integration tests using the currently compiled code
  - both of these test targets will run tests relevant to the VS version of the developer command prompt

### Debug

#### Visual Studio

- Open the Toolkit solution in Visual Studio
  - The main solution is `/solutions/AWSVisualStudioToolkit.sln`. This contains projects and tests for all supported major versions of Visual Studio.
  - If you are primarily developing in one version of Visual Studio, you can load a [filtered solution](https://docs.microsoft.com/en-us/visualstudio/ide/filtered-solutions) instead. Look for the `.slnf` files in [solutions](./solutions/)
- Locate the project **AWSToolkitPackage** (**AWSToolkitPackage.v17** for VS 2022)
  - Right click -> Set as StartUp Project
- You can now debug the toolkit

### Writing Tests

This codebase uses [xUnit](https://xunit.net/) for testing. The codebase has a very small collection of tests; you are encouraged to help grow test coverage by including tests with your changes.

At this time, unit and component-level tests are available. UI Tests have not been implemented.

Tests are run as part of the build steps in msbuild. You can also run tests from within Visual Studio by using the Test Explorer.

#### Test coverage for code leveraging the VS SDK

Toolkit code that uses the VS SDK requires different versions of the SDK for each major version of Visual Studio. There will generally be two different versions in play at a given time - for example in July 2022, SDKs supporting VS 2019 (v16) and VS 2022 (v17) are in use. This Toolkit code should be exercised with each version of the VS SDK that the Toolkit could be running with.

Tests covering code (and projects) using the VS SDK must reside in shared projects, that are referenced by VS-version-specific test projects. This allows us to run the code under test with the same conditions and references as the Toolkit. It also allows us to iterate more quickly during local development using a single version of Visual Studio, by allowing us to filter out projects (and tests) by version.

See tests projects listed under [toolkit-architecture](./designs/toolkit-architecture/README.md#projects-and-assemblies) for a high level description of the current test project arrangements.

### Adding Metrics

Instructions for how to prototype and develop metrics specific to this Toolkit can be found on the [Toolkit Common repo](https://github.com/aws/aws-toolkit-common/tree/master/telemetry).

### Overriding Hosted Files

To locally test hosted files changes, copy the hostedfiles folder to a temporary location locally. In the Visual Studio options (_Tools_ -> _Options_), go to the _AWS Toolkit_ section, set the _Toolkit Metadata_ option to use the local filesystem, and fill in your temporary local hostedfiles location. Remember to change this option back to _Default_ once you've finished iterating.

### Known Issues

- In VS 2019, you might get an error around `IAsyncQuickInfoBroker` or `Microsoft.VisualStudio.Language`. The above issue may prevent VS from fully downloading NuGet packages. If this happens, open a VS 2019 Developer Command Prompt and run `msbuild buildtools\build.proj /t:restore` to download the NuGet packages.

## Hosted Files

"Hosted Files" refers to a bunch of different files that are hosted on S3, and that the Toolkit retrieves at runtime from S3 and/or CloudFront endpoints. This allows the Toolkit to retrieve updated manifests and content without releasing a new Toolkit version. These files are sourced from [the hostedfiles folder](hostedfiles).

It is critical that we are mindful that past and present versions of the Toolkit are in use and accessing these files. We do not want to cause crashes or otherwise impact the stability of those Toolkits.

- Backwards compatibility must be considered when changing existing files
- Once published, files likely cannot be deleted, unless we know that all versions of the code that is/was accessing it will successfully handle its absence.
- When adding new files, consider the long term impacts of maintaining these files (as listed above).

### Deprecated Hosted Files

The following files are no longer referenced by the Toolkit, but were used in previous Toolkit releases. Care must be taken to not remove the following files from the repo, because older versions of the Toolkit that are still in use try to download these files and may crash if these files are not found.

- AccountTypes.xml
  - The changes to support MFA and SSO credentials in 2021 eliminated the use of this file. The Toolkit now uses Partition/Region details from the Credentials profile, previously users had to indicate if their account was based in the China or GovCloud partitions.
- ServiceEndPoints.xml
  - This file is published into the hosting location through internal pipelines. It is being listed here for completeness, but this file should not exist in the repo.
  - The changes to support MFA and SSO credentials in 2021 migrated the Toolkit towards endpoints.json as the successor to this file.
- hostedfiles\\flags\\*.png
  - Older versions of the Toolkit used to show a flag next to each region in region selection UIs. The code to show flags was removed in 2019. While unused, flag images were still embedded into the DLL, and this was removed in 2022.

## AWS SDK Package References

The AWS .NET SDK’s NuGet packages are referenced from multiple locations across to repo. To help keep the referenced versions in alignment, an msbuild task automates the process of updating the package references. The task is called `update-awssdk` and resides in `build.proj`.

## Plugins

Plugins are a way to provide the Toolkit with functionality for an AWS Service. Plugins are commonly used to add nodes into the AWS Explorer, and to provide service abstractions.

When creating plugins, or any new projects, try to do as much as possible through Visual Studio itself rather than directly editing files in an editor to reduce the likelihood of unintended side-effects.

1. In the solution folder ToolkitCore\PluginInterfaces, create a new Class Library (.NET Framework) as AWSToolkit.[plugin name].Interface in the .\toolkitcore\plugins folder.
1. Remove Class1.cs
1. Edit Properties\AssemblyInfo.cs to strip out everything except AssemblyTitle, ComVisible, and Guid.
1. Save then unload the project and open another recent plugin interface project. Compare the csproj files.  Update the new csproj file to be similar to the existing one.
1. Save and reload the projects.  As a sanity check, compare all tabs of the project properties to look for anything that doesn't appear similar.
1. Set the default namespace to something more inline with the other namespaces of the peer projects.  Something like Amazon.AwsToolkit.MyShinyNewPlugin.  If you're creating a shared project for any reason, open the Properties tool window and change the "Root namespace".

Do the same steps above for the non-interface plugin project in the ToolkitCore\Plugins solution folder.  

NOTE:  As you shouldn't directly reference your plugin project anywhere and only the interface project where needed, you may have to move the project declarations in the AWSVisualStudioToolkit.sln file above the AWSToolkit.csproj declarations to ensure they're built in time to be
packaged in the VSIX.  This will be obvious if you `msbuild buildtools\build.proj /t:build-vstoolkit` and get an error about your plugin/interface dlls not being found.  If you check the Deployment... output path, they will be there, but they weren't there when AWSToolkitPackage*
was being built.

Update the following files with your new plugin projects:

.\vspackages\AWSToolkitPackage.v17\source.extension.vsixmanifest
.\vspackages\manifests\15.0\source.extension.vsixmanifest
.\vspackages\AwsToolkitPackage.Shared\ProjectReferences\Plugins.xml

NOTE: The VSIX Manifest Editor only works when opening the source.extension.vsixmanifest file from within a solution.  If you attempt to open this file through Windows Explorer or the File/Open File... menu item in VS, it will only load in the XML Editor.

To register a plugin with the Toolkit, add the assembly level attribute `PluginActivatorType` and indicate your plugin activator's type. This is typically placed in the plugin's `AssemblyInfo.cs` file. Look at any of the [existing plugins](./toolkitcore/plugins/AWSToolkit.S3/Properties/AssemblyInfo.cs) as an example.

See [toolkit-architecture](./designs/toolkit-architecture/README.md#plugins) for additional details about plugins.

## Publish to AWS 

### Updating the referenced Deploy Tool version

The Publish to AWS experience is powered by the [dotnet deploy tool](https://github.com/aws/aws-dotnet-deploy). We currently hardcode the Toolkit to use a specific vesrion of the Tool, which is downloaded at runtime. A client package ["AWS.Deploy.ServerMode.Client"](https://www.nuget.org/packages/AWS.Deploy.ServerMode.Client/) contains an API and wrapper code that interacts with the deploy tool's server mode. Both of these versions must remain in sync, otherwise the overall experience could fail to compile or could be unstable.

To integrate a new verion of the Deploy Tool:
- releases are posted on https://github.com/aws/aws-dotnet-deploy/releases. Determine the version of the deploy tool to use.
- update [InstallOptionsFactory.cs](/vspackages/Publishing/PublishToAws.Shared/Install/InstallOptionsFactory.cs) (VersionRange) with the new version
- download the corresponding version of the client package from https://www.nuget.org/packages/AWS.Deploy.ServerMode.Client/
- extract the downloaded package (it is a zip file)
- from the extracted contents, copy "lib\netstandard2.0\AWS.Deploy.ServerMode.Client.dll" into [\thirdparty](/thirdparty), overwriting the existing dll
- compile the Toolkit. If there were breaking changes in the API, you may have to made additional changes in order to successfully compile
- run the integration tests. From the Visual Studio test runner, run the tests associated with the `AwsToolkit.Tests.vXX.Integration` project
  - you will need to have a [default profile defined](https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-files.html#cli-configure-files-where), and you will need to have docker desktop running
  - these tests will run for 30+ minutes. They are performing deployments.
- run the Publish to AWS experience, and try things out. Make sure functionality hasn't disappeared or regressed.
  - examples of previous regressions
    - configuration validations fail to appear
    - publishing a project would succeed, but the list of resulting resources would not populate
  - if you have detected a regression
    - if the cause appears to be caused by the Toolkit, work towards a mitigation
    - if the cause appears to be caused by the deploy tool, report it to the deploy tool team
    - create an integration test to catch this scenario moving forward
- look through the deploy tool's release notes. Create Toolkit changelog entries for all customer affecting changes, and word them from the user's perspective, and how they are affected by the change.
- If things look good, you're ready to PR your changes into the main branch.
  - If you found blocking problems or regressions, this release cannot be merged into the main branch. You can abandon this release (and your local changes), or, if you plan to work on other changes that build on this release, it is reasonable to bring these changes into a "vnext" feature branch. A subsequent release of the deploy tool that fixes the identified problems is required before promoting the changes into the main branch.

## Reporting Bugs/Feature Requests

We welcome you to use the GitHub issue tracker to report bugs or suggest features.

When filing an issue, please check [existing open](https://github.com/aws/aws-toolkit-visual-studio-staging/issues), or [recently closed](https://github.com/aws/aws-toolkit-visual-studio-staging/issues?utf8=%E2%9C%93&q=is%3Aissue%20is%3Aclosed%20), issues to make sure somebody else hasn't already
reported the issue. Please try to include as much information as you can. Details like these are incredibly useful:

- A reproducible test case or series of steps
- The version of our code being used
- Any modifications you've made relevant to the bug
- Anything unusual about your environment or deployment

## Contributing via Pull Requests

Contributions via pull requests are much appreciated. Before sending us a pull request, please ensure that:

1. You are working against the latest source on the _main_ branch.
1. You check existing open, and recently merged, pull requests to make sure someone else hasn't addressed the problem already.
1. You open an issue to discuss any significant work - we would hate for your time to be wasted.

To send us a pull request, please:

1. Fork the repository.
1. Modify the source; please focus on the specific change you are contributing. If you also reformat all the code, it will be hard for us to focus on your change.
1. Ensure local tests pass.
1. Commit to your fork using clear commit messages.
1. Once you are done with your change, create a changelog entry with the command `msbuild buildtools\changelog.proj /t:newChange`. Follow the prompts, then commit the changelog item to your fork. Write the change description from a user's perspective rather than as the author of the code change.
1. Send us a pull request, answering any default questions in the pull request interface.
1. Pay attention to any automated CI failures reported in the pull request, and stay involved in the conversation.

GitHub provides additional document on [forking a repository](https://help.github.com/articles/fork-a-repo/) and
[creating a pull request](https://help.github.com/articles/creating-a-pull-request/).

## Finding contributions to work on

Looking at the existing issues is a great way to find something to contribute on. As our projects, by default, use the default GitHub issue labels (enhancement/bug/duplicate/help wanted/invalid/question/wontfix), looking at any ['help wanted'](https://github.com/aws/aws-toolkit-visual-studio-staging/labels/help%20wanted) issues is a great place to start.

## Code of Conduct

This project has adopted the [Amazon Open Source Code of Conduct](https://aws.github.io/code-of-conduct).
For more information see the [Code of Conduct FAQ](https://aws.github.io/code-of-conduct-faq) or contact
opensource-codeofconduct@amazon.com with any additional questions or comments.

## Security issue notifications

If you discover a potential security issue in this project we ask that you notify AWS/Amazon Security via our [vulnerability reporting page](https://aws.amazon.com/security/vulnerability-reporting/). Please do **not** create a public github issue.

## Licensing

See the [LICENSE](https://github.com/aws/aws-toolkit-visual-studio-staging/blob/main/LICENSE) file for our project's licensing. We will ask you to confirm the licensing of your contribution.

We may ask you to sign a [Contributor License Agreement (CLA)](https://en.wikipedia.org/wiki/Contributor_License_Agreement) for larger changes.

<a name="reference"></a>
# Reference

## Developing Visual Studio Extensions

### User Experience (UX) Guidelines

UIs and experiences that match Visual Studio's UX guidance generally feel like they belong in the IDE, and can be more intuitive for users.

The extension has a lot of UIs, which will take time to align with these guidelines. New UIs should make a best effort to adhere to the Visual Studio User Experience guidelines.

* Guideline Hub: https://docs.microsoft.com/en-us/visualstudio/extensibility/ux-guidelines/visual-studio-user-experience-guidelines
* Noteworthy entries:
  * Style and casing for UI Text: https://docs.microsoft.com/en-us/visualstudio/extensibility/ux-guidelines/fonts-and-formatting-for-visual-studio#BKMK_TextStyle
  * Layout and spacing for UI controls: https://docs.microsoft.com/en-us/visualstudio/extensibility/ux-guidelines/layout-for-visual-studio

### Async / Multithreading

Properly leveraging UI and Background threads reduces the chances of negatively impacting the IDE's overall performance and stability.
* Definitive article on the subject: https://devblogs.microsoft.com/premier-developer/asynchronous-and-multithreaded-programming-within-vs-using-the-joinabletaskfactory/
* Additional supporting material: https://github.com/microsoft/vs-threading/blob/main/doc/index.md

### Extension examples and Prior Art

[Mads Kristensen](https://twitter.com/mkristensen) is a PM on the Visual Studio team, and one of (if not) the most [prolific](https://marketplace.visualstudio.com/publishers/MadsKristensen) Visual Studio extension authors. He has a YouTube series [Writing Visual Studio Extensions with Mads](https://www.youtube.com/playlist?list=PLReL099Y5nRdG2n1PrY_tbCsUznoYvqkS), which covers many aspects (and undocumented concepts!) of extension development with the VS SDK. Source code for many of Mads' extensions can be found [on GitHub](https://github.com/madskristensen), which makes for great reference material.

Older Visual Studio extensibility samples can be found at https://github.com/microsoft/VSSDK-Extensibility-Samples , however your mileage may vary.

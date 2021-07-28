# Contributing Guidelines

Thank you for your interest in contributing to our project. Whether it's a bug report, new feature, correction, or additional
documentation, we greatly value feedback and contributions from our community.

Please read through this document before submitting any issues or pull requests to ensure we have all the necessary
information to effectively respond to your bug report or contribution.

## Getting Started

You might be interested in reading about the Toolkit's [architecture](./designs/toolkit-architecture/README.md). Additional reference material related to developing Visual Studio extensions can be found in the [Reference](#reference) section of this page.

### Setup

Before you start, you will need the following:

- Visual Studio 2017 or 2019
  - The following workloads need to be installed:
    - .NET desktop development
    - ASP.NET and web development
    - Visual Studio extension development
  - IDE Configuration adjustments:
    - Adhere to the repo's formatting conventions: **Tools** > **Options** > **Text Editor** > **General** > Check **Follow project coding conventions** ([Reference](https://docs.microsoft.com/en-us/visualstudio/ide/create-portable-custom-editor-options?view=vs-2017#troubleshoot-editorconfig-settings))

### Build

- Open a Developer Command Prompt for VS 2017 or 2019
- change directories to the repo root
- Typical command: `msbuild buildtools\build.proj /t:restore;compile;test`
- More comprehensive rebuild: `msbuild buildtools\build.proj /t:build-tools;clean;build-vstoolkit`
- Other handy build targets:
  - `test`: Runs the main suite of unit/component tests using the currently compiled code
  - `test-integ`: Runs the suite of integration tests using the currently compiled code

### Debug

#### Visual Studio

- Open `/solutions/AWSVisualStudioToolkit.sln` in VS 2017 or 2019
- Locate the project **AWSToolkitPackage**
  - Right click -> Set as StartUp Project
- You can now debug the toolkit

### Writing Tests

This codebase uses [xUnit](https://xunit.net/) for testing. The codebase has a very small collection of tests; you are encouraged to help grow test coverage by including tests with your changes.

At this time, unit and component-level tests are available. UI Tests have not been implemented.

Tests are run as part of the build steps in msbuild. You can also run tests from within Visual Studio by using the Test Explorer.

### Adding Metrics

Instructions for how to prototype and develop metrics specific to this Toolkit can be found on the [Toolkit Common repo](https://github.com/aws/aws-toolkit-common/tree/master/telemetry).

### Overriding Hosted Files

To locally test hosted files changes, copy the hostedfiles folder to a temporary location locally. In the Visual Studio options (_Tools_ -> _Options_), go to the _AWS Toolkit_ section, set the _Toolkit Metadata_ option to use the local filesystem, and fill in your temporary local hostedfiles location. Remember to change this option back to _Default_ once you've finished iterating.

### Known Issues

- After compiling within VS 2017 or 2019, the Output tab will report `Build: 62 succeeded, 0 failed, 0 up-to-date, 0 skipped` but the Error List tab will show four errors with the text "AWSToolkit.CodeCommitTeamExplorer.v16 is not compatible with net46".
  - You can still run and debug the Toolkit
  - This is caused by the CodeCommit integration, which leverages TeamExplorer. Each version of Visual Studio uses a specific version of TeamExplorer, and the version for VS 2019 (TeamExplorer 16) targets .NET 4.7.2. The project responsible for producing the Toolkit VSIX targets .NET 4.6 (required for VS 2017), which results in this error.
- In VS 2019, you might get an error around `IAsyncQuickInfoBroker` or `Microsoft.VisualStudio.Language`. The above issue may prevent VS from fully downloading NuGet packages. If this happens, open a VS 2019 Developer Command Prompt and run `msbuild buildtools\build.proj /t:restore` to download the NuGet packages.

## Hosted Files

"Hosted Files" refers to a bunch of different files that are hosted on S3, and that the Toolkit retrieves at runtime from S3 and/or CloudFront endpoints. This allows the Toolkit to retrieve updated manifests and content without releasing a new Toolkit version. These files are sourced from [the hostedfiles folder](hostedfiles).

It is critical that we are mindful that past and present versions of the Toolkit are in use and accessing these files. We do not want to cause crashes or otherwise impact the stability of those Toolkits.

- Backwards compatibility must be considered when changing existing files
- Once published, files likely cannot be deleted, unless we know that all versions of the code that is/was accessing it will successfully handle its absence.
- When adding new files, consider the long term impacts of maintaining these files (as listed above).

### Deprecated Hosted Files

The following files are no longer referenced by the Toolkit, but were used in previous Toolkit releases. Care must be taken to not remove the following files from the repo:

- AccountTypes.xml
  - The changes to support MFA and SSO credentials in 2021 eliminated the use of this file. The Toolkit now uses Partition/Region details from the Credentials profile, previously users had to indicate if their account was based in the China or GovCloud partitions.
- ServiceEndPoints.xml
  - This file is published into the hosting location through internal pipelines. It is being listed here for completeness, but this file should not exist in the repo.
  - The changes to support MFA and SSO credentials in 2021 migrated the Toolkit towards endpoints.json as the successor to this file.

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

1. You are working against the latest source on the _master_ branch.
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

See the [LICENSE](https://github.com/aws/aws-toolkit-visual-studio-staging/blob/master/LICENSE) file for our project's licensing. We will ask you to confirm the licensing of your contribution.

We may ask you to sign a [Contributor License Agreement (CLA)](https://en.wikipedia.org/wiki/Contributor_License_Agreement) for larger changes.

<a name="reference"></a>
# Reference

## Developing Visual Studio Extensions

### Async / Multithreading

Properly leveraging UI and Background threads reduces the chances of negatively impacting the IDE's overall performance and stability.
* Definitive article on the subject: https://devblogs.microsoft.com/premier-developer/asynchronous-and-multithreaded-programming-within-vs-using-the-joinabletaskfactory/
* Additional supporting material: https://github.com/microsoft/vs-threading/blob/main/doc/index.md

### Extension examples and Prior Art

[Mads Kristensen](https://twitter.com/mkristensen) is a PM on the Visual Studio team, and one of (if not) the most [prolific](https://marketplace.visualstudio.com/publishers/MadsKristensen) Visual Studio extension authors. He has a YouTube series [Writing Visual Studio Extensions with Mads](https://www.youtube.com/playlist?list=PLReL099Y5nRdG2n1PrY_tbCsUznoYvqkS), which covers many aspects (and undocumented concepts!) of extension development with the VS SDK. Source code for many of Mads' extensions can be found [on GitHub](https://github.com/madskristensen), which makes for great reference material.

Older Visual Studio extensibility samples can be found at https://github.com/microsoft/VSSDK-Extensibility-Samples , however your mileage may vary.

# Contributing Guidelines

Thank you for your interest in contributing to our project. Whether it's a bug report, new feature, correction, or additional
documentation, we greatly value feedback and contributions from our community.

Please read through this document before submitting any issues or pull requests to ensure we have all the necessary
information to effectively respond to your bug report or contribution.

## Getting Started

### Setup

Before you start, you will need the following:

-   Visual Studio 2017
    -   The following workloads need to be installed:
        -   .NET desktop development
        -   ASP.NET and web development
        -   Visual Studio extension development
    -   IDE Configuration adjustments:
        -   Adhere to the repo's formatting conventions: **Tools** > **Options** > **Text Editor** > **General** > Check **Follow project coding conventions** ([Reference](https://docs.microsoft.com/en-us/visualstudio/ide/create-portable-custom-editor-options?view=vs-2017#troubleshoot-editorconfig-settings))

### Build

-   Open a Developer Command Prompt for VS 2017
-   change directories to the repo root
-   Typical command: `msbuild buildtools\build.proj /t:restore;compile;test`
-   More comprehensive rebuild: `msbuild buildtools\build.proj /t:build-tools;clean;build-vstoolkit`

### Debug

#### Visual Studio

-   Open `/solutions/AWSVisualStudioToolkit.sln` in VS 2017
-   Locate the project **AWSToolkitPackage**
    -   Right click -> Set as StartUp Project
    -   Right click and open the project Properties
        -   Click on the **Debug** tab
        -   Click on **Start external program** and point this at the Visual Studio application (`devenv.exe`) that you'd like to launch the Toolkit with
            -   example: `<program files>\Microsoft Visual Studio\2017\Professional\Common7\IDE\devenv.exe`
        -   Set the **Command line arguments** to `/rootsuffix Exp`
-   Save and close the project properties
-   You can now debug the toolkit

#### Testing the Toolkit in VS 2019

You'll have to install the toolkit into VS 2019, and attach to it from VS 2017.

-   Compile the toolkit in VS 2017 (above)
-   Go to Deployment\15.0\Debug and install AWSToolkitPackage.vsix into your VS 2019 instance
-   look at the install log to see which folder the extension was installed to. This will be something like C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\Common7\IDE\Extensions\(some-random-string)
-   Launch VS 2019 with the installed toolkit, then Attach your VS 2017 debugger to this devenv.exe process
-   To iterate from here:
    -   close VS 2019
    -   after making code changes and compiling, update dlls from Deployment\15.0\Debug into the folder where the extension was installed
    -   launch VS 2019

If you make changes that affect the installer, you will have to uninstall the toolkit from VS 2019 and reinstall your updated version.

### Writing Tests

This codebase uses [xUnit](https://xunit.net/) for testing. The codebase has a very small collection of tests; you are encouraged to help grow test coverage by including tests with your changes.

At this time, unit and component-level tests are available. UI Tests have not been implemented.

Tests are run as part of the build steps in msbuild. You can also run tests from within Visual Studio by using the Test Explorer.

### Adding Metrics

Instructions for how to prototype and develop metrics specific to this Toolkit can be found on the [Toolkit Common repo](https://github.com/aws/aws-toolkit-common/tree/master/telemetry).

### Overriding Hosted Files

"Hosted Files" refers to a bunch of different files that are hosted on S3, and that the Toolkit retrieves at runtime from S3 and/or CloudFront endpoints. This allows the Toolkit to retrieve updated manifests and content without releasing a new Toolkit version. These files are sourced from [the hostedfiles folder](hostedfiles).

To locally test hosted files changes, copy the hostedfiles folder to a temporary location locally. In the Visual Studio options (_Tools_ -> _Options_), go to the _AWS Toolkit_ section, set the _Toolkit Metadata_ option to use the local filesystem, and fill in your temporary local hostedfiles location. Remember to change this option back to _Default_ once you've finished iterating.

### Known Issues

-   The Toolkit currently does not compile under Visual Studio 2019
-   After compiling within VS 2017, the Output tab will report `Build: 62 succeeded, 0 failed, 0 up-to-date, 0 skipped` but the Error List tab will show four errors with the text "AWSToolkit.CodeCommitTeamExplorer.v16 is not compatible with net46". You can still run and debug the Toolkit from VS2017. The errors are related to VS 2019 specific files to support CodeCommit.

## Reporting Bugs/Feature Requests

We welcome you to use the GitHub issue tracker to report bugs or suggest features.

When filing an issue, please check [existing open](https://github.com/aws/aws-toolkit-visual-studio-staging/issues), or [recently closed](https://github.com/aws/aws-toolkit-visual-studio-staging/issues?utf8=%E2%9C%93&q=is%3Aissue%20is%3Aclosed%20), issues to make sure somebody else hasn't already
reported the issue. Please try to include as much information as you can. Details like these are incredibly useful:

-   A reproducible test case or series of steps
-   The version of our code being used
-   Any modifications you've made relevant to the bug
-   Anything unusual about your environment or deployment

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

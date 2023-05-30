# VS Toolkit Architecture

The AWS Toolkit for Visual Studio is a Visual Studio extension. This document outlines some of the extension’s architecture and dependencies.

This document is not complete, and some of the gaps are made explicit. This document intends to serve as a somewhat useful starting point when working with the repo. Gaps can be filled in when there is a focus on a certain area.

## Overview

The Toolkit can be thought of as a “collection of plugins”. Each service supported by the Toolkit is implemented in its own plugin (`IPluginActivator`). The Toolkit dynamically loads these plugins on startup (`ToolkitFactory::InitializePluginActivators`). Functionality for services is only made available through the Toolkit if its plugin was found and loaded.

The Toolkit is integrated into the IDE, however there is a layer of separation between the Visual Studio SDK (VSSDK) and the Toolkit’s functionality. This is done to minimize the support cost and impact caused by inevitable changes made to Visual Studio over time and versions. Most VSSDK access is performed within the main project `AWSToolkitPackage`. When plugins need to perform Visual Studio specific interactions - like showing a message box or dialog - they use an abstraction represented by `IAWSToolkitShellProvider` (and implemented in `AWSToolkitShellProviderService`). These are commonly accessed via `ToolkitFactory.Instance`.

Here is a high level simplified representation of the Toolkit’s class libraries and dependencies.

![ToolkitArchitecture]

## Plugins

Plugins are defined by implementing `AbstractPluginActivator`. Plugins do the following:

- initialize service specific functionality (where appropriate)
- instantiate service level AWS Explorer nodes
- register event hooks for things like Explorer node context menus
- allow other parts of the Toolkit to query for features abstracted behind interfaces

Plugins are largely independent of each other. To minimize plugin-to-plugin coupling, plugins have a lightweight interface class library, and a larger implementation class library. For example, Lambda functionality resides in `AWSToolkit.Lambda.Interface` and `AWSToolkit.Lambda`.

The interface libraries:

- contain interfaces defining shared functionality
- contain general POCO structures
- no service specific datatypes or implementation (eg nothing that would require references to that service’s SDK packages)

The implementation libraries:

- reference service specific SDK (NuGet) packages
- implement the Toolkit’s functionality for a given service

In places where functionality from one plugin is required in another, interfaces from the interface class libraries are provided by the core toolkit.

Here is a dependency graph of (a subset of) the Toolkit’s assemblies to illustrate its dependencies:

![PackageReferences]

## Main Entrypoints

The Toolkit’s main entrypoint is `AWSToolkitPackage`. This class represents the Visual Studio Extension, and is created by the IDE on startup. `InitializeAsync` is where the Toolkit starts setting itself up, constructing core components like the Credentials Manager, Telemetry, and the AWS Explorer.

Since Visual Studio Extensions are [MEF](https://docs.microsoft.com/en-us/dotnet/framework/mef/)-based, there are additional exported components that are automatically activated. One example is functionality that exposes CodeCommit repos through TeamExplorer. Classes like `InvitationSection` mark themselves as exportable components, which are then activated by the IDE.

## Team Explorer

Team Explorer is a Visual Studio component that provides access to service-based source control repositories. The AWS Toolkit has a Team Explorer integration that allows users to connect to and access their CodeCommit repos within Visual Studio. Each major version of Visual Studio contains its own versioned Team Explorer libraries that must be referenced by the extension. Since the Toolkit supports more than one major version of Visual Studio, there are identical projects (for example `AWSToolkit.CodeCommitTeamExplorer.v15` for Toolkits running on VS 2017 and `AWSToolkit.CodeCommitTeamExplorer.v16` for Toolkits running on VS 2019). These projects reference the same source code, but are configured to reference their respective versions (and target their respective .NET Framework versions). All of these projects are bundled into the same extension, and Team Explorer libraries are not available as NuGet packages, therefore a system building the Toolkit must have access to all supported versions of Visual Studio.

More should be written about Team Explorer components in this space. See `InvitationSection` for an example of a Team Explorer entrypoint in the Toolkit.

## AWS Explorer and Accounts

The AWS Explorer is implemented in `NavigatorControl` and lets users select a Credentials - Region pairing. Resources are shown from the selected region, for the account corresponding to the selected credentials. `AccountViewModel` is a representation of credentials, and is how much of the Toolkit knows which account to perform operations against.

The data backing the resource explorer tree is a hierarchical collection of Model classes. Service and Resource explorer nodes are served by classes deriving from `ServiceRootViewModel` and `ServiceRootViewMetaNode`.

ServiceRootViewModel:

- top level nodes in the AWS Explorer
- Example: `LambdaRootViewModel` represents the “AWS Lambda” explorer node, whose children represent single Lambda Functions in an account.

ServiceRootViewMetaNode:

- created by the Toolkit Plugin Activators (see Plugins section)
- describe aspects of ServiceRootViewModel, like whether or not a service is supported in a region
- responsible for creating ServiceRootViewModel objects
- contains event hooks, like context menu handlers
- Example: LambdaRootViewMetaNode describes the “AWS Lambda” explorer node, and adds the “Create new Function” item into the node’s context menu

## AWS SDK Package References

See [Contributing Guide](https://github.com/aws/aws-toolkit-visual-studio-staging/blob/main/CONTRIBUTING.md#aws-sdk-package-references).

## Projects and Assemblies

Here is a brief summary of the projects contained within the Toolkit Solution:

- AWSDeploymentLib
  - Code that used to be shared with a now-deprecated, third party tool that helps customers deploy Beanstalk (.NET Framework deployments only) and CloudFormation; partially used by Toolkit today
- AmazonCLIExtensions
  - Code that is copied (and manually kept in sync) with the AWS .NET SDK team's dotnet cli tools - https://github.com/aws/aws-extensions-for-dotnet-cli - it is used to deploy Lambda, Beanstalk (.NET Core deployments only), and ECS.
- AWSSDK.Extensions
  - Utility methods to complement the AWS SDK Clients
- AWSToolkit.Util
  - Most central common code
  - Common Toolkit systems (Telemetry, Region Management, Settings, ...)
  - Does not contain any UI code
  - contains "HostedFiles" contents
- AWSToolkit
  - Second-most central common code
  - Common Toolkit components (Navigator, Accounts, Wizards)
  - Contains some UI code
- AWSToolkit.BindingPath.Resources
  - BindingPathAssemblies folder is added to VS binding path via ProvideBindingPath on AWSToolkitPackage; assemblies here will be found when VS probes for assemblies
  - The CodeCatalyst icon so that VS can find it when displaying the Git Clone dialog
  - Resources with similar needs may be added to this project or additional projects can be added to this folder if needed
- Plugins

  - AWSToolkit.ZZZ.Interface
  - AWSToolkit.ZZZ
  - Where “ZZZ” are: CloudFormation, CodeCommit, DynamoDB, ... , SQS

- AWSToolkit.PolicyEditor
  - Graphical IAM Policy editor
- TeamExplorer extensions for CodeCommit
  - AWSToolkit.CodeCommitTeamExplorer.v15
    - Supports VS 2017
  - AWSToolkit.CodeCommitTeamExplorer.v16
    - Supports VS 2019
- AWSToolkitPackage
  - The Toolkit's main entrypoint; contains most VSSDK interactions
- AwsToolkitPackage.Shared
  - Contains the code and resources that AWSToolkitPackage projects use as a symbolic link
  - This is a [shared project](https://docs.microsoft.com/en-us/xamarin/cross-platform/app-fundamentals/shared-projects?tabs=windows). Projects that reference a shared project act as if they naturally contain the same files and resources.
  - This allows the one codebase to service extensions for multiple major versions of Visual Studio: https://docs.microsoft.com/en-us/visualstudio/extensibility/migration/update-visual-studio-extension?view=vs-2022#use-shared-projects-for-multi-targeting
- CloudFormation specific
  - ???
  - AWSToolkit.CloudFormation.EditorExtensions
  - AWSToolkit.CloudFormation.MSBuildTasks
  - AWSToolkit.CloudFormation.Parser
  - CloudFormationProjectTemplate
  - CloudFormationTemplate
- VS Project Template Related
  - ???
  - AWSToolkit.Lambda.TemplateWizards
  - AWSToolkit.CloudFormation.TemplateWizards
  - LambdaFunctionProjectTemplate
  - MsbuildFunctionTemplate
  - MsbuildFunctionTemplateFSharp
  - MsbuildFunctionWithTestsTemplate
  - MsbuildFunctionWithTestsTemplateFSharp
  - MsbuildServerlessTemplate
  - MsbuildServerlessTemplateFSharp
  - MsbuildServerlessWithTestsTemplate
  - MsbuildServerlessWithTestsTemplateFSharp
  - TemplateWizard
- AwsToolkit.VsSdk.Common
  - Functionality that leverages portions of the VS SDK that is reusable between projects that do access the VS SDK
  - Also contains some older code relating to Visual Studio Extension support, and Solution Explorer integration
- AwsToolkit.VsSdk.Common.Shared
  - Contains the code used by AwsToolkit.VsSdk.Common
  - This is a [shared project](https://docs.microsoft.com/en-us/xamarin/cross-platform/app-fundamentals/shared-projects?tabs=windows). Projects that reference a shared project act as if they naturally contain the same files and resources.
  - This allows the one codebase to service extensions for multiple major versions of Visual Studio: https://docs.microsoft.com/en-us/visualstudio/extensibility/migration/update-visual-studio-extension?view=vs-2022#use-shared-projects-for-multi-targeting
- PublishToAws
  - Responsible for newer "Publish to AWS" functionality
  - Contains all of the implementation except for the commands, which are set up in AWSToolkitPackage
  - Supports VS 2019 and newer (.NET Framework 4.7.2 implementation)
- Tests
  - AmazonCLIExtensions.Tests
    - Tests related to the AmazonCLIExtensions project
  - AWSToolkit.Tests.Common
    - Common testing utilities for any of the testing projects, for code that does not require the VS SDK
  - AWSToolkit.Tests.Common.VS.Shared
    - Common testing utilities that require the VS SDK for any of the testing projects
  - AWSToolkit.Tests
    - Tests related to AWSToolkit, the Toolkit plugins, and supporting code
    - Code and projects under test cannot reference the VSSDK, because this code should be tested against each relevant VS SDK version. Those tests should use AwsToolkit.Vs.vXX.Tests
  - AWSToolkit.Util.Tests
    - Tests related to the AWSToolkit.Util project
  - AwsToolkit.Vs.v16.Tests, AwsToolkit.Vs.v17.Tests
    - Tests related to Tolkit code and projects that leverage the VS SDK
  - AWSToolkitPackage.Tests, AWSToolkitPackage.v17.Tests
    - Tests related to the AWSToolkitPackage project
  - AwsToolkit.Tests.v16.Integration, AwsToolkit.Tests.v17.Integration, AwsToolkit.Tests.Integration.Shared
    - Home of Integration tests for any aspect of the Toolkit
    - Integration tests are run against each different version of the VS SDK used by the Toolkit
  - PublishToAws.Tests, PublishToAws.v17.Tests, PublishToAws.Tests.Shared
    - Tests related to the PublishToAws project

[toolkitarchitecture]: architecture.svg
[packagereferences]: package-references.svg

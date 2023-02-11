## 1.39.0.0 (2023-02-10)

### Changelog
- **Breaking Change** - When connecting to AWS IAM Identity Center (formerly AWS SSO) and AWS Builder ID, it is now necessary to copy a user code from the Toolkit login dialog and paste it into the login page that is opened in the browser. This change helps to ensure that the browser-based login request is associated with actions being performed in the Toolkit.
- **Bug Fix** - Before cloning a CodeCatalyst repository, a message is now written to the Output pane stating that a clone will be attempted.
- **Bug Fix** - Loading CloudFormation template projects no longer displays a "Target framework not supported" dialog that attempts to target .NET Framework 4.0. The Toolkit now requires .NET Framework 4.7.2 in order to handle these projects.
- **Bug Fix** - Fixed issue with Lambda project templates having an invalid region specified in appsettings.Development.json file
- **Bug Fix** - AWS IAM Identity Center (AWS SSO) related credentials profiles that are created using the AWS CLI command `aws configure sso` now appear in the Toolkit.
- **Bug Fix** - When an error is displayed on the Clone CodeCatalyst Repository dialog, only the error message is shown rather than all exception details.
- **Bug Fix** - Improves error message detail on CodeCatalyst cloning failures and ensures the proper Output window pane is displayed to show the error details.
- **Feature** - Publish to AWS has been updated to use v1.10.4 of the AWS .NET deploy tool. This version of the Toolkit includes the following Deploy Tool changes:
  - .NET 6 is now required in order to use the publish experience. .NET Core 3.1 was the previously supported minimum version, but this has reached end of life.
  - Error messaging has been improved when deployments fail
  - Security group validations have been improved when publishing to Fargate
  - See https://github.com/aws/aws-dotnet-deploy/releases for more details.
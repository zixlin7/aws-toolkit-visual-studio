## 1.37.0.0 (2022-11-07)

### Changelog
- **Breaking Change** - The AWS Toolkit now produces log files for each running instance of Visual Studio. This allows concurrent running instances of Visual Studio to have their own unique Toolkit log file. Previously, the first running instance of Visual Studio would append its logs to the same log file. Log files are now produced in `%localappdata%/AWSToolkit/logs/visualstudio/(VisualStudioVersion)`, and are named based on the date and time when Visual Studio was opened.
- **Bug Fix** - Beanstalk platform versions can now be configured when deploying to existing Beanstalk targets with Publish to AWS.
- **Bug Fix** - Push commands have been updated to use `get-login-password` when viewing an ECR Repository
- **Bug Fix** - Fixed issue where Lambda functions could not be published to regions like GovCloud due to "GetFunctionURLConfig does not exist in (region)" error
- **Feature** - The Toolkit's Logs can now be accessed from the "AWS Toolkit" menu, located in the "Extensions" menu.
- **Feature** - Added support for publishing .NET 7 AOT Lambda functions. To use this, use a Custom Runtime Lambda Project targeting .NET 7, and set the project property `PublishAot` to true
- **Feature** - Added local debugging support for .NET 7 Lambda projects using the Lambda Test Tool.
- **Feature** - The "Publish to AWS" experience is now capable of publishing .NET 7 applications to Beanstalk using self contained build. This is necessary for Beanstalk images that do not support .NET 7.
- **Feature** - Publish to AWS has been updated to use v1.6.4 of the AWS .NET deploy tool. This version of the Toolkit includes the following Deploy Tool changes:
  - Add support for deploying .NET 7 applications to Elastic Beanstalk
  - Allow changing Elastic Beanstalk's platform version for environments
  - See https://github.com/aws/aws-dotnet-deploy/releases/tag/1.6.4 for more details.
- **Feature** - Publish to Beanstalk experience now defaults to using self contained deployment bundles for .NET 7 projects. This setting is necessary for Beanstalk images that do not support .NET 7.
- **Removal** - CloudFormation Template cost estimator has been removed.
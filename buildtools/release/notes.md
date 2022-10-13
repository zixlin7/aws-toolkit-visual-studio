## 1.36.0.0 (2022-10-12)

### Changelog
- **Bug Fix** - Configuring a Lambda function to remove all Environment Variables and VPC settings from the Configuration tab of the function's view will now do this. Previously, the Lambda function would be updated, but the function's VPC settings and Environment Variables would not be adjusted.
- **Bug Fix** - Update credentials dialog help to link to the User Guide
- **Bug Fix** - Fixed bug so that default location, if set in Options, is used when creating or cloning CodeCommit repos rather than always defaulting to %USERPROFILE%\Source\Repos.
- **Feature** - The "Publish to AWS" experience is now capable of publishing applications to existing Windows Beanstalk environments
- **Feature** - Publish to AWS has been updated to use v1.5.4 of the AWS .NET deploy tool. This version of the Toolkit includes the following Deploy Tool changes:
  - Applications can now be published to existing Windows Beanstalk environments
  - Applications can now be published to Fargate using internet-facing load balancers
  - BlazorWasm applications can now be published with Http3 support
  - See https://github.com/aws/aws-dotnet-deploy/releases/tag/1.3.7, https://github.com/aws/aws-dotnet-deploy/releases/tag/1.4.10, https://github.com/aws/aws-dotnet-deploy/releases/tag/1.5.4 for more details
- **Feature** - This release of the Toolkit includes an updated signing certificate, which can be seen when installing the extension.
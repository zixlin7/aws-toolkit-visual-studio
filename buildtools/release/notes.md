## 1.44.0.0 (2023-11-14)

### Changelog
- **Bug Fix** - Fix a source of "Unknown error converting pem to ppk file" issues when making SSH connections to EC2 instances
- **Deprecation** - An upcoming release of the AWS Toolkit for Visual Studio 2022 will require a minimum Visual Studio version of 17.7. An info bar has been added to the Toolkit as a reminder - see https://github.com/aws/aws-toolkit-visual-studio/issues/375 for details.
- **Feature** - Added support for Lambda Custom Runtime AL2023 deployment.
- **Feature** - Publish to AWS has been updated to use v1.16.7 of the AWS .NET deploy tool. This version of the Toolkit includes the following Deploy Tool changes:
  - Add .NET8 support for Beanstalk web apps and ECS Fargate console apps
  - Add support for .NET8 container-based web apps
  - Add support for NodeJS 18 in generated dockerfiles
  - Improves on some error messaging
  - Uses an updated CDK bootstrap template
  - See https://github.com/aws/aws-dotnet-deploy/releases/tag/1.12.3, https://github.com/aws/aws-dotnet-deploy/releases/tag/1.13.4, https://github.com/aws/aws-dotnet-deploy/releases/tag/1.14.6, https://github.com/aws/aws-dotnet-deploy/releases/tag/1.15.7 and https://github.com/aws/aws-dotnet-deploy/releases/tag/1.16.7 for more details.
- **Feature** - Added support for deploying Custom Runtime AL2 functions with Arm64 architecture to AWS Lambda.
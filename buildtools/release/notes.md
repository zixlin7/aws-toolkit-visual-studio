## 1.43.0.0 (2023-09-05)

### Changelog
- **Bug Fix** - Improvements have been made to the Lambda publish dialog to differentiate between creating a new function and publishing to an existing function. This fixes an issue where typing a function name could cause the function's configuration to be overwritten with a different function's configuration.
- **Bug Fix** - Switched to signed version of the underlying cli tool used for zipping Lambda function deployment bundles
- **Feature** - AWS Toolkit for Visual Studio is now generally available for Arm64 Visual Studio
- **Feature** - The Getting Started experience has been improved, making it easier to add IAM Identity Center authentication to the AWS Toolkit for Visual Studio.
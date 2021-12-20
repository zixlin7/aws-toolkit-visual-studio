## 1.24.0.0 (2021-12-20)

### Changelog
- **Bug Fix** - Fix issue in the Lambda publish dialog that would prevent it from showing the list of Lambda functions in an account.
- **Bug Fix** - Fix "An update is in progress for resource" failure when publishing NodeJs Lambda functions
- **Bug Fix** - If the new "Publish to AWS" experience has a problem starting up, it attempts to enable the older publishing experience.
- **Bug Fix** - The Toolkit now shows a message if Credentials are not valid when trying to open the new "Publish to AWS" experience.
- **Feature** - Added .NET 6 Lambda container images to the Lambda project blueprints for Visual Studio 2022
- **Feature** - The AWS Toolkit for Visual Studio 2022 is now out of Preview.
- **Feature** - Add support for configuring Lambda Test Tool for .NET 6 projects
- **Feature** - Updated the charts in the Monitoring panel of Beanstalk Environments and CloudFormation stacks. This addresses the "DataVisualization.Charting" error that could happen when trying to view Environments.
- **Feature** - Improved the "Publish to AWS" experience for configuring VPCs and IAM Roles. Existing resources can be selected from a dialog.
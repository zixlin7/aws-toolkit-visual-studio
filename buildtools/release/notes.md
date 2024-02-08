## 1.47.0.0 (2024-02-08)

### Changelog
- **Bug Fix** - Fix "Bucket is not in the same region" validation in Serverless deployment dialog when working with buckets in eu-west-1.
- **Feature** - Publish to Beanstalk experience now defaults to using self contained deployment bundles when publishing .NET 8 projects to Linux images. This setting is necessary for Beanstalk Linux images that do not support .NET 8.
- **Feature** - Added local debugging support for .NET 8 Lambda projects using the Lambda Test Tool.
- **Feature** - The AWS Builder Id and IAM Identity Center (SSO) login flow has been improved to confirm that the device code in the Toolkit and browser match. Previously, the code needed to be copied from the Toolkit and pasted into the browser.
- **Feature** - CodeWhisperer functionality can be fully enabled and disabled from the AWS Toolkit / CodeWhisperer section of the Visual Studio Options dialog.
- **Feature** - Added support for using proxy servers with Amazon CodeWhisperer.
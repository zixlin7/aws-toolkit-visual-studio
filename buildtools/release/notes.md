## 1.27.0.0 (2022-03-23)

### Changelog
- **Bug Fix** - Editing Beanstalk environment values now ignores entries with empty keys in the Publish to AWS experience.
- **Bug Fix** - Fix Publish Container to AWS wizard so that the Schedule Rule Type is correctly restored as Fixed/Cron with the correct expression.
- **Bug Fix** - Fix showing CloudFormation stack after Publish to AWS completes.
- **Bug Fix** - Fix issue where AWS Explorer button images would not display
- **Deprecation** - An upcoming release will remove support for Visual Studio 2017. An info bar has been added to the Toolkit as a reminder - see https://github.com/aws/aws-toolkit-visual-studio/issues/221 for details.
- **Feature** - The wizard for publishing Lambda projects and Serverless application projects now defaults to the Credentials profile and region from `aws-lambda-tools-defaults.json`. If these values are not found, the current AWS Explorer values are used.
- **Feature** - Added a way to configure the Credentials and region used when publishing with the "Publish to AWS" experience.
- **Feature** - Added menu items to view the '.NET on AWS' and '.NET on AWS Community' websites, available from the "AWS Toolkit" menu, located in the "Extensions" menu ("Tools" menu in Visual Studio 2017)
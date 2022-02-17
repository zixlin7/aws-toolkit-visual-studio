## 1.25.0.0 (2022-02-17)

### Changelog
- **Bug Fix** - The Toolkit Output channel no longer takes focus when credentials profiles are reloaded
- **Bug Fix** - "Publish to AWS" experience now supports new regions like Jakarta
- **Bug Fix** - Fix issue with icons not correctly handling the dark theme in Visual Studio 2022.
- **Bug Fix** - Fixed bug where the "Publish to AWS" experience would not work for users with a space in their Windows profile path
- **Feature** - Beanstalk environment variables can now be configured in the "Publish to AWS" experience
- **Feature** - Added more validation checks to ECS Fargate and App Runner based targets in the Publish to AWS experience
- **Feature** - Menu items for the new (preview) "Publish to AWS" experience and the older publishing experiences are no longer mutually exclusive.
- **Feature** - Improved the "Publish to AWS" experience for configuring EC2 Instance Types by adding a selection dialog.
- **Feature** - The "Publish to AWS" experience is now capable of publishing applications to existing Beanstalk environments
- **Feature** - Added support for credential profiles that use `role_arn` and `credential_source`, where `credential_source = Ec2InstanceMetadata`. This makes it easier to use the Toolkit from within a Windows EC2 instance, since it isn't necessary to place account secrets in the credentials file on the instance.
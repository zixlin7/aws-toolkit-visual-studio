## 1.23.3.0 (2021-11-17)

### Changelog
- **Bug Fix** - Added support for CloudFormation Template projects into the Visual Studio 2022 Toolkit.
- **Feature** - Add option in the Publish to Beanstalk workflow to build self-contained deployment bundles, when working with .NET Core/.NET 5+ applications and Windows images. This enables .NET 6 projects to be published to Windows servers using the Publish to Beanstalk feature.
- **Feature** - Make Publish to Beanstalk experience default to using self contained deployment bundles for .NET 6 projects. This setting is necessary for Beanstalk images that do not contain the .NET 6 runtime.
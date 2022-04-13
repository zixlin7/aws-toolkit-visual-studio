## 1.28.0.0 (2022-04-12)

### Changelog
- **Breaking Change** - From this version on, the Toolkit no longer supports Visual Studio 2017.
- **Bug Fix** - Fix problem where dropdown controls would not open, and UI controls would flicker when shown on secondary displays. This mostly affected the Publish to AWS settings screen.
- **Bug Fix** - Fixes case where the Publish to AWS summary screen was unable to open the CloudFormation stack viewer for a profile/region combination that did not match what is currently selected in the AWS Explorer.
- **Feature** - Add support for publishing .NET 6 applications to Linux based Beanstalk images in the Publish to AWS experience
- **Feature** - The Publish To Beanstalk experience no longer defaults to using self-contained deployment bundles when publishing .NET 6 projects to Linux images.
- **Feature** - Publish to AWS experience can now publish images to ECR Repositories
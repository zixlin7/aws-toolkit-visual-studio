## 1.34.0.0 (2022-06-30)

### Changelog
- **Breaking Change** - Publish to AWS now requires a minimum NodeJs version of 14. Previously, this was 10.
- **Bug Fix** - Fix "Assembly AWSSDK.SSOOIDC could not be found or loaded. This assembly must be available at runtime to use Amazon.Runtime.SSOAWSCredentials" error introduced in Toolkit version 1.32.0.0. This prevented the Toolkit from working with SSO based credentials.
- **Feature** - Publish to AWS is now capable of publishing Elastic Beanstalk running on Windows images
- **Feature** - Publish to AWS has been updated to use v0.50.2 of the AWS .NET deploy tool. This includes https://github.com/aws/aws-dotnet-deploy/releases/tag/0.50.2, https://github.com/aws/aws-dotnet-deploy/releases/tag/0.49.14, and https://github.com/aws/aws-dotnet-deploy/releases/tag/0.48.15
- **Feature** - Add workaround for Publish to AWS when running on a system that has a space in the user profile's path. See https://aws.github.io/aws-dotnet-deploy/docs/getting-started/custom-workspace/ for details
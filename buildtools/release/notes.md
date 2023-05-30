## 1.41.0.0 (2023-05-30)

### Changelog
- **Bug Fix** - Fix issue viewing Beanstalk Environments where the Resources tab would not show details. Health check information is no longer displayed in the load balancer details.
- **Bug Fix** - Entering a local repo path on the CodeCatalyst Clone Repository dialog now validates the path on changes rather than clicking away from the textbox which did not always work consistently depending on where the user clicked.
- **Bug Fix** - Clone CodeCatalyst Repository dialog now only lists repositories that are hosted on CodeCatalyst
- **Bug Fix** - Fix "Unable to get IAM security credentials from EC2 Instance Metadata Service." error when trying to clone CodeCatalyst repos on a system that does not contain a default credentials profile.
- **Bug Fix** - Fixes issue where the AWS Explorer would sometimes show both the connection status and the resource tree at once.
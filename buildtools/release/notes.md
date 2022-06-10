## 1.31.0.0 (2022-06-07)

### Changelog
- **Bug Fix** - Fix scenario in Publish to AWS where invalid setting values would revert to a previous valid value while the settings were still being edited.
- **Feature** - Adds a Browse button to the Publish to AWS targets that allow for Dockerfile paths. This opens a file dialog to allow the user to browse to and select the Dockerfile to be used in the deployment.
- **Feature** - Add Publish to AWS validation checks for scenarios where a deployment could conflict with existing account resources.
- **Feature** - Add Publish to AWS validations to check that references to existing IAM Roles are not left empty
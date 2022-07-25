## 1.35.1.0 (2022-07-25)

### Changelog
- **Bug Fix** - CodeArtifact NuGet credential provider has been improved to reduce the amount of times it falls back to a standard basic authentication dialog.
- **Bug Fix** - Fixes bug that attempted to set ACLs on S3 buckets that don't support ACLs.  This was seen in the S3 object browser as an error message from operations such as rename and copy/paste."
- **Bug Fix** - Fix Lambda project template not detecting Node.js tools properly and displaying error message.
- **Bug Fix** - Display error message in Publish to AWS when the Deploy Tool fails to load configuration for a target. This used to leave the Publish button enabed, but it would not do anything.
- **Bug Fix** - Fix bug where AWS Explorer would cause multiple AWS MFA and AWS SSO Login prompts to appear, even when cancelled
- **Bug Fix** - Fixes bug where uploading a file to the root of an S3 Bucket actually uploads to a no-name folder at root.
- **Bug Fix** - Fix issue where downloading a CloudWatch log stream would occasionally never complete.
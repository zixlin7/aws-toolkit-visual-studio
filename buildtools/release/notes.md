## 1.29.0.0 (2022-05-16)

### Changelog
- **Bug Fix** - Fix DynamoDB Local connection failure when using a non-basic default credentials profile (SSO or MFA for example)
- **Bug Fix** - Configuring the Lambda publish wizard to remove all VPC subnets and security groups from an existing Lambda will now do this when publishing to Lambda. Previously, the Lambda function would be updated, but the function's VPC settings would not be adjusted.
- **Feature** - The Publish to AWS experience now indicates if the Application Name is not valid, and prevents a deployment from starting.
- **Feature** - Add support for configuring VPC Connectors when publishing to App Runner through the Publish to AWS experience.
- **Feature** - When configuring roles in the Publish to AWS experience, if the publish target requires a service principal, the role selection dialog will only show roles that reference the service principal.
# Publish Integration Test

This tests the integration between the toolkit and the Deploy CLI for the new Publish to AWS experience.
## Setup Credentials
You will need an AWS credentials profile under the name "default". 


If you do not already have one, you can do this using the toolkit [link to documentation](https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/keys-profiles-credentials.html)

(Note: this will temporarily deploy resources to the configured AWS account)

## Troubleshooting
There is a possible race condition where the CloudFormation stack will not delete successfully at the end of the test due to automation that may attach a policy to one of the stack's IAM Roles.

To remedy this, manually delete the IAM role from the AWS Console and then reattempt to delete the CloudFormation stack.

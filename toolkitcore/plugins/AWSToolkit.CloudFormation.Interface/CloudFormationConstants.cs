namespace Amazon.AWSToolkit.CloudFormation
{
    public static class CloudFormationConstants
    {

        public const string CreateInProgressStatus = "CREATE_IN_PROGRESS";
        public const string CreateFailedStatus = "CREATE_FAILED";
        public const string CreateCompleteStatus = "CREATE_COMPLETE";

        public const string RollbackInProgressStatus = "ROLLBACK_IN_PROGRESS";
        public const string RollbackCompleteStatus = "ROLLBACK_COMPLETE";
        public const string RollbackFailedStatus = "ROLLBACK_FAILED";

        public const string DeleteFailedStatus = "DELETE_FAILED";
        public const string DeleteCompleteStatus = "DELETE_COMPLETE";
        public const string DeleteInProgressStatus = "DELETE_IN_PROGRESS";

        public const string UpdateCompleteStatus = "UPDATE_COMPLETE";
        public const string UpdateInProgressStatus = "UPDATE_IN_PROGRESS";
        public const string UpdateCompleteCleanupInProgressStatus = "UPDATE_COMPLETE_CLEANUP_IN_PROGRESS";
        public const string UpdateRollbackInProgressStatus = "UPDATE_ROLLBACK_IN_PROGRESS";
        public const string UpdateRollbackFailedStatus = "UPDATE_ROLLBACK_FAILED";
        public const string UpdateRollbackCompleteCleanupInProgressStatus = "UPDATE_ROLLBACK_COMPLETE_CLEANUP_IN_PROGRESS";
        public const string UpdateRollbackCompleteStatus = "UPDATE_ROLLBACK_COMPLETE";

        public const string AWS_SERVERLESS_TAG = "AWSServerlessAppNETCore";

        public const string RESOURCE_TYPE_REST_API = "AWS::ApiGateway::RestApi";
        public const string RESOURCE_TYPE_STAGE_API = "AWS::ApiGateway::Stage";

        public static bool IsUpdateableStatus(string status)
        {
            switch (status)
            {
                case CreateCompleteStatus:
                case UpdateCompleteStatus:
                case UpdateCompleteCleanupInProgressStatus:
                case UpdateRollbackCompleteCleanupInProgressStatus:
                case UpdateRollbackCompleteStatus:
                    return true;
            }

            return false;
        }

        public const string VSToolkitDeployedOuputParam = "VSToolkitDeployed";

        // param keys used when inspecting for existing deployment targets
        public const string DeploymentTargetQueryParam_StackName = "StackName";

        public const string NO_ECHO_VALUE_RETURN_FROM_CONSOLE = "******";
    }
}

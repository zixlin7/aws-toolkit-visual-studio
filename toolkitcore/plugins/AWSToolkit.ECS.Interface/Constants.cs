namespace Amazon.AWSToolkit.ECS
{

    public static class Constants
    {
        public enum DeployMode { PushOnly, DeployService, ScheduleTask, RunTask }

        public const string WIZARD_CREATE_TAG_KEY = "ToolCreatedFrom";
        public const string WIZARD_CREATE_TAG_VALUE = "awsVSToolkit";



    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amazon.AWSToolkit.ECS
{

    public static class Constants
    {
        public enum DeployMode { PushOnly, DeployToECSCluster, ScheduleTask, RunTask }

        public const string WIZARD_CREATE_TAG_KEY = "ToolCreatedFrom";
        public const string WIZARD_CREATE_TAG_VALUE = "awsVSToolkit";



    }
}

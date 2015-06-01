using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ThirdParty.Json.LitJson;
using AWSDeploymentHostManager.Tasks;

using log4net;

namespace AWSDeploymentHostManager
{
    public class TaskFactory
    {
        private IDictionary<string, Type> tasks = new Dictionary<string, Type>();

        private const string
            JSON_KEY_PARAMS = "parameters",
            JSON_KEY_NAME = "name";

        public void RegisterTask(string taskName, Type taskType)
        {
            if (taskType.IsSubclassOf(typeof(Task)))
            {
                tasks.Add(taskName, taskType);
            }
        }

        public Task CreateTaskFromRequest(string json)
        {
            JsonData jData = JsonMapper.ToObject(json);
            string taskName = (string)jData[JSON_KEY_NAME];
            Task task;

            if (!tasks.ContainsKey(taskName))
            {
                task = new UnknownTask(taskName);
            }
            else
            {
                task = (Task)tasks[taskName].GetConstructor(System.Type.EmptyTypes).Invoke(new Object[0]);
            }
            JsonData jParams = jData[JSON_KEY_PARAMS] as JsonData;

            if (jParams != null)
            {
                if (jParams.IsArray || jParams.IsObject)
                {
                    foreach (KeyValuePair<string, JsonData> param in jParams)
                    {
                        if (param.Value.IsString)
                        {
                            task.SetParameter(param.Key, (string)param.Value);
                            HostManager.LOGGER.Info(String.Format("Complex parameter {0}:{1} sent", param.Key, (string)param.Value));
                            Event.LogInfo("HostManager", String.Format("Complex parameter {0}:{1} sent", param.Key, (string)param.Value));
                        }
                        else
                        {
                            HostManager.LOGGER.Warn(String.Format("Complex parameter {0} has incorrect type", param.Key));
                            Event.LogWarn("HostManager", String.Format("Complex parameter {0} has incorrect type", param.Key));
                        }
                    }
                }
                else
                {
                    HostManager.LOGGER.Warn(String.Format("Parameters for task {0} has incorrect type", taskName));
                    Event.LogWarn("HostManager", String.Format("Parameters for task {0} has incorrect type", taskName));
                }
            }

            return task;
        }
    }
}

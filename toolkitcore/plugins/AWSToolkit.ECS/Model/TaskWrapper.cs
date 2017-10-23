using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.ECS.Model;

using Task = Amazon.ECS.Model.Task;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.ECS.Model
{
    public class TaskWrapper
    {
        Task _nativeTask;

        public TaskWrapper(Task nativeTask)
        {
            this._nativeTask = nativeTask;
        }

        public Task NativeTask
        {
            get { return this._nativeTask; }
        }

        public string TaskId
        {
            get
            {
                var name = this._nativeTask.TaskArn.Substring(this._nativeTask.TaskArn.LastIndexOf('/') + 1);
                return name;
            }
        }

        public string TaskDefinition
        {
            get
            {
                var name = this._nativeTask.TaskDefinitionArn.Substring(this._nativeTask.TaskDefinitionArn.LastIndexOf('/') + 1);
                return name;
            }
        }

        public string StartedAt
        {
            get
            {
                if (this._nativeTask.StartedAt == DateTime.MinValue)
                    return null;

                return this._nativeTask.StartedAt.ToString();
            }
        }

        public string StoppedAt
        {
            get
            {
                if (this._nativeTask.StoppedAt == DateTime.MinValue)
                    return null;

                return this._nativeTask.StoppedAt.ToString();
            }
        }
    }
}

using System;
using System.Text;
using Task = Amazon.ECS.Model.Task;
using Amazon.AWSToolkit.CommonUI;
using System.Windows;

namespace Amazon.AWSToolkit.ECS.Model
{
    public class TaskWrapper : PropertiesModel
    {
        Task _nativeTask;

        public TaskWrapper(Task nativeTask)
        {
            this._nativeTask = nativeTask;
        }

        public Task NativeTask => this._nativeTask;

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

        public string LaunchType => this._nativeTask.LaunchType;

        public string StartedAt
        {
            get
            {
                if (this._nativeTask.StartedAt == DateTime.MinValue)
                    return null;

                return this._nativeTask.StartedAt.ToLocalTime().ToString();
            }
        }

        public string StoppedAt
        {
            get
            {
                if (this._nativeTask.StoppedAt == DateTime.MinValue)
                    return null;

                return this._nativeTask.StoppedAt.ToLocalTime().ToString();
            }
        }

        public string CombinedStoppedReason
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                if (!string.Equals(this._nativeTask.StoppedReason, "Essential container in task exited"))
                    sb.Append(this._nativeTask.StoppedReason);

                foreach(var container in this._nativeTask.Containers)
                {
                    if(!string.IsNullOrEmpty(container.Reason))
                    {
                        if (sb.Length > 0) sb.AppendLine("");
                        sb.Append(container.Reason);
                    }
                }

                return sb.ToString();
            }
        }

        public string FormattedAttachments
        {
            get
            {
                var sb = new StringBuilder();

                foreach(var attachment in this.NativeTask.Attachments)
                {
                    foreach(var detail in attachment.Details)
                    {
                        sb.AppendLine(detail.Name + ": " + detail.Value);
                    }
                }

                if (!string.IsNullOrEmpty(this._publicIp))
                    sb.AppendLine("publicIPv4Address: " + this._publicIp);
                if (!string.IsNullOrEmpty(this._publicDns))
                    sb.AppendLine("publicDNS: " + this._publicDns);

                return sb.ToString();
            }
        }

        string _publicIp;
        string _publicDns;
        public void AddNetworkInterfaceInfo(string publicIp, string publicDns)
        {
            this._publicIp = publicIp;
            this._publicDns = publicDns;
            base.NotifyPropertyChanged("FormattedAttachments");
        }



        public string NetworkInterfaceId
        {
            get
            {
                foreach(var attachment in this.NativeTask.Attachments)
                {
                    foreach(var detail in attachment.Details)
                    {
                        if (string.Equals("networkInterfaceId", detail.Name, StringComparison.OrdinalIgnoreCase))
                            return detail.Value;
                    }
                }

                return null;
            }
        }


        public Visibility StoppedVisibility => string.Equals(this._nativeTask.DesiredStatus, "STOPPED", StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;

        public Visibility RunningVisibility => this.StoppedVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

        public override void GetPropertyNames(out string className, out string componentName)
        {
            className = "Task";
            componentName = this.TaskId;
        }
    }
}

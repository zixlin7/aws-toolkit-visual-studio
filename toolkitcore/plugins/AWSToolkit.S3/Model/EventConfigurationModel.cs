using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using Amazon.S3.Model;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.S3.Model
{
    public class EventConfigurationModel : BaseModel, ICloneable
    {
        public const string SNS_FRIENDLY_NAME = "SNS";
        public const string SQS_FRIENDLY_NAME = "SQS";
        public const string LAMBDA_FRIENDLY_NAME = "Lambda";


        public enum Service {SNS, SQS, Lambda};

        public EventConfigurationModel()
        {
        }

        private string _id;
        public string Id
        {
            get { return this._id; }
            set 
            { 
                this._id = value;
                base.NotifyPropertyChanged("Id");
            }
        }

        Service _service;
        public Service TargetService
        {
            get { return this._service; }
            set
            {
                this._service = value;
                base.NotifyPropertyChanged("TargetService");
                base.NotifyPropertyChanged("ServiceName");
                base.NotifyPropertyChanged("ServiceIcon");
            }
        }

        public string ServiceName
        {
            get
            {
                switch (this.TargetService)
                {
                    case Service.SQS:
                        return SQS_FRIENDLY_NAME;
                    case Service.SNS:
                        return SNS_FRIENDLY_NAME;
                    case Service.Lambda:
                        return LAMBDA_FRIENDLY_NAME;
                    default:
                        return "Unknown";
                }
            }
        }

        string _resourceArn;
        public string ResourceArn
        {
            get { return this._resourceArn; }
            set
            {
                this._resourceArn = value;
                base.NotifyPropertyChanged("ResourceArn");
                base.NotifyPropertyChanged("FormattedResourceName");
            }
        }

        string _prefix;
        public string Prefix
        {
            get { return this._prefix; }
            set
            {
                this._prefix = value;
                base.NotifyPropertyChanged("Prefix");
            }
        }

        string _suffix;
        public string Suffix
        {
            get { return this._suffix; }
            set
            {
                this._suffix = value;
                base.NotifyPropertyChanged("Suffix");
            }
        }

        string _invocationRoleArn;
        public string InvocationRoleArn
        {
            get { return this._invocationRoleArn; }
            set
            {
                this._invocationRoleArn = value;
                base.NotifyPropertyChanged("InvocationRole");
                base.NotifyPropertyChanged("FormattedResourceName");
            }
        }

        public string FormattedResourceName
        {
            get
            {

                if (string.IsNullOrEmpty(this.ResourceArn))
                    return null;

                var arnTokens = ResourceArn.Split(':');
                var resourceName = arnTokens[arnTokens.Length - 1];
                if (this.TargetService == Service.Lambda && !string.IsNullOrEmpty(this.InvocationRoleArn))
                {
                    string roleName;
                    int pos = InvocationRoleArn.LastIndexOf('/');
                    if (pos != -1)
                        roleName = InvocationRoleArn.Substring(pos + 1);
                    else
                        roleName = InvocationRoleArn;

                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("{0} (Role: {1})", resourceName, roleName);
                    return sb.ToString();
                }
                else
                {
                    return resourceName;
                }
            }
        }


        ObservableCollection<Amazon.S3.EventType> _eventTypes = new ObservableCollection<Amazon.S3.EventType>();
        public ObservableCollection<Amazon.S3.EventType> EventTypes
        {
            get { return this._eventTypes; }
        }

        public string FormattedEventTypes
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                foreach (var eventType in EventTypes)
                {
                    if (sb.Length > 0)
                        sb.Append(", ");
                    sb.Append(eventType);
                }

                return sb.ToString();
            }
        }

        public string FormattedFilter
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                if (!string.IsNullOrEmpty(this.Prefix))
                    sb.AppendFormat("Prefix={0}", this.Prefix);

                if (!string.IsNullOrEmpty(this.Suffix))
                {
                    if (sb.Length > 0)
                        sb.Append(", ");
                    sb.AppendFormat("Suffix={0}", this.Suffix);
                }


                return sb.ToString();
            }
        }

        public System.Windows.Media.ImageSource ServiceIcon
        {
            get
            {
                switch (this.ServiceName)
                {
                    case SNS_FRIENDLY_NAME:
                        return IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.sns-service-icon.png").Source;
                    case SQS_FRIENDLY_NAME:
                        return IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.sqs-service-icon.png").Source;
                    case LAMBDA_FRIENDLY_NAME:
                        return IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.S3.Resources.EmbeddedImages.lambda-service-icon.png").Source;
                    default:
                        return null;
                }
            }
        }

        public object Clone()
        {
            var newConfig = new EventConfigurationModel()
            {
                Id = this.Id,
                TargetService = this.TargetService,
                ResourceArn = this.ResourceArn,
                InvocationRoleArn = this.InvocationRoleArn
            };

            foreach (var eventType in this.EventTypes)
                newConfig.EventTypes.Add(eventType);

            return newConfig;
        }
    }
}

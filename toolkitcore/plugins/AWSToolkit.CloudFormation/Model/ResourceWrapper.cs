using Amazon.AWSToolkit.CommonUI;
using Amazon.CloudFormation.Model;

namespace Amazon.AWSToolkit.CloudFormation.Model
{
    public class ResourceWrapper : BaseModel
    {
        StackResource _nativeResource;

        public ResourceWrapper(StackResource resource)
        {
            this._nativeResource = resource;
        }

        public string LogicalResourceId
        {
            get => this._nativeResource.LogicalResourceId;
            set
            {
                this._nativeResource.LogicalResourceId = value;
                base.NotifyPropertyChanged("LogicalResourceId");
            }
        }

        public string PhysicalResourceId
        {
            get => this._nativeResource.PhysicalResourceId;
            set
            {
                this._nativeResource.PhysicalResourceId = value;
                base.NotifyPropertyChanged("PhysicalResourceId");
            }
        }

        public string ResourceStatus
        {
            get => this._nativeResource.ResourceStatus;
            set
            {
                this._nativeResource.ResourceStatus = value;
                base.NotifyPropertyChanged("ResourceStatus");
            }
        }

        public string ResourceStatusReason
        {
            get => this._nativeResource.ResourceStatusReason;
            set
            {
                this._nativeResource.ResourceStatusReason = value;
                base.NotifyPropertyChanged("ResourceStatusReason");
            }
        }

        public string ResourceType
        {
            get => this._nativeResource.ResourceType;
            set
            {
                this._nativeResource.ResourceType = value;
                base.NotifyPropertyChanged("ResourceType");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            get { return this._nativeResource.LogicalResourceId; }
            set
            {
                this._nativeResource.LogicalResourceId = value;
                base.NotifyPropertyChanged("LogicalResourceId");
            }
        }

        public string PhysicalResourceId
        {
            get { return this._nativeResource.PhysicalResourceId; }
            set
            {
                this._nativeResource.PhysicalResourceId = value;
                base.NotifyPropertyChanged("PhysicalResourceId");
            }
        }

        public string ResourceStatus
        {
            get { return this._nativeResource.ResourceStatus; }
            set
            {
                this._nativeResource.ResourceStatus = value;
                base.NotifyPropertyChanged("ResourceStatus");
            }
        }

        public string ResourceStatusReason
        {
            get { return this._nativeResource.ResourceStatusReason; }
            set
            {
                this._nativeResource.ResourceStatusReason = value;
                base.NotifyPropertyChanged("ResourceStatusReason");
            }
        }

        public string ResourceType
        {
            get { return this._nativeResource.ResourceType; }
            set
            {
                this._nativeResource.ResourceType = value;
                base.NotifyPropertyChanged("ResourceType");
            }
        }
    }
}

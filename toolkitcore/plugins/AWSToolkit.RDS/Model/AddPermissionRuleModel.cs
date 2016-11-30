using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.RDS.Model
{
    public class AddPermissionRuleModel : BaseModel
    {
        bool _useCidrIP = true;
        public bool UseCidrIP
        {
            get { return this._useCidrIP; }
            set
            {
                this._useCidrIP = value;
                base.NotifyPropertyChanged("UseCidrIP");
            }
        }

        public bool UseEC2SecurityGroup
        {
            get { return !this.UseCidrIP; }
            set
            {
                this.UseCidrIP = !value;
                base.NotifyPropertyChanged("UseEC2SecurityGroup");
            }
        }

        string _cidr;
        public string CIDR
        {
            get { return this._cidr; }
            set
            {
                this._cidr = value;
                base.NotifyPropertyChanged("CIDR");
                base.NotifyPropertyChanged("Details");
            }
        }

        string _awsUser;
        public string AWSUser
        {
            get { return this._awsUser; }
            set
            {
                this._awsUser = value;
                base.NotifyPropertyChanged("AWSUser");
                base.NotifyPropertyChanged("Details");
            }
        }

        string _ec2SecurityGroupName;
        public string EC2SecurityGroupName
        {
            get { return this._ec2SecurityGroupName; }
            set
            {
                this._ec2SecurityGroupName = value;
                base.NotifyPropertyChanged("EC2SecurityGroupName");
                base.NotifyPropertyChanged("Details");
            }
        }

        string _ec2SecurityGroupId;
        public string EC2SecurityGroupId
        {
            get { return this._ec2SecurityGroupId; }
            set
            {
                this._ec2SecurityGroupId = value;
                base.NotifyPropertyChanged("EC2SecurityGroupId");
                base.NotifyPropertyChanged("Details");
            }
        }
    }
}

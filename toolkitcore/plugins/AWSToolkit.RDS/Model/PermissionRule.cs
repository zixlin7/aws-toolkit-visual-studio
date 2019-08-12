using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.RDS.Model
{
    public class PermissionRule : BaseModel
    {
        public PermissionRule(ConnectionType type, string cidr)
        {
            this.Type = type;
            this.CIDR = cidr;
        }

        public PermissionRule(ConnectionType type, string awsUser, string ec2SecurityGroup)
        {
            this.Type = type;
            this.AWSUser = awsUser;
            this.EC2SecurityGroup = ec2SecurityGroup;
        }

        ConnectionType _type = ConnectionType.CIDR;
        public ConnectionType Type
        {
            get => this._type;
            set
            {
                this._type = value;
                base.NotifyPropertyChanged("Type");
                base.NotifyPropertyChanged("Details");
            }
        }

        string _cidr;
        public string CIDR
        {
            get => this._cidr;
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
            get => this._awsUser;
            set
            {
                this._awsUser = value;
                base.NotifyPropertyChanged("AWSUser");
                base.NotifyPropertyChanged("Details");
            }
        }

        string _ec2SecurityGroup;
        public string EC2SecurityGroup
        {
            get => this._ec2SecurityGroup;
            set
            {
                this._ec2SecurityGroup = value;
                base.NotifyPropertyChanged("EC2SecurityGroup");
                base.NotifyPropertyChanged("Details");
            }
        }

        public string Details
        {
            get 
            {
                if (Type == ConnectionType.CIDR)
                    return this.CIDR;
                else
                {
                    if(string.IsNullOrEmpty(this.AWSUser))
                        return string.Format("VPC EC2 Security Group: {0}", this.EC2SecurityGroup); 
                    else
                        return string.Format("AWS User: {0}, EC2 Security Group: {1}", this.AWSUser, this.EC2SecurityGroup); 
                }
            }
        }
        public class ConnectionType
        {
            public static readonly ConnectionType CIDR = new ConnectionType("CIDR/IP");
            public static readonly ConnectionType EC2SecurityGroup = new ConnectionType("EC2 Security Group");

            private ConnectionType(string name)
            {
                this.Name = name;
            }

            public string Name
            {
                get;
            }

            public override string ToString()
            {
                return this.Name;
            }
        }
    }
}

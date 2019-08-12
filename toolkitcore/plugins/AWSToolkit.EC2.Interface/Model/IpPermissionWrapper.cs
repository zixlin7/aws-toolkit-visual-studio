using System;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class IPPermissionWrapper : BaseModel
    {
        public IPPermissionWrapper(string ipProtocol, int fromPort, int toPort, string userId, string groupName, string source)
        {
            this.IPProtocol = ipProtocol;
            this.FromPort = fromPort;
            this.ToPort = toPort;
            this.UserId = userId;
            this.GroupName = groupName;
            this.Source = source;
        }

        public string IPProtocol
        {
            get;
        }

        public string FormattedIPProtocol
        {
            get
            {
                if (this.IPProtocol == "-1")
                    return "ALL";

                if (this.FromPort != this.ToPort)
                    return this.IPProtocol.ToUpper();

                NetworkProtocol prot = NetworkProtocol.Find(this.IPProtocol, this.FromPort);
                if (prot == null)
                    return this.IPProtocol.ToUpper();

                return string.Format("{0} ({1})", prot.DisplayName, this.IPProtocol.ToUpper());
            }
        }

        public int FromPort
        {
            get;
        }

        public int ToPort
        {
            get;
        }

        public string FormattedPortRange
        {
            get
            {
                if (this.FromPort == this.ToPort || this.ToPort == 0)
                    return this.FromPort.ToString();

                return string.Format("{0} - {1}", this.FromPort, this.ToPort);
            }
        }

        public string UserId
        {
            get;
        }

        public string GroupName
        {
            get;
        }

        public string FormattedUserAndGroup
        {
            get
            {
                if(string.IsNullOrEmpty(this.UserId))
                    return null;

                return string.Format("{0}:{1}", this.UserId, this.GroupName);
            }            
        }

        public string Source
        {
            get;
        }

        public string UnderlyingProtocol
        {
            get
            {
                NetworkProtocol prot = NetworkProtocol.Find(this.IPProtocol, this.FromPort);
                return Enum.GetName(typeof(NetworkProtocol.Protocol), prot.UnderlyingProtocol);
            }
        }
    }
}

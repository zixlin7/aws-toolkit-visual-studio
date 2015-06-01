using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

using Amazon.EC2.Model;

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
            private set;
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
            private set;
        }

        public int ToPort
        {
            get;
            private set;
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
            private set;
        }

        public string GroupName
        {
            get;
            private set;
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
            private set;
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

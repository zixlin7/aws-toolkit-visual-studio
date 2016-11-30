using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

using Amazon.AWSToolkit.CommonUI;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class NetworkAclEntryWrapper : IWrapper
    {
        internal const int DEFAULT_RULE_NUMBER = 32767;

        NetworkAclEntry _networkAclEntry;

        public NetworkAclEntryWrapper(NetworkAclEntry networkAclEntry)
        {
            this._networkAclEntry = networkAclEntry;
        }

        [Browsable(false)]
        public NetworkAclEntry NativeNetworkAclEntry
        {
            get { return this._networkAclEntry; }
        }

        [Browsable(false)]
        public string DisplayName
        {
            get { return this.NativeNetworkAclEntry.RuleNumber.ToString(); }
        }

        [Browsable(false)]
        public string TypeName
        {
            get { return "Network ACL Rule"; }
        }

        [DisplayName("Protocol")]
        public string FormattedProtocol
        {
            get
            {
                if (this.NativeNetworkAclEntry.Protocol == "-1")
                    return "ALL";

                string protocolName;
                switch (this.NativeNetworkAclEntry.Protocol)
                {
                    case "1":
                        protocolName = "ICMP";
                        break;
                    case "6":
                        protocolName = "TCP";
                        break;
                    case "17":
                        protocolName = "UDP";
                        break;
                    default:
                        protocolName = this.NativeNetworkAclEntry.Protocol;
                        break;
                }

                if (this.NativeNetworkAclEntry.PortRange.From != this.NativeNetworkAclEntry.PortRange.To)
                    return protocolName;

                NetworkProtocol port = NetworkProtocol.Find(protocolName, (int)this.NativeNetworkAclEntry.PortRange.From);
                if (port == null)
                    return protocolName.ToUpper();

                return string.Format("{0} ({1})", port.DisplayName, protocolName.ToUpper());
            }
        }

        [DisplayName("Port")]
        public string FormattedPortRange
        {
            get
            {
                if (this.NativeNetworkAclEntry.PortRange == null)
                    return "ALL";
                if (this.NativeNetworkAclEntry.PortRange.From == this.NativeNetworkAclEntry.PortRange.To || this.NativeNetworkAclEntry.PortRange.To == 0)
                    return this.NativeNetworkAclEntry.PortRange.From.ToString();

                return string.Format("{0} - {1}", this.NativeNetworkAclEntry.PortRange.From, this.NativeNetworkAclEntry.PortRange.To);
            }
        }

        [DisplayName("CIDR/IP")]
        public string CidrBlock
        {
            get { return this.NativeNetworkAclEntry.CidrBlock; }
        }

        [DisplayName("Rule #")]
        public string FormattedRuleNumber
        {
            get
            {
                if (this.NativeNetworkAclEntry.RuleNumber == DEFAULT_RULE_NUMBER)
                    return "*";

                return this.NativeNetworkAclEntry.RuleNumber.ToString();
            }
        }

        [DisplayName("Allow/Deny")]
        public string FormattedAccess
        {
            get
            {
                return this.NativeNetworkAclEntry.RuleAction.Value.ToUpper();
            }
        }
    }
}

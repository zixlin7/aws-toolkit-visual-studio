using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.CommonUI;
using Amazon.EC2.Model;
using Amazon.AWSToolkit.EC2.View.DataGrid;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class DHCPOptionsWrapper : PropertiesModel, IWrapper
    {
        DhcpOptions _dhcpOptions;

        public DHCPOptionsWrapper(DhcpOptions dhcpOptions)
        {
            this._dhcpOptions = dhcpOptions;
        }

        public override void GetPropertyNames(out string className, out string componentName)
        {
            className = "DHCP Options";
            componentName = this._dhcpOptions.DhcpOptionsId;
        }

        [Browsable(false)]
        public string DisplayName
        {
            get { return this._dhcpOptions.DhcpOptionsId; }
        }

        [Browsable(false)]
        public string TypeName
        {
            get { return "DHCP Options"; }
        }

        public DhcpOptions NativeDHCPOptions
        {
            get { return this._dhcpOptions; }
        }

        [Browsable(false)]
        public string FormattedLabel
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (!string.IsNullOrEmpty(this.DomainName))
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.AppendFormat("domain-name = {0}", this.DomainName);
                }
                if (!string.IsNullOrEmpty(this.DomainNameServices))
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.AppendFormat("domain-name-servers = {0}", this.DomainNameServices);
                }
                if (!string.IsNullOrEmpty(this.NTPServers))
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.AppendFormat("ntp-servers = {0}", this.NTPServers);
                }
                if (!string.IsNullOrEmpty(this.NetbiosNameServers))
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.AppendFormat("netbios-name-servers = {0}", this.NetbiosNameServers);
                }
                if (!string.IsNullOrEmpty(this.NetbiosNodeType))
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.AppendFormat("netbios-node-type = {0}", this.NetbiosNodeType);
                }

                return string.Format("{0} ({1})", this.NativeDHCPOptions.DhcpOptionsId, sb.ToString());
            }
        }

        [DisplayName("DHCP Options Set ID")]
        public string DhcpOptionsId
        {
            get { return this._dhcpOptions.DhcpOptionsId; }
            set { this._dhcpOptions.DhcpOptionsId = value; }
        }

        [DisplayName("Domain Name")]
        public string DomainName
        {
            get { return this.getValue("domain-name"); }
            set { this.setValue("domain-name", value); }
        }

        [DisplayName("Domain Name Services")]
        public string DomainNameServices
        {
            get { return this.getValue("domain-name-servers"); }
            set { this.setValue("domain-name-servers", value); }
        }

        [DisplayName("NTP Servers")]
        public string NTPServers
        {
            get { return this.getValue("ntp-servers"); }
            set { this.setValue("ntp-servers", value); }
        }

        [DisplayName("Netbios Name Servers")]
        public string NetbiosNameServers
        {
            get { return this.getValue("netbios-name-servers"); }
            set { this.setValue("netbios-name-servers", value); }
        }

        [DisplayName("Netbios Node Type")]
        public string NetbiosNodeType
        {
            get { return this.getValue("netbios-node-type"); }
            set { this.setValue("netbios-node-type", value); }
        }

        private string getValue(string name)
        {
            var config = this._dhcpOptions.DhcpConfigurations.FirstOrDefault(x => x.Key == name);
            if (config == null)
                return null;
            return Amazon.AWSToolkit.Util.StringUtils.CreateCommaDelimitedList(config.Values);
        }

        private void setValue(string name, string value)
        {
            var tokens = StringUtils.ParseCommaDelimitedList(value);
            var config = this._dhcpOptions.DhcpConfigurations.FirstOrDefault(x => x.Key == name);

            if (tokens.Count == 0)
            {
                if (config != null)
                    this._dhcpOptions.DhcpConfigurations.Remove(config);
            }
            else if (config == null)
                this._dhcpOptions.DhcpConfigurations.Add(new DhcpConfiguration() { Key = name, Values = tokens });
            else
                config.Values = tokens;
        }

        public Tag FindTag(string name)
        {
            if (this.NativeDHCPOptions.Tags == null)
                return null;

            return this.NativeDHCPOptions.Tags.FirstOrDefault(x => string.Equals(x.Key, name));
        }

        public void SetTag(string name, string value)
        {
            var tag = FindTag(name);
            if (tag == null)
            {
                tag = new Tag();
                tag.Key = name;
                tag.Value = value;
                this.NativeDHCPOptions.Tags.Add(tag);
            }
            else
            {
                tag.Value = value;
            }
        }

        [Browsable(false)]
        public List<Tag> Tags
        {
            get { return this.NativeDHCPOptions.Tags; }
        }
    }
}

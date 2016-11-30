using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.EC2
{
    public class NetworkProtocol
    {
        public enum Protocol { ALL = -1, ICMP = 1, TCP = 6, UDP = 17 };

        public static readonly NetworkProtocol ALL = new NetworkProtocol("ALL", Protocol.ALL, true, null);
        public static readonly NetworkProtocol TCP = new NetworkProtocol("TCP", Protocol.TCP, true, null);
        public static readonly NetworkProtocol UDP = new NetworkProtocol("UDP", Protocol.UDP, true, null);
        public static readonly NetworkProtocol ICMP = new NetworkProtocol("ICMP", Protocol.ICMP, true, null);


        public static readonly NetworkProtocol SSH = new NetworkProtocol("SSH", Protocol.TCP, false, 22);
        public static readonly NetworkProtocol SMTP = new NetworkProtocol("SMTP", Protocol.TCP, false, 25);
        public static readonly NetworkProtocol DNS = new NetworkProtocol("DNS", Protocol.UDP, false, 53);
        public static readonly NetworkProtocol HTTP = new NetworkProtocol("HTTP", Protocol.TCP, false, 80);
        public static readonly NetworkProtocol POP3 = new NetworkProtocol("POP3", Protocol.TCP, false, 110);
        public static readonly NetworkProtocol IMAP = new NetworkProtocol("IMAP", Protocol.TCP, false, 143);
        public static readonly NetworkProtocol LDAP = new NetworkProtocol("LDAP", Protocol.TCP, false, 389);
        public static readonly NetworkProtocol HTTPS = new NetworkProtocol("HTTPS", Protocol.TCP, false, 443);
        public static readonly NetworkProtocol SMTPS = new NetworkProtocol("SMTPS", Protocol.TCP, false, 465);
        public static readonly NetworkProtocol IMAPS = new NetworkProtocol("IMAPS", Protocol.TCP, false, 993);
        public static readonly NetworkProtocol POP3S = new NetworkProtocol("POP3S", Protocol.TCP, false, 995);
        public static readonly NetworkProtocol MS_SQL = new NetworkProtocol("MS SQL", Protocol.TCP, false, 1433);
        public static readonly NetworkProtocol MYSQL = new NetworkProtocol("MYSQL", Protocol.TCP, false, 3306);
        public static readonly NetworkProtocol RDP = new NetworkProtocol("RDP", Protocol.TCP, false, 3389);

        static NetworkProtocol[] _allProtocols;
        static NetworkProtocol[] _allProtocolsWithWildCard;

        static NetworkProtocol()
        {
            _allProtocols = new NetworkProtocol[] {
                    TCP,
                    UDP,
                    ICMP,

                    DNS,
                    HTTP,
                    HTTPS,
                    IMAP,
                    IMAPS,
                    LDAP,
                    MS_SQL,
                    MYSQL,
                    POP3,
                    POP3S,
                    RDP,
                    SMTP,
                    SMTPS,
                    SSH
            };


            _allProtocolsWithWildCard = new NetworkProtocol[_allProtocols.Length + 1];
            _allProtocolsWithWildCard[0] = ALL;
            Array.Copy(_allProtocols, 0, _allProtocolsWithWildCard, 1, _allProtocols.Length);
        }


        public static IEnumerable<NetworkProtocol> AllProtocols
        {
            get { return _allProtocols; }
        }

        public static IEnumerable<NetworkProtocol> AllProtocolsWithWildCard
        {
            get { return _allProtocolsWithWildCard; }
        }

        public static NetworkProtocol Find(string protocol, int port)
        {
            protocol = protocol.ToLower();
            foreach(var item in AllProtocols)
            {
                if(item.DefaultPort == port && item.UnderlyingProtocol.ToString().ToLower().Equals(protocol))
                    return item;
            }
            return null;
        }


        private NetworkProtocol(string displayName, Protocol protocol, bool supportsPortRange, int? defaultPort)
        {
            this.DisplayName = displayName;
            this.UnderlyingProtocol = protocol;
            this.SupportsPortRange = supportsPortRange;
            this.DefaultPort = defaultPort;
        }

        public string DisplayName
        {
            get;
            private set;
        }

        public NetworkProtocol.Protocol UnderlyingProtocol
        {
            get;
            private set;
        }

        public bool SupportsPortRange
        {
            get;
            private set;
        }

        public int? DefaultPort
        {
            get;
            private set;
        }
    }
}

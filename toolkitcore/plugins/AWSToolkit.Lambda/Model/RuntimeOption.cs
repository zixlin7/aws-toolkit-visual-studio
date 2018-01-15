using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amazon.AWSToolkit.Lambda.Model
{
    public class RuntimeOption
    {
        public static readonly RuntimeOption NodeJS_v0_10 = new RuntimeOption("nodejs", "Node.js v0.10.42");
        public static readonly RuntimeOption NodeJS_v4_30 = new RuntimeOption("nodejs4.3", "Node.js v4.3");
        public static readonly RuntimeOption NodeJS_v6_10 = new RuntimeOption("nodejs6.10", "Node.js v6.10");

        public static readonly RuntimeOption NetCore_v1_0 = new RuntimeOption("dotnetcore1.0", ".NET Core v1.0");
        public static readonly RuntimeOption NetCore_v2_0 = new RuntimeOption("dotnetcore2.0", ".NET Core v2.0");

        public static readonly RuntimeOption[] ALL_OPTIONS = new RuntimeOption[] { NetCore_v1_0, NetCore_v2_0, NodeJS_v6_10, NodeJS_v4_30, NodeJS_v0_10 };

        public static readonly RuntimeOption[] VS2015_OPTIONS = new RuntimeOption[] { NetCore_v1_0, NodeJS_v6_10, NodeJS_v4_30, NodeJS_v0_10 };

        public RuntimeOption(string value, string displayName)
        {
            this.Value = value;
            this.DisplayName = displayName;
        }

        public string Value { get; private set; }
        public string DisplayName { get; private set; }

        public bool IsNetCore
        {
            get { return this.Value.StartsWith("dotnetcore", StringComparison.OrdinalIgnoreCase); }
        }

        public bool IsNode
        {
            get { return this.Value.StartsWith("nodejs", StringComparison.OrdinalIgnoreCase); }
        }
    }
}

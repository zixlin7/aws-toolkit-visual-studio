using System;

namespace Amazon.AWSToolkit.Lambda.Model
{
    public class RuntimeOption
    {
        public static readonly RuntimeOption NodeJS_v8_10 = new RuntimeOption("nodejs8.10", "Node.js v8.10");
        public static readonly RuntimeOption NodeJS_v10_X = new RuntimeOption("nodejs10.x", "Node.js v10");

        public static readonly RuntimeOption NetCore_v1_0 = new RuntimeOption("dotnetcore1.0", ".NET Core v1.0");
        public static readonly RuntimeOption NetCore_v2_1 = new RuntimeOption("dotnetcore2.1", ".NET Core v2.1");

        public static readonly RuntimeOption PROVIDED = new RuntimeOption("provided", "Custom .NET Core Runtime");

        public static readonly RuntimeOption[] ALL_OPTIONS = new RuntimeOption[] { NetCore_v1_0, NetCore_v2_1, NodeJS_v8_10, NodeJS_v10_X, PROVIDED };

        private RuntimeOption(string value, string displayName)
        {
            this.Value = value;
            this.DisplayName = displayName;
        }

        public string Value { get; private set; }
        public string DisplayName { get; private set; }

        public bool IsNetCore
        {
            get { return this.Value.StartsWith("dotnetcore", StringComparison.OrdinalIgnoreCase) || this.Value.Equals(PROVIDED.Value, StringComparison.OrdinalIgnoreCase); }
        }

        public bool IsNode
        {
            get { return this.Value.StartsWith("nodejs", StringComparison.OrdinalIgnoreCase); }
        }
    }
}

using System;

namespace Amazon.AWSToolkit.Lambda.Model
{
    public class RuntimeOption
    {
        public static readonly RuntimeOption NodeJS_v12_X = new RuntimeOption(Amazon.Lambda.Runtime.Nodejs12X.Value, "Node.js v12");

        public static readonly RuntimeOption NetCore_v3_1 = new RuntimeOption(Amazon.Lambda.Runtime.Dotnetcore31.Value, ".NET Core v3.1");

        public static readonly RuntimeOption PROVIDED = new RuntimeOption(Amazon.Lambda.Runtime.Provided.Value, "Custom .NET Core Runtime");

        public static readonly RuntimeOption PROVIDED_AL2 = new RuntimeOption(Amazon.Lambda.Runtime.ProvidedAl2.Value, "Custom .NET Core Runtime (AL2)");

        public static readonly RuntimeOption[] ALL_OPTIONS = new RuntimeOption[] { NetCore_v3_1, NodeJS_v12_X, PROVIDED, PROVIDED_AL2};

        private RuntimeOption(string value, string displayName)
        {
            this.Value = value;
            this.DisplayName = displayName;
        }

        public string Value { get; }
        public string DisplayName { get; }

        public bool IsNetCore => this.Value.StartsWith("dotnetcore", StringComparison.OrdinalIgnoreCase) || IsCustomRuntime;

        public bool IsNode => this.Value.StartsWith("nodejs", StringComparison.OrdinalIgnoreCase);

        public bool IsCustomRuntime => this.Value.StartsWith("provided", StringComparison.OrdinalIgnoreCase);
    }
}

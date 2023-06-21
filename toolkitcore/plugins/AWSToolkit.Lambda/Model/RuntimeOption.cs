using System;

namespace Amazon.AWSToolkit.Lambda.Model
{
    public class RuntimeOption
    {
        public static readonly RuntimeOption NodeJS_v12_X = new RuntimeOption(Amazon.Lambda.Runtime.Nodejs12X.Value, "Node.js v12");

        public static readonly RuntimeOption DotNet6 = new RuntimeOption(Amazon.Lambda.Runtime.Dotnet6.Value, ".NET 6")
        {
            IsDotNet = true,
        };

        public static readonly RuntimeOption PROVIDED = new RuntimeOption(Amazon.Lambda.Runtime.Provided.Value, "Custom .NET Core Runtime")
        {
            IsDotNet = true,
        };

        public static readonly RuntimeOption PROVIDED_AL2 = new RuntimeOption(Amazon.Lambda.Runtime.ProvidedAl2.Value, "Custom .NET Core Runtime (AL2)")
        {
            IsDotNet = true,
        };

        public static readonly RuntimeOption[] ALL_OPTIONS = new RuntimeOption[] { DotNet6, NodeJS_v12_X, PROVIDED, PROVIDED_AL2};

        private RuntimeOption(string value, string displayName)
        {
            this.Value = value;
            this.DisplayName = displayName;
        }

        public string Value { get; }
        public string DisplayName { get; }

        public bool IsDotNet { get; private set; } = false;

        public bool IsNode => this.Value.StartsWith("nodejs", StringComparison.OrdinalIgnoreCase);

        public bool IsCustomRuntime => this.Value.StartsWith("provided", StringComparison.OrdinalIgnoreCase);
    }
}

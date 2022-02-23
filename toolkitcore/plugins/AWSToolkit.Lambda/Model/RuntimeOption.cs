using System;

namespace Amazon.AWSToolkit.Lambda.Model
{
    public class RuntimeOption
    {
        // Using string "dotnet6" instead of Runtime.Dotnet6 because the AWS SDK for .NET hasn't been updated yet with the new value.
        public const string ManagedRuntimeDotNet6 = "dotnet6";

        public static readonly RuntimeOption NodeJS_v12_X = new RuntimeOption(Amazon.Lambda.Runtime.Nodejs12X.Value, "Node.js v12");

        public static readonly RuntimeOption DotNet6 = new RuntimeOption(ManagedRuntimeDotNet6, ".NET 6")
        {
            IsDotNet = true,
        };

        public static readonly RuntimeOption NetCore_v3_1 = new RuntimeOption(Amazon.Lambda.Runtime.Dotnetcore31.Value, ".NET Core v3.1")
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

        public static readonly RuntimeOption[] ALL_OPTIONS = new RuntimeOption[] { DotNet6, NetCore_v3_1, NodeJS_v12_X, PROVIDED, PROVIDED_AL2};

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

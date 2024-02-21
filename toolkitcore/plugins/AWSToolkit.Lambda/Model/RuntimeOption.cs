using System;

namespace Amazon.AWSToolkit.Lambda.Model
{
    public class RuntimeOption
    {
        // TODO: IDE-12354 Update to use Runtime.Dotnet8 instead fo string "dotnet8" when the AWS SDK for .NET is updated with the new value.
        public const string ManagedRuntimeDotNet8 = "dotnet8";

        public static readonly RuntimeOption NodeJS_v12_X = new RuntimeOption(Amazon.Lambda.Runtime.Nodejs12X.Value, "Node.js v12");

        public static readonly RuntimeOption DotNet8 = new RuntimeOption(ManagedRuntimeDotNet8, ".NET 8")
        {
             IsDotNet = true,
        };

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

        public static readonly RuntimeOption PROVIDED_AL2023 = new RuntimeOption(Amazon.Lambda.Runtime.ProvidedAl2023.Value, "Custom .NET Core Runtime (AL2023)")
        {
            IsDotNet = true,
        };

        public static readonly RuntimeOption[] ALL_OPTIONS = new RuntimeOption[] { DotNet8, DotNet6, NodeJS_v12_X, PROVIDED, PROVIDED_AL2, PROVIDED_AL2023 };

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

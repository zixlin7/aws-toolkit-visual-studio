using System.Diagnostics;

using Amazon.Lambda;

namespace Amazon.AWSToolkit.Lambda.Model
{
    [DebuggerDisplay("Lambda Arch: {DisplayName}")]
    public class LambdaArchitecture
    {
        public static readonly LambdaArchitecture X86 = new LambdaArchitecture("x86", Architecture.X86_64.Value);
        public static readonly LambdaArchitecture Arm = new LambdaArchitecture("ARM", Architecture.Arm64.Value);
        public static readonly LambdaArchitecture[] All = { X86, Arm };

        public string DisplayName { get; }

        public string Value { get; }

        public LambdaArchitecture(string displayName, string value)
        {
            DisplayName = displayName;
            Value = value;
        }
    }
}

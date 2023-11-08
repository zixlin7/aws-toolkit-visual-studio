using Amazon.AwsToolkit.VsSdk.Common.Commands;

using Xunit;

namespace AwsToolkit.Vs.Tests.VsSdk.Common.Commands
{
    public class KeyBindingUtilitiesTests
    {
        [Theory]
        [InlineData("Global::Alt+C", "Alt+C")]
        [InlineData("C# Editor::Ctrl+K, Ctrl+Z", "Ctrl+K, Ctrl+Z")]
        [InlineData("Ctrl+C", "Ctrl+C")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void FormatKeyBindingDisplayText(string binding, string expected)
        {
            Assert.Equal(KeyBindingUtilities.FormatKeyBindingDisplayText(binding), expected);
        }
    }
}

using System.Collections.Generic;

using Amazon.AwsToolkit.VsSdk.Common.OutputWindow;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Suggestions
{
    public class FakeOutputWindow : IOutputWindow
    {
        public bool IsShown = false;
        public readonly List<string> Messages = new List<string>();

        public void Show()
        {
            IsShown = true;
        }

        public void WriteText(string message)
        {
            Messages.Add(message);
        }

        public void Dispose()
        {
        }
    }
}

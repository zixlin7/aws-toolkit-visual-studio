using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Suggestions
{
    public class FakeReferenceLogger : IReferenceLogger
    {
        public bool DidShowReferenceLogger = false;
        public readonly List<LogReferenceRequest> LoggedReferences = new List<LogReferenceRequest>();

        public Task ShowAsync()
        {
            DidShowReferenceLogger = true;
            return Task.CompletedTask;
        }

        public Task LogReferenceAsync(LogReferenceRequest request)
        {
            LoggedReferences.Add(request);
            return Task.CompletedTask;
        }
    }
}

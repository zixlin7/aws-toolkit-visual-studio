using System.Collections.Generic;
using System.Threading.Tasks;

namespace Amazon.AWSToolkit.Lambda
{
    public interface IAWSLambda
    {
        void UploadFunctionFromPath(Dictionary<string, object> seedProperties);

        Task EnsureLambdaTesterConfiguredAsync(string projectPath);
    }
}

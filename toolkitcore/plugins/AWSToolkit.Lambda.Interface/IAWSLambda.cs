using System.Collections.Generic;

namespace Amazon.AWSToolkit.Lambda
{
    public interface IAWSLambda
    {
        void UploadFunctionFromPath(Dictionary<string, object> seedProperties);
    }
}

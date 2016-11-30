using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.Lambda
{
    public interface IAWSLambda
    {
        void UploadFunctionFromPath(Dictionary<string, object> seedProperties);
    }
}

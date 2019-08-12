using System.Collections.Generic;

namespace Amazon.AWSToolkit.ECS
{
    public interface IAWSECS
    {
        void PublishContainerToAWS(Dictionary<string, object> seedProperties);

        bool SupportedInThisVersionOfVS();
    }
}

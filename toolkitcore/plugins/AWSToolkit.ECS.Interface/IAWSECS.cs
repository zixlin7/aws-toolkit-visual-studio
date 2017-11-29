using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amazon.AWSToolkit.ECS
{
    public interface IAWSECS
    {
        void PublishContainerToAWS(Dictionary<string, object> seedProperties);

        bool SupportedInThisVersionOfVS();
    }
}

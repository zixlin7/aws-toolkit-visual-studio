using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.CloudWatchEvents.Model;

namespace Amazon.AWSToolkit.ECS.Model
{
    public class ScheduledTaskWrapper
    {
        Rule _nativeRule;
        IList<Target> _nativeTargets;
        public ScheduledTaskWrapper(Rule rule, IList<Target> targets)
        {
            this._nativeRule = rule;
            this._nativeTargets = targets;
        }

        public Rule NativeRule { get { return this._nativeRule; } }
        public IList<Target> NativeTargets { get { return this._nativeTargets; } }

        public string TaskDefinitions
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                foreach(var target in this._nativeTargets)
                {
                    if (target.EcsParameters == null)
                        continue;


                    if (sb.Length > 0)
                        sb.Append(", ");

                    var name = target.EcsParameters.TaskDefinitionArn.Substring(target.EcsParameters.TaskDefinitionArn.LastIndexOf('/') + 1);
                    sb.Append(name);
                }

                return sb.ToString();
            }
        }
    }
}

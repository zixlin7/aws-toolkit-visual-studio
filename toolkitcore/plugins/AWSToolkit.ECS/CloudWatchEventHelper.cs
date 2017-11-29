using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.CloudWatchEvents;
using Amazon.CloudWatchEvents.Model;

namespace Amazon.AWSToolkit.ECS
{
    public static class CloudWatchEventHelper
    {

        public static ScheduleRulesState FetchScheduleRuleState(IAmazonCloudWatchEvents cweClient, string clusterArn)
        {
            try
            {
                var state = new ScheduleRulesState();
                var rules = cweClient.ListRules().Rules;

                var tasks = new List<Task>();
                foreach(var rule in rules)
                {
                    if (tasks.Count > 20)
                        break;

                    string ruleName = rule.Name;

                    Action<Rule> targetSearcher = x =>
                    {
                        try
                        {
                            var targets = cweClient.ListTargetsByRule(new ListTargetsByRuleRequest { Rule = ruleName }).Targets;
                            List<Target> ecsTargets = new List<Target>();
                            foreach (var target in targets)
                            {
                                if (target.EcsParameters != null)
                                {
                                    ecsTargets.Add(target);
                                }
                            }

                            if (ecsTargets.Count > 0)
                            {
                                lock (state)
                                {
                                    state.AddRule(rule, ecsTargets);
                                }
                            }
                        }                        
                        catch(Exception e)
                        {
                            if(!(e is TaskCanceledException))
                            {
                                state.LastException = e;
                            }
                        }
                    };
                    tasks.Add(Task.Run(() => targetSearcher(rule)));
                }

                Task.WaitAll(tasks.ToArray(), 3000);

                return state;
            }
            catch(Exception e)
            {
                return new ScheduleRulesState() {LastException = e } ;
            }
        }

        public class ScheduleRulesState
        {
            Dictionary<string, Rule> _rules = new Dictionary<string, Rule>();
            Dictionary<string, List<Target>> _rulesByTarget = new Dictionary<string, List<Target>>();

            public void AddRule(Rule rule, List<Target> targets)
            {
                _rules[rule.Name] = rule;
                _rulesByTarget[rule.Name] = targets;
            }

            public IEnumerable<string> RuleNames
            {
                get { return this._rules.Keys; }
            }

            public Rule GetRule(string ruleName)
            {
                Rule rule;
                if (_rules.TryGetValue(ruleName, out rule))
                    return rule;

                return null;
            }

            public IList<Target> GetRuleTargets(string ruleName)
            {
                List<Target> targets;
                if (_rulesByTarget.TryGetValue(ruleName, out targets))
                    return targets;

                return new List<Target>();

            }

            public Exception LastException { get; set; }
        }
    }
}

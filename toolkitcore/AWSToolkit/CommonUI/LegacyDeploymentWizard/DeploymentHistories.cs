using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.CommonUI.DeploymentWizard
{
    /// <summary>
    /// Collection of region-name:stack-name deployment histories for
    /// toolkit accounts. This gets persisted into the solution option
    /// (.suo) files.
    /// </summary>
    public class DeploymentHistories
    {
        Dictionary<string, Dictionary<string, string>> _deploymentHistories = new Dictionary<string, Dictionary<string, string>>();

        public Dictionary<string, string> DeploymentsForAccount(string accountID)
        {
            if (_deploymentHistories.ContainsKey(accountID))
                return _deploymentHistories[accountID];
            else
                return new Dictionary<string, string>();
        }

        public IEnumerable<string> Accounts
        {
            get { return _deploymentHistories.Keys; }
        }

        public bool HasPriorDeployments
        {
            // might want to tighten this to has accounts and at least one has a deployment stack
            get { return _deploymentHistories.Keys.Count > 0; }
        }

        public bool HasPriorDeploymentsForAccount(string accountID)
        {
            bool ret = false;

            if (_deploymentHistories.ContainsKey(accountID))
                ret = _deploymentHistories[accountID].Count > 0;

            return ret;
        }

        public void AddDeployment(string accountID, string region, string stackName)
        {
            Dictionary<string, string> stackByRegion = null;
            if (_deploymentHistories.ContainsKey(accountID))
                stackByRegion = _deploymentHistories[accountID];
            else
            {
                stackByRegion = new Dictionary<string, string>();
                _deploymentHistories.Add(accountID, stackByRegion);
            }

            if (stackByRegion.ContainsKey(region))
                stackByRegion[region] = stackName;
            else
                stackByRegion.Add(region, stackName);
        }


    }
}

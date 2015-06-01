using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.Auth.AccessControlPolicy;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.IdentityManagement.Model
{
    public class IAMPolicyModel : BaseModel
    {
        string _originalJson = string.Empty;

        public IAMPolicyModel()
        {
            this._policy = new Policy();
            Statement defaultStatement = new Statement(Statement.StatementEffect.Allow);
            defaultStatement.Resources.Add(new Resource("*"));
            defaultStatement.Actions.Add(new ActionIdentifier("*"));
            this._policy.Statements.Add(defaultStatement);
        }

        string _name;
        public string Name
        {
            get { return this._name; }
            set
            {
                this._name = value;
                base.NotifyPropertyChanged("Name");
            }
        }

        Policy _policy;
        public Policy Policy
        {
            get { return this._policy; }
            set
            {
                this._policy = value;
                base.NotifyPropertyChanged("Policy");
            }
        }

        public void CommitChanges()
        {
            this._originalJson = this.Policy.ToJson();
        }

        public bool HasChanged
        {
            get
            {
                return !this.Policy.ToJson().Equals(this._originalJson);
            }
        }
    }
}

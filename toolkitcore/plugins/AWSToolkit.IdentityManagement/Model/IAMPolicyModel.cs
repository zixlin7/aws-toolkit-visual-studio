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
            get => this._name;
            set
            {
                this._name = value;
                base.NotifyPropertyChanged("Name");
            }
        }

        Policy _policy;
        public Policy Policy
        {
            get => this._policy;
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

        public bool HasChanged => !this.Policy.ToJson().Equals(this._originalJson);
    }
}

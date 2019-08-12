using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Amazon.AWSToolkit.CommonUI;

using Amazon.Auth.AccessControlPolicy;

namespace Amazon.AWSToolkit.PolicyEditor.Model
{
    public class PolicyModel : BaseModel
    {
        public enum PolicyModelMode { UNDEFINED, IAM, S3, SNS, SQS };
        Policy _policy;

        public event EventHandler OnChange;

        public PolicyModel(PolicyModelMode mode)
            : this(mode, new Policy())
        {           
        }

        public PolicyModel(PolicyModelMode mode, Policy policy)
        {
            this._mode = mode;
            this._statements.CollectionChanged += new NotifyCollectionChangedEventHandler(onStatementsCollectionChanged);
            this._policy = policy;

            foreach (var statement in policy.Statements.ToArray())
            {
                this.AddStatement(statement);
            }
        }

        public string GetPolicyDocument()
        {
            return this._policy.ToJson();
        }

        void onStatementsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.NotifyPropertyChanged("HasStatements");
        }

        PolicyModelMode _mode;
        public PolicyModelMode Mode => this._mode;

        public Policy Policy
        {
            get => this._policy;
            set
            {
                this._policy = value;
                base.NotifyPropertyChanged("Policy");
            }
        }

        public Visibility PrincipalVisibility
        {
            get
            {
                if (this.Mode == PolicyModelMode.IAM)
                    return Visibility.Hidden;
                return Visibility.Visible;
            }
        }

        public void ImportPolicy(string content, bool clearExistingStatements)
        {
            if (clearExistingStatements)
            {
                this._statements.Clear();
                this._policy.Statements.Clear();
            }

            Policy policy;
            if (string.IsNullOrEmpty(content))
                policy = new Policy();
            else
                policy = Policy.FromJson(content);

            this._policy.Statements = policy.Statements;

            foreach (var statement in this._policy.Statements)
            {
                statement.Id = Guid.NewGuid().ToString().Replace("-", "");

                this.AddStatement(statement);
            }

            IsDirty = true;
        }

        public bool HasStatements => this._statements.Count > 0;

        ObservableCollection<StatementModel> _statements = new ObservableCollection<StatementModel>();
        public ObservableCollection<StatementModel> Statements
        {
            get => this._statements;
            set
            {
                this._statements = value;
                base.NotifyPropertyChanged("Statements");
                IsDirty = true;
            }
        }

        public void AddStatement()
        {
            Statement statement = new Statement(Statement.StatementEffect.Allow);
            AddStatement(statement);
        }

        public void AddStatement(Statement statement)
        {
            if (!this._policy.Statements.Contains(statement))
            {
                this._policy.Statements.Add(statement);
            }

            var model = new StatementModel(this, statement);
            model.PropertyChanged += new PropertyChangedEventHandler(onStatementChange);
            this._statements.Add( model );
            IsDirty = true;
        }

        public void RemoveStatement(StatementModel statement)
        {
            this._policy.Statements.Remove(statement.InternalStatement);
            this._statements.Remove(statement);
            IsDirty = true;
        }

        void onStatementChange(object sender, PropertyChangedEventArgs e)
        {
            IsDirty = true;
        }

        bool _isDirty;
        public bool IsDirty
        {
            get => this._isDirty;
            set
            {
                if (this.OnChange != null)
                {
                    this.OnChange(this, new EventArgs());
                }

                if (this._isDirty == value)
                {
                    return;
                }

                this._isDirty = value;
                base.NotifyPropertyChanged("IsDirty");
            }
        }
    }
}

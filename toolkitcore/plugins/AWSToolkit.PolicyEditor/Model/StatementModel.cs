using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Xml;
using System.Text;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.CheckedTree;
using Amazon.AWSToolkit.Util;

using Amazon.Auth.AccessControlPolicy;

namespace Amazon.AWSToolkit.PolicyEditor.Model
{
    public class StatementModel : BaseModel
    {
        PolicyModel _policyModel;
        Statement _statement;
        ObservableCollection<ConditionModel> _conditions;
        IList<CheckedViewModel<ActionModel>> _rootActions;
        HashSet<string> _hashOfSelectedActions = new HashSet<string>();
        bool _loading = false;

        public StatementModel(PolicyModel policyModel, Statement statement)
        {
            this._policyModel = policyModel;
            this._statement = statement;

            if (this.PolicyModel.Mode == PolicyModel.PolicyModelMode.IAM)
            {
                this._statement.Principals.Clear();
            }


            fixCaseForExistingActions();
            this.buildHashOfSelectedActions();
            this.loadActions();
        }

        public PolicyModel PolicyModel
        {
            get
            {
                return this._policyModel;
            }
        }

        internal Statement InternalStatement
        {
            get { return this._statement; }
        }

        public string Id
        {
            get { return this._statement.Id; }
        }

        void fixCaseForExistingActions()
        {
            foreach (var actionIdentifier in this._statement.Actions)
            {
                string[] tokens = actionIdentifier.ActionName.Split(':');
                if (tokens.Length == 2)
                {
                    actionIdentifier.ActionName = string.Format("{0}:{1}", tokens[0].ToLower(), tokens[1]);
                }
            }
        }

        #region Effect

        public string EffectDisplayLabel
        {
            get { return this._statement.Effect.ToString(); }
        }

        public bool AllowEffect
        {
            get { return this._statement.Effect == Statement.StatementEffect.Allow; }
            set
            {
                this._statement.Effect = value ? Statement.StatementEffect.Allow : Statement.StatementEffect.Deny;

                base.NotifyPropertyChanged("AllowEffect");
                base.NotifyPropertyChanged("DenyEffect");
                base.NotifyPropertyChanged("EffectDisplayLabel");
            }
        }

        public bool DenyEffect
        {
            get { return this._statement.Effect == Statement.StatementEffect.Deny; }
            set
            {
                this._statement.Effect = value ? Statement.StatementEffect.Deny : Statement.StatementEffect.Allow;
                base.NotifyPropertyChanged("DenyEffect");
                base.NotifyPropertyChanged("AllowEffect");
                base.NotifyPropertyChanged("EffectDisplayLabel");
            }
        }

        #endregion

        #region Principals

        public string[] PrincipalsLabel
        {
            get
            {
                List<string> principals = new List<string>();
                foreach (Principal principal in this._statement.Principals)
                {
                    if (principal.Provider == Principal.AWS_PROVIDER)
                    {
                        principals.Add(principal.Id);
                    }
                }
                return principals.ToArray();
            }

        }

        ObservableCollection<MutableString> _principals;
        public ObservableCollection<MutableString> Principals
        {
            get 
            {
                if (this._principals == null)
                {
                    this._principals = new ObservableCollection<MutableString>();
                    foreach (var principal in this._statement.Principals)
                    {
                        if (principal.Provider == Principal.AWS_PROVIDER)
                        {
                            var wrappped = new MutableString(principal.Id);
                            wrappped.PropertyChanged += new PropertyChangedEventHandler(OnPrincipalChange);
                            this._principals.Add(wrappped);
                        }
                    }
                }
                return this._principals; 
            }
            set
            {
            }
        }

        public void AddPrincipal(string accountNumber)
        {
            var wrappped = new MutableString(accountNumber);
            wrappped.PropertyChanged += new PropertyChangedEventHandler(OnPrincipalChange);

            if (this.Principals.Count == 1 && this.Principals[0].Value == "*")
            {
                this.Principals.Clear();
            }

            this.Principals.Add(wrappped);
            SyncPrincipals();
        }

        public void OnPrincipalChange(object sender, PropertyChangedEventArgs e)
        {
            SyncPrincipals();
        }

        public void SyncPrincipals()
        {
            this._statement.Principals.Clear();
            foreach (var wrappedPrincipal in this._principals)
            {
                this._statement.Principals.Add(new Principal(wrappedPrincipal.Value));
            }

            base.NotifyPropertyChanged("PrincipalsLabel");
        }

        #endregion

        #region Actions

        public string[] ActionIdentifiers
        {
            get
            {
                int maxNumberToShow = 10;
                int count = this._statement.Actions.Count <= maxNumberToShow ? this._statement.Actions.Count : maxNumberToShow + 1;

                string[] array = new string[count];

                int i = 0;
                foreach (var item in this._statement.Actions.OrderBy(item => item.ActionName.ToLower()))
                {
                    array[i] = item.ActionName;
                    i++;
                    if (i == maxNumberToShow)
                        break;
                }

                if (this._statement.Actions.Count > maxNumberToShow)
                {
                    array[maxNumberToShow] = string.Format("{0} more items.", this._statement.Actions.Count - maxNumberToShow);
                }
                return array;
            }
        }


        public IList<CheckedViewModel<ActionModel>> RootAction
        {
            get { return this._rootActions; }
        }

        void loadActions()
        {
            this._loading = true;
            try
            {
                string config = S3FileFetcher.Instance.GetFileContent("IAMConfiguration.xml");
                if (string.IsNullOrEmpty(config))
                    return;

                this._rootActions = new List<CheckedViewModel<ActionModel>>();
                XmlDocument xdoc = new XmlDocument();
                xdoc.LoadXml(config);
                foreach (XmlElement node in xdoc.DocumentElement.SelectNodes("actions/node"))
                {
                    var model = buildNode(null, node);
                    model.IsInitiallyExpanded = true;
                    this._rootActions.Add(model);
                    loadActions(node, model);
                }

                foreach (var model in this._rootActions)
                {
                    model.VerifyChildrenState();
                }
            }
            finally
            {
                this._loading = false;
            }
        }

        void loadActions(XmlElement parentNode, CheckedViewModel<ActionModel> parentModel)
        {
            foreach (XmlElement node in parentNode.SelectNodes("children/node"))
            {
                var model = buildNode(parentModel, node);
                loadActions(node, model);
            }

            parentModel.Children.Sort((Comparison<CheckedViewModel<ActionModel>>)((x, y) => 
            { 
                return x.Name.CompareTo(y.Name); 
            }));
        }

        CheckedViewModel<ActionModel> buildNode(CheckedViewModel<ActionModel> parentModel, XmlElement node)
        {
            string displayName = node.SelectSingleNode("display-name").InnerText;
            string systemName = node.SelectSingleNode("system-name").InnerText; ;
            
            CheckedViewModel<ActionModel> model = new CheckedViewModel<ActionModel>(parentModel, new ActionModel(systemName, displayName));
            if (parentModel != null)
            {
                parentModel.Children.Add(model);
                parentModel.IsChecked = parentModel.IsChecked;
            }
            if (this._hashOfSelectedActions.Contains(model.Data.SystemName))
                model.IsChecked = true;

            model.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(onActionModelChange);
            return model;
        }

        void onActionModelChange(object sender, PropertyChangedEventArgs e)
        {
            if (this._loading)
                return;

            if (!e.PropertyName.Equals("IsChecked"))
                return;

            bool change = false;
            CheckedViewModel<ActionModel> model = sender as CheckedViewModel<ActionModel>;
            if (model.IsChecked == null)
            {
                this._hashOfSelectedActions.Remove(model.Data.SystemName);
                checkChildren(model);
            }
            else if (model.IsChecked.GetValueOrDefault())
            {
                if (model.Parent != null && model.Parent.IsChecked.GetValueOrDefault())
                    return;

                this._hashOfSelectedActions.Add(model.Data.SystemName);
                removeChildren(model);
                change = true;
            }
            else
            {
                this._hashOfSelectedActions.Remove(model.Data.SystemName);
                change = true;
            }

            if (change)
            {
                this.syncHashOfActions();
            }
        }

        void checkChildren(CheckedViewModel<ActionModel> parent)
        {
            bool parentCheck = parent.IsChecked.GetValueOrDefault();
            foreach (var child in parent.Children)
            {
                if (child.IsChecked.GetValueOrDefault() && !parentCheck)
                {
                    if (!this._hashOfSelectedActions.Contains(child.Data.SystemName))
                    {
                        this._hashOfSelectedActions.Add(child.Data.SystemName);
                    }
                }
                else if (child.IsChecked != null && child.IsChecked.Value)
                {
                    this._hashOfSelectedActions.Remove(child.Data.SystemName);
                }                

                checkChildren(child);
            }
        }

        void removeChildren(CheckedViewModel<ActionModel> parent)
        {
            foreach (var child in parent.Children)
            {
                this._hashOfSelectedActions.Remove(child.Data.SystemName);
                removeChildren(child);
            }
        }

        void syncHashOfActions()
        {
            this._statement.Actions = new List<ActionIdentifier>();
            foreach(var action in this._hashOfSelectedActions)
            {
                this._statement.Actions.Add(new ActionIdentifier(action));
            }

            base.NotifyPropertyChanged("ActionIdentifiers");
        }


        void buildHashOfSelectedActions()
        {
            this._hashOfSelectedActions.Clear();
            foreach (var action in this._statement.Actions)
            {
                this._hashOfSelectedActions.Add(action.ActionName);
            }
        }

        #endregion

        #region Resources

        public string[] ResourcesLabel
        {
            get
            {
                List<string> resources = new List<string>();
                foreach (var item in this._statement.Resources)
                {
                    resources.Add(item.Id);
                }
                return resources.ToArray();
            }

        }

        ObservableCollection<MutableString> _resources;
        public ObservableCollection<MutableString> Resources
        {
            get 
            {
                if (this._resources == null)
                {
                    this._resources = new ObservableCollection<MutableString>();
                    foreach (var item in this._statement.Resources)
                    {
                        var wrappped = new MutableString(item.Id);
                        wrappped.PropertyChanged += new PropertyChangedEventHandler(OnResourceChange);
                        this._resources.Add(wrappped);
                    }
                }
                return this._resources; 
            }
        }

        public void AddResource(string arn)
        {
            var wrappped = new MutableString(arn);
            wrappped.PropertyChanged += new PropertyChangedEventHandler(OnResourceChange);

            if (this.Resources.Count == 1 && this.Resources[0].Value == "*")
            {
                this.Resources.Clear();
            }

            this.Resources.Add(wrappped);
            SyncResources();
        }

        public void OnResourceChange(object sender, PropertyChangedEventArgs e)
        {
            SyncResources();
        }

        public void SyncResources()
        {
            this._statement.Resources.Clear();
            foreach (var item in this._resources)
            {
                this._statement.Resources.Add(new Resource(item.Value));
            }

            base.NotifyPropertyChanged("ResourcesLabel");
        }

        #endregion

        #region Condition

        public void AddCondition()
        {
            var condition = new Amazon.Auth.AccessControlPolicy.Condition();
            this._statement.Conditions.Add(condition);
            var model = new ConditionModel(condition);
            model.PropertyChanged += new PropertyChangedEventHandler(OnConditionChange);
            this._conditions.Add(model);
        }

        public void RemoveCondition(ConditionModel condition)
        {
            this._statement.Conditions.Remove(condition.InternalCondition);
            this._conditions.Remove(condition);
            base.NotifyPropertyChanged("Conditions");
        }

        public ObservableCollection<ConditionModel> Conditions
        {
            get 
            {
                if (this._conditions == null)
                {
                    this._conditions = new ObservableCollection<ConditionModel>();
                    foreach (var condition in this._statement.Conditions)
                    {
                        var model = new ConditionModel(condition);
                        model.PropertyChanged += new PropertyChangedEventHandler(OnConditionChange);
                        this._conditions.Add(model);
                    }
                }

                return this._conditions; 
            }
        }

        public void OnConditionChange(object sender, PropertyChangedEventArgs e)
        {
            base.NotifyPropertyChanged("Conditions");
        }

        #endregion
    }
}

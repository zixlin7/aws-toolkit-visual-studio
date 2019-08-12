using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.IdentityManagement.Model;
using Amazon.AWSToolkit.PolicyEditor;

namespace Amazon.AWSToolkit.IdentityManagement.View.Components
{
    /// <summary>
    /// Interaction logic for PoliciesControl.xaml
    /// </summary>
    public partial class PoliciesControl
    {
        readonly Dictionary<TabItem, IAMPolicyModel> _tabsToModels = new Dictionary<TabItem, IAMPolicyModel>();
        public PoliciesControl()
        {
            this.DataContextChanged += this.onDataContextChanged;
            InitializeComponent();
        }

        void onDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.BuildTabs();
        }

        public void BuildTabs()
        {
            this._tabsToModels.Clear();
            this._ctlPolicyTabs.Items.Clear();
            var model = this.DataContext as EditSecureItemModel;
            if (model == null)
            {
                return;
            }

            foreach (var policy in model.IAMPolicyModels)
            {
                addPolicyTab(policy);
            }

            if (this._ctlPolicyTabs.Items.Count > 0)
            {
                this._ctlPolicyTabs.SelectedIndex = 0;
            }
        }

        void addPolicyTab(IAMPolicyModel policy)
        {
            var tab = new TabItem
            {
                Header = policy.Name, 
                Template = FindResource("awsVerticalTabItemTemplate") as ControlTemplate
            };
            var editor = new PolicyEditorControl {Policy = policy.Policy, PolicyMode = "IAM"};

            tab.Content = editor;
            editor.OnChange += this.onPolicyChange;
            this._tabsToModels[tab] = policy;
            this._ctlPolicyTabs.Items.Add(tab);
        }

        void onPolicyChange(object sender, EventArgs e)
        {
            var model = this.DataContext as EditSecureItemModel;
            if (model != null && this.IsEnabled)
                model.IsDirty = true;
        }


        public void OnAddPolicy(object sender, RoutedEventArgs e)
        {
            var nameControl = new NewPolicyNameControl();
            if (ToolkitFactory.Instance.ShellProvider.ShowModal(nameControl))
            {
                var model = this.DataContext as EditSecureItemModel;

                var policyModel = model.AddPolicy(nameControl.NewPolicyName);
                addPolicyTab(policyModel);
                this._ctlPolicyTabs.SelectedIndex = model.IAMPolicyModels.Count - 1;
            }
        }

        public void OnRemovePolicy(object sender, RoutedEventArgs e)
        {
            var tab = this._ctlPolicyTabs.SelectedItem as TabItem;
            if (tab == null)
                return;

            IAMPolicyModel policyModel;

            if (this._tabsToModels.TryGetValue(tab, out policyModel))
            {
                var model = this.DataContext as EditSecureItemModel;
                model.RemovePolicy(policyModel);
                this._ctlPolicyTabs.Items.Remove(tab);
            }
        }
    }
}

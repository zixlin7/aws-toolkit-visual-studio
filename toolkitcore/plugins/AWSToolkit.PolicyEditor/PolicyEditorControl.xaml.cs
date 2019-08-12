using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Amazon.AWSToolkit.PolicyEditor.Model;
using Amazon.Auth.AccessControlPolicy;

using log4net;

namespace Amazon.AWSToolkit.PolicyEditor
{
    /// <summary>
    /// Interaction logic for PolicyEditor.xaml
    /// </summary>
    public partial class PolicyEditorControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(PolicyEditorControl));

        PolicyModel _model;

        public PolicyEditorControl()
        {
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(onDataContextChange);
            InitializeComponent();
        }

        public event EventHandler OnChange;

        public ToolBar MainToolBar => this._ctlToolBar;

        public Policy Policy
        {
            get => (Policy)GetValue(PolicyProperty);
            set => SetValue(PolicyProperty, value);
        }

        public static readonly DependencyProperty PolicyProperty =
            DependencyProperty.Register("Policy",
            typeof(Policy),
            typeof(PolicyEditorControl),
            new UIPropertyMetadata(null, policyCallback));

        static void policyCallback(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            PolicyEditorControl control = obj as PolicyEditorControl;
            if (control == null)
                return;

            Policy value = args.NewValue as Policy;
            control.Policy = value;

            if (control.Policy != null && !string.IsNullOrEmpty(control.PolicyMode))
            {
                PolicyModel.PolicyModelMode mode = (PolicyModel.PolicyModelMode)Enum.Parse(typeof(PolicyModel.PolicyModelMode), control.PolicyMode);
                control._model = new PolicyModel(mode, value);
                control.DataContext = control._model;
            }
        }

        void updateChildDataContext(PolicyModel policyModel)
        {
            this._ctlDataGrid.DataContext = policyModel;
        }

        public string PolicyMode
        {
            get => (string)GetValue(PolicyModeProperty);
            set => SetValue(PolicyModeProperty, value);
        }

        public static readonly DependencyProperty PolicyModeProperty =
            DependencyProperty.Register("PolicyMode",
            typeof(string),
            typeof(PolicyEditorControl),
            new UIPropertyMetadata(null, policyModeCallback));

        static void policyModeCallback(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            PolicyEditorControl control = obj as PolicyEditorControl;
            if (control == null)
                return;

            control.PolicyMode = args.NewValue as string;
            PolicyModel.PolicyModelMode mode = (PolicyModel.PolicyModelMode)Enum.Parse(typeof(PolicyModel.PolicyModelMode), args.NewValue as string);
            if (control.Policy != null && !string.IsNullOrEmpty(control.PolicyMode))
            {
                control._model = new PolicyModel(mode, control.Policy);
                control.DataContext = control._model;
            }
        }

        public void LoadPolicy(string policyString)
        {
            Policy policy = Policy.FromJson(policyString);
            this._model = new PolicyModel(PolicyModel.PolicyModelMode.IAM, policy);
            this.DataContext = this._model;
        }

        public void OnAddStatement(object sender, RoutedEventArgs e)
        {
            PolicyModel model = this.DataContext as PolicyModel;
            model.AddStatement();
            this._ctlDataGrid.SelectedIndex = model.Statements.Count - 1;            
        }

        public void OnRemoveStatement(object sender, RoutedEventArgs e)
        {
            PolicyModel model = this.DataContext as PolicyModel;

            List<StatementModel> itemsToBeRemoved = new List<StatementModel>();
            foreach (StatementModel value in this._ctlDataGrid.SelectedItems)
            {
                itemsToBeRemoved.Add(value);
            }

            foreach (var value in itemsToBeRemoved)
            {
                model.RemoveStatement(value);
            }
        }

        public void OnExportStatement(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Title = "Export Policy";
            if (!dlg.ShowDialog().GetValueOrDefault())
            {
                return;
            }

            string json = null;
            try
            {
                json = this.Policy.ToJson();
                using (StreamWriter writer = new StreamWriter(dlg.FileName, false))
                {
                    writer.Write(json);
                }
            }
            catch (Exception ex)
            {
                LOGGER.Error("Error Exporting Policy: " + json, ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error Exporting Policy: " + ex.Message);
            }
        }


        public void OnImportStatement(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Import Policy";
            dlg.CheckPathExists = true;

            if (!dlg.ShowDialog().GetValueOrDefault())
            {
                return;
            }

            try
            {
                bool clearExistingStatements = false;
                if (this._model.Statements.Count > 0)
                {
                    if (ToolkitFactory.Instance.ShellProvider == null ||
                        ToolkitFactory.Instance.ShellProvider.Confirm("Import Policy", 
                                                                      "Clear existing statements before import?"))
                    {
                        clearExistingStatements = true;
                    }
                }
                this._model.ImportPolicy(File.ReadAllText(dlg.FileName), clearExistingStatements);
            }
            catch (Exception ex)
            {
                LOGGER.Error("Error Importing Policy", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error Importing Policy: " + ex.Message);
            }
        }

        void onDataContextChange(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.DataContext is PolicyModel && this._model == null)
            {
                this._model = this.DataContext as PolicyModel;
            }

            checkColumnVisiblity();
            if (this._model != null)
            {
                this._model.OnChange += new EventHandler(onModelChange);
                this.updateChildDataContext(this._model);
            }

            if (this._model.Statements.Count > 0)
            {
                this._ctlDataGrid.SelectedIndex = 0;
            }
        }

        void onModelChange(object sender, EventArgs e)
        {
            if (this.OnChange != null)
            {
                this.OnChange(this, new EventArgs());
            }
        }

        void checkColumnVisiblity()
        {
            if (this._model == null)
                return;

            if (this._model.Mode == PolicyModel.PolicyModelMode.IAM)
            {
                this._ctlPrincipalColumn.Visibility = Visibility.Hidden;
                this._ctlStatementEditor.RemovePrincipalTab();
            }
        }

        public void OnDrag(object sender, DragEventArgs e)
        {
            e.Handled = true;

            if (e.Data.GetDataPresent("ARN") ||
                (e.Data.GetDataPresent("ACCOUNT_NUMBER") && this.PolicyMode != PolicyModel.PolicyModelMode.IAM.ToString()))
                e.Effects = DragDropEffects.Move;
            else if (this._model.Mode != PolicyModel.PolicyModelMode.IAM && e.Data.GetDataPresent("PRINCIPAL.ARN"))
                e.Effects = DragDropEffects.Move;
            else
                e.Effects = DragDropEffects.None;
        }

        public void OnDrop(object sender, DragEventArgs e)
        {
            StatementModel statement;
            if (this._model.Statements.Count == 1)
            {
                statement = this._model.Statements[0];
            }
            else if (this._model.Statements.Count == 0)
            {
                this.OnAddStatement(this, new RoutedEventArgs());
                statement = this._model.Statements[0];
            }
            else if (this._ctlDataGrid.SelectedItems.Count == 1)
            {
                statement = this._model.Statements[this._ctlDataGrid.SelectedIndex];
            }
            else
            {
                return;
            }

            if (e.Data.GetDataPresent("ARN"))
            {
                string arn = e.Data.GetData("ARN") as string;
                if (arn.Contains(":s3:") && !arn.EndsWith("*"))
                    arn += "/*";

                statement.AddResource(arn);
            }
            else if (e.Data.GetDataPresent("ACCOUNT_NUMBER"))
            {
                string principal = e.Data.GetData("ACCOUNT_NUMBER") as string;
                statement.AddPrincipal(principal);
            }
            else if (e.Data.GetDataPresent("PRINCIPAL.ARN"))
            {
                string principal = e.Data.GetData("PRINCIPAL.ARN") as string;
                statement.AddPrincipal(principal);
            }
        }

        private void onSelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var statementModel = this._ctlDataGrid.SelectedItem as StatementModel;
            this._ctlStatementEditor.DataContext = statementModel;
            this._ctlStatementEditor.IsEnabled = statementModel != null;
        }

    }
}

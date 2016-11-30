using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.Lambda.Controller;
using Amazon.AWSToolkit.Lambda.Model;
using Amazon.AWSToolkit.Lambda.WizardPages.PageUI;
using Amazon.EC2.Model;
using Amazon.KeyManagementService.Model;
using log4net;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Amazon.AWSToolkit.Lambda.View.Components
{
    /// <summary>
    /// Interaction logic for AdvancedSettingsControl.xaml
    /// </summary>
    public partial class AdvancedSettingsControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(AdvancedSettingsControl));

        public ViewFunctionController Controller { get; private set; }

        public AdvancedSettingsControl()
        {
            InitializeComponent();

            FillAdvanceSettingsComboBoxes();

            _ctlSecurityGroups.PropertyChanged += ForwardEmbeddedControlPropertyChanged;
            _ctlVpcSubnets.PropertyChanged += ForwardEmbeddedControlPropertyChanged;
            _ctlKMSKey.PropertyChanged += ForwardEmbeddedControlPropertyChanged;
        }

        public void Initialize(ViewFunctionController controller)
        {
            Controller = controller;
        }

        public VpcAndSubnetWrapper SetAvailableVpcSubnets(IEnumerable<Vpc> vpcs, IEnumerable<Subnet> subnets, IEnumerable<string> selectedSubnetIds)
        {
            return _ctlVpcSubnets.SetAvailableVpcSubnets(vpcs, subnets, selectedSubnetIds);
        }

        public void SetAvailableSecurityGroups(IEnumerable<SecurityGroup> existingGroups, string autoSelectGroup, IEnumerable<string> selectedSecurityGroups)
        {
            _ctlSecurityGroups.SetAvailableSecurityGroups(existingGroups, autoSelectGroup, selectedSecurityGroups);
        }

        public IEnumerable<SubnetWrapper> SelectedSubnets
        {
            get
            {
                return _ctlVpcSubnets.SelectedSubnets;
            }
        }

        public IEnumerable<SecurityGroupWrapper> SelectedSecurityGroups
        {
            get
            {
                return _ctlSecurityGroups.SelectedSecurityGroups;
            }
        }

        public bool SubnetsSpanVPCs
        {
            get
            {
                return _ctlVpcSubnets.SubnetsSpanVPCs;
            }
        }

        public void SetAvailableKMSKeys(IEnumerable<KeyListEntry> keys, IEnumerable<AliasListEntry> aliases, string keyArnToSelect)
        {
            KeyListEntry preselectedKey = null;
            if (!string.IsNullOrEmpty(keyArnToSelect))
            {
                foreach (var k in keys)
                {
                    if (k.KeyArn.Equals(keyArnToSelect, System.StringComparison.OrdinalIgnoreCase))
                    {
                        preselectedKey = k;
                        break;
                    }
                }
            }
            else
                preselectedKey = KeyAndAliasWrapper.LambdaDefaultKMSKey.Key;

            _ctlKMSKey.SetAvailableKMSKeys(keys,
                                           aliases,
                                           new KeyAndAliasWrapper[] { KeyAndAliasWrapper.LambdaDefaultKMSKey },
                                           preselectedKey);
        }

        public KeyListEntry SelectedKMSKey
        {
            get
            {
                return _ctlKMSKey.SelectedKey;
            }
        }

        private void AddVariable_Click(object sender, RoutedEventArgs e)
        {
            Controller.Model.EnvironmentVariables.Add(new EnvironmentVariable());
            // todo: usability tweak here - put focus into the new key cell...
        }

        private void RemoveVariable_Click(object sender, RoutedEventArgs e)
        {
            EnvironmentVariable cellData = _ctlEnvironmentVariables.CurrentCell.Item as EnvironmentVariable;
            for (int i = Controller.Model.EnvironmentVariables.Count - 1; i >= 0; i--)
            {
                if (string.Compare(Controller.Model.EnvironmentVariables[i].Variable, cellData.Variable, true) == 0)
                {
                    Controller.Model.EnvironmentVariables.RemoveAt(i);
                    NotifyPropertyChanged("EnvironmentVariables");
                    return;
                }
            }
        }

        // used to trap attempts to create a duplicate variable
        private void _ctlEnvironmentVariables_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Cancel)
                return;

            TextBox editBox = e.EditingElement as TextBox;
            if (editBox == null)
            {
                LOGGER.ErrorFormat("Expected but did not receive TextBox EditingElement type for CellEditEnding event at row {0} column {1}; cannot validate for dupes.",
                                    e.Row.GetIndex(), e.Column.DisplayIndex);
                return;
            }

            string pendingEntry = editBox.Text;

            EnvironmentVariable cellData = _ctlEnvironmentVariables.CurrentCell.Item as EnvironmentVariable;
            if (cellData != null)
            {
                foreach (EnvironmentVariable ev in Controller.Model.EnvironmentVariables)
                {
                    if (ev != cellData && string.Compare(ev.Variable, pendingEntry, true) == 0)
                    {
                        e.Cancel = true;
                        MessageBox.Show(string.Format("A value already exists for variable '{0}'.", pendingEntry),
                                        "Duplicate Variable", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            NotifyPropertyChanged("EnvironmentVariables");
        }

        // used to bubble up change events from within the embedded user controls
        private void ForwardEmbeddedControlPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged(e.PropertyName);
        }

        private void PreviewIntTextInput(object sender, TextCompositionEventArgs e)
        {
            int i;
            e.Handled = !int.TryParse(e.Text, out i);
        }

        void FillAdvanceSettingsComboBoxes()
        {
            this._ctlMemory.Items.Clear();

            foreach (var value in LambdaUtilities.GetValidMemorySizes())
            {
                this._ctlMemory.Items.Add(value);
            }
        }
    }
}

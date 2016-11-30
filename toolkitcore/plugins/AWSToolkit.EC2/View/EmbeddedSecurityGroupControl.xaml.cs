using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Model;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Embeddable Security Group creation/edit control, suitable for use in dialogs
    /// or restricted-area windows. It foregoes the full editor capability for simple
    /// 'point and choose' rule setup.
    /// </summary>
    public partial class EmbeddedSecurityGroupControl : INotifyPropertyChanged
    {
        public static readonly string uiProperty_GroupName = "groupName";
        public static readonly string uiProperty_GroupDescription = "groupDescription";
        public static readonly string uiProperty_GroupPermissions = "groupPermissions";

        ObservableCollection<IPPermissionWrapper> _rulePermissions = new ObservableCollection<IPPermissionWrapper>();

        public EmbeddedSecurityGroupControl()
        {
            InitializeComponent();
            this._protocols.ItemsSource = NetworkProtocol.AllProtocols;
            this._permissions.ItemsSource = _rulePermissions;
            this._addPermission.IsEnabled = AddPermissionEnabler;
        }

        public string GroupName
        {
            get { return this._groupName.Text; }
        }

        public string GroupDescription
        {
            get { return this._groupDescription.Text; }
        }

        public ICollection<IPPermissionWrapper> GroupPermissions
        {
            get { return _rulePermissions as ICollection<IPPermissionWrapper>; }

            set
            {
                _rulePermissions.Clear();
                foreach (IPPermissionWrapper wrapper in value)
                {
                    _rulePermissions.Add(wrapper);
                }
            }
        }

        void RemovePermission_Click(object sender, RoutedEventArgs e)
        {
            IPPermissionWrapper pw = _permissions.CurrentCell.Item as IPPermissionWrapper;
            if (pw == null)
                return;

            for (int i = _rulePermissions.Count - 1; i >= 0; i--)
            {
                if (string.Compare(pw.FormattedIPProtocol, _rulePermissions[i].FormattedIPProtocol, false) == 0)
                {
                    _rulePermissions.RemoveAt(i);
                    break;
                }
            }

            NotifyPropertyChanged(uiProperty_GroupPermissions);
        }

        bool AddPermissionEnabler
        {
            get
            {
                NetworkProtocol np = _protocols.SelectedItem as NetworkProtocol;
                if (np != null)
                    return _portRange.Text.Length > 0;

                return false;
            }
        }

        void AddPermission_Click(object sender, RoutedEventArgs e)
        {
            NetworkProtocol np = _protocols.SelectedItem as NetworkProtocol;
            if (np == null)
                return;

            int startPort = -1;
            int endPort = -1;
            bool isValid = true;

            if (!_portRange.IsEnabled || string.IsNullOrEmpty(_portRange.Text))
            {
                // none of the built-in defaults support a range
                startPort = np.DefaultPort.Value;
                endPort = np.DefaultPort.Value;
            }
            else
            {
                if (np.SupportsPortRange && _portRange.Text.Contains('-'))
                {
                    string[] ports = _portRange.Text.Split(new char[] { '-' });
                    isValid = Int32.TryParse(ports[0], out startPort) && Int32.TryParse(ports[1], out endPort);
                }
                else
                {
                    isValid = Int32.TryParse(_portRange.Text, out startPort);
                    if (isValid)
                        endPort = startPort;
                }
            }

            if (isValid)
            {
                IPPermissionWrapper pw = new IPPermissionWrapper(Enum.GetName(typeof(NetworkProtocol.Protocol), np.UnderlyingProtocol),
                                                                 startPort,
                                                                 endPort,
                                                                 string.Empty,
                                                                 string.Empty,
                                                                 !string.IsNullOrEmpty(_source.Text) ? _source.Text : "0.0.0.0/0");
                _rulePermissions.Add(pw);

                // cannot get this to fire thru binding, so do it manually
                this._addPermission.IsEnabled = AddPermissionEnabler;
                NotifyPropertyChanged(uiProperty_GroupPermissions);
            }
            else
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Value for port is not a valid port number or range.");
            }
        }

        void onProtocolSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
                return;

            NetworkProtocol newProtocol = e.AddedItems[0] as NetworkProtocol;
            this._portRange.IsEnabled = newProtocol.SupportsPortRange;

            if (newProtocol.DefaultPort != null)
            {
                this._portRange.Text = newProtocol.DefaultPort.Value.ToString();
            }
            else
            {
                this._portRange.Text = string.Empty;
            }
        }

        void PortRange_TextChanged(object sender, TextChangedEventArgs e)
        {
            this._addPermission.IsEnabled = AddPermissionEnabler;
        }

        void GroupName_TextChanged(object sender, TextChangedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_GroupName);
        }

        private void GroupDescription_TextChanged(object sender, TextChangedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_GroupDescription);
        }
    }
}

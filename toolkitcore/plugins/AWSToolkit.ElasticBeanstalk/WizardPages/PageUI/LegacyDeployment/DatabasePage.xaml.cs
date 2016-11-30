using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;
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

using Amazon.RDS.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.LegacyDeployment
{
    /// <summary>
    /// Interaction logic for DatabasePage.xaml
    /// </summary>
    public partial class DatabasePage : INotifyPropertyChanged
    {
        ObservableCollection<SelectableGroup<DBSecurityGroup>> _securityGroups = new ObservableCollection<SelectableGroup<DBSecurityGroup>>();

        public DatabasePage()
        {
            InitializeComponent();
            DataContext = this;
            _dbSecurityGroups.ItemsSource = _securityGroups;
        }

        public void SetAvailableSecurityGroups(IEnumerable<DBSecurityGroup> groups, IEnumerable<DBInstance> dbInstances)
        {
            _securityGroups.Clear();

            foreach (DBSecurityGroup group in groups)
            {
                StringBuilder sb = new StringBuilder();
                foreach (DBInstance dbInstance in dbInstances)
                {
                    foreach (var groupMembership in dbInstance.DBSecurityGroups)
                    {
                        if (string.Compare(groupMembership.DBSecurityGroupName, group.DBSecurityGroupName, true) == 0)
                        {
                            if (sb.Length > 0)
                                sb.AppendFormat(",{0}", dbInstance.DBInstanceIdentifier);
                            else
                                sb.Append(dbInstance.DBInstanceIdentifier);
                        }
                    }
                }

                _securityGroups.Add(new SelectableGroup<DBSecurityGroup>(group, sb.ToString()));
            }

            _dbSecurityGroups.IsEnabled = _securityGroups.Count() > 0;
        }

        public List<string> DBSecurityGroups
        {
            get
            {
                List<string> groups = new List<string>();
                foreach (SelectableGroup<DBSecurityGroup> group in this._securityGroups)
                {
                    if (group.IsSelected)
                        groups.Add(group.InnerObject.DBSecurityGroupName);
                }

                return groups;
            }
        }

        private void SecurityGroupCheckbox_Click(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged("SecurityGroupItems");
        }
    }

    // Used to handle multi-select of security groups in an items collection control
    // and to deal with different RDS/EC2-VPC security group types. All we care about
    // is the id/name
    public class SecurityGroupInfo
    {
        public string Name { get; set; }

        // only set group id for VPC mode
        public string Id { get; set; }

        public string Description { get; set; }

        public string DisplayName
        {
            get
            {
                return IsVPCGroup ? string.Format("{0} (VPC)", Id) : Name;
            }
        }

        public bool IsVPCGroup
        {
            get { return !string.IsNullOrEmpty(Id); }            
        }
    }

    // Used to handle multi-select of security groups in a combo box
    public class SelectableGroup<T>
    {
        public bool IsSelected { get; set; }
        public T InnerObject { get; set; }
        public string ReferencingDBInstances { get; set; }

        public SelectableGroup(T innerObject, string referencingDatabases)
            : this(innerObject, referencingDatabases, false)
        {
        }

        public SelectableGroup(T innerObject, string referencingDatabases, bool isSelected)
        {
            this.InnerObject = innerObject;
            this.ReferencingDBInstances = referencingDatabases;
            this.IsSelected = isSelected;
        }
    }
}

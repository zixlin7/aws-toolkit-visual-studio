using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.Components;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.IdentityManagement.Model;
using AWSDeployment;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment
{
    /// <summary>
    /// Interaction logic for PermissionsPage.xaml
    /// </summary>
    public partial class PermissionsPage : INotifyPropertyChanged
    {
        public PermissionsPage()
        {
            DataContext = this;
            InitializeComponent();
        }

        public PermissionsPage(IAWSWizardPageController controller)
            : this()
        {
            PageController = controller;
        }

        public void InitializeIAM(AccountViewModel account, ToolkitRegion region)
        {
            this._iamPicker.Initialize(account, region, IAMCapabilityPicker.IAMMode.InstanceProfiles, BeanstalkParameters.DefaultRoleName);
        }

        public IAWSWizardPageController PageController { get; set; }

        public string SelectedInstanceProfile
        {
            get
            {
                if (this._iamPicker.SelectedRole == null)
                    return null;

                return this._iamPicker.SelectedRole.Name;
            }
        }

        public IAMCapabilityPicker.PolicyTemplate[] SelectedPolicyTemplates => this._iamPicker.SelectedPolicyTemplates;

        private ObservableCollection<string> _servicePermissionRoles = new ObservableCollection<string>();

        public ObservableCollection<string> ServicePermissionRoles => _servicePermissionRoles;

        public void SetServicePermissionRoles(IEnumerable<Role> roles)
        {
            ServicePermissionRoles.Clear();

            // load the profile names into a sorted set so we can more easily detect if
            // we need to add the default profile name into the collection (at the head)
            var names = new SortedSet<string>(StringComparer.InvariantCultureIgnoreCase);
            if (roles != null)
            {
                foreach (var r in roles)
                {
                    names.Add(r.RoleName);
                }
            }

            ServicePermissionRoles.Clear();
            if (!names.Contains(BeanstalkParameters.DefaultServiceRoleName))
                _servicePermissionRoles.Add(BeanstalkParameters.DefaultServiceRoleName);

            foreach (var n in names)
            {
                ServicePermissionRoles.Add(n);
            }

            _ctlServicePermissions.SelectedIndex = 0;
        }

        public string SelectedServicePermissionRole { get; set; }
    }
}

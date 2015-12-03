using Amazon.AWSToolkit.CommonUI.WizardFramework;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment
{
    /// <summary>
    /// Interaction logic for PermissionsPage.xaml
    /// </summary>
    public partial class PermissionsPage : INotifyPropertyChanged
    {
        public PermissionsPage()
        {
            InitializeComponent();
        }

        public PermissionsPage(IAWSWizardPageController controller)
            : this()
        {
            PageController = controller;
        }

        public IAWSWizardPageController PageController { get; set; }

        private ObservableCollection<string> _servicePermissionRoles = new ObservableCollection<string>();

        public ObservableCollection<string> ServicePermissionRoles
        {
            get { return _servicePermissionRoles; }
            set
            {
                _servicePermissionRoles.Clear();
                if (value != null)
                {
                    foreach (var v in value)
                    {
                        _servicePermissionRoles.Add(v);
                    }
                }
            }
        }

        public string SelectedServicePermissionRole { get; set; }
    }
}

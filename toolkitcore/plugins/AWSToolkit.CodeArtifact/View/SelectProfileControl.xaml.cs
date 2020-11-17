using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CodeArtifact.Controller;
using Amazon.AWSToolkit.CodeArtifact.Model;
using Amazon.AWSToolkit.Account;
using System.Collections.ObjectModel;
using Amazon.AWSToolkit.Navigator.Node;
using System;
using Amazon.AWSToolkit.CodeArtifact.Utils;
using System.Linq;

namespace Amazon.AWSToolkit.CodeArtifact.View
{
    /// <summary>
    /// Interaction logic for SelectProfileControl.xaml
    /// </summary>
    public partial class SelectProfileControl : BaseAWSControl
    {

        SelectProfileController _controller;
        AWSViewModel _rootViewModel;

        public SelectProfileControl()
        {
            InitializeComponent();
        }

        public SelectProfileControl(SelectProfileController controller)
            : this()
        {
            this._controller = controller;
            InitializeAccounts();
        }

        private void InitializeAccounts()
        {
            _rootViewModel = ToolkitFactory.Instance.RootViewModel;
            this._accountSelector.PopulateComboBox(Accounts);
            if (ToolkitFactory.Instance.Navigator != null)
            {
                string defaultProfile = Configuration.LoadInstalledConfiguration().DefaultProfile;
                _accountSelector.SelectedAccount = Accounts.FirstOrDefault(a => a.Profile.Name == defaultProfile);
            }

        }

        ObservableCollection<AccountViewModel> _accounts;

        public ObservableCollection<AccountViewModel> Accounts
        {
            get
            {
                if (_rootViewModel == null)
                {
                    return null;
                }

                if (_accounts == null)
                {
                    _accounts = new ObservableCollection<AccountViewModel>();

                    foreach (var account in this._rootViewModel.RegisteredAccounts)
                    {
                        _accounts.Add(account);
                    }
                }

                return _accounts;
            }
        }

        public override string Title => "Select CodeArtifact AWS Profile";

        public AccountViewModel SelectedAccount
        {
            get => _accountSelector.SelectedAccount;
            set
            {
                if (IsInitialized)
                {
                    _accountSelector.SelectedAccount = value;
                }
            }
        }

    }
}

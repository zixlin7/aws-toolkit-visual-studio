using System.Collections.ObjectModel;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.VisualStudio.TeamExplorer.CodeCommit.Controllers;

namespace Amazon.AWSToolkit.VisualStudio.TeamExplorer.CodeCommit.Controls
{
    /// <summary>
    /// "Login" control to associate a credential profile with an
    /// active connection to the CodeCommit provider in Team Explorer.
    /// </summary>
    public partial class ConnectControl
    {
        private ConnectController _controller;

        public ConnectControl()
        {
            InitializeComponent();
        }

        public ConnectControl(ConnectController controller)
            : this()
        {
            _controller = controller;
            InitializeAccounts();
        }

        public override string Title => "Connect to AWS CodeCommit";

        public override bool Validated()
        {
            // as we only list known profiles, we don't really have any validation to do
            return true;
        }

        public override bool OnCommit()
        {
            return true;
        }

        private void InitializeAccounts()
        {
            RootViewModel = ToolkitFactory.Instance.RootViewModel;
            this._accountSelector.PopulateComboBox(Accounts);
            if (ToolkitFactory.Instance.Navigator != null)
            {
                var selectedAccount = ToolkitFactory.Instance.Navigator.SelectedAccount;
                if (selectedAccount != null)
                    _accountSelector.SelectedAccount = selectedAccount;
            }

        }

        public AWSViewModel RootViewModel { get; set; }

        ObservableCollection<AccountViewModel> _accounts;
        public ObservableCollection<AccountViewModel> Accounts
        {
            get
            {
                if (RootViewModel == null)
                    return null;

                if (_accounts == null)
                {
                    _accounts = new ObservableCollection<AccountViewModel>();

                    foreach (var account in this.RootViewModel.RegisteredAccounts)
                    {
                        _accounts.Add(account);
                    }
                }

                return _accounts;
            }
        }

        public AccountViewModel SelectedAccount
        {
            get { return _accountSelector.SelectedAccount; }
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

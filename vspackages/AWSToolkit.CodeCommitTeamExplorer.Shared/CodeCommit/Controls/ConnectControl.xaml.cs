using System.Collections.ObjectModel;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;

using log4net;

namespace Amazon.AWSToolkit.CodeCommitTeamExplorer.CodeCommit.Controls
{
    /// <summary>
    /// "Login" control that prompts users to select which credentials to 
    /// use to connect to the CodeCommit provider in Team Explorer.
    /// </summary>
    public partial class ConnectControl
    {
        private static readonly ILog LOGGER = LogManager.GetLogger(typeof(ConnectControl));

        private ObservableCollection<AccountViewModel> _accounts;

        public ConnectControl()
        {
            InitializeComponent();
            InitializeAccounts();
        }

        public override string Title => "Connect to AWS CodeCommit";

        public override bool Validated()
        {
            // As we only list known profiles, we don't really have any validation to do
            return true;
        }

        public override bool OnCommit()
        {
            return true;
        }

        private void InitializeAccounts()
        {
            RootViewModel = ToolkitFactory.Instance.RootViewModel;
            _accountSelector.PopulateComboBox(Accounts);

            if (ToolkitFactory.Instance.Navigator != null)
            {
                var selectedAccount = ToolkitFactory.Instance.Navigator.SelectedAccount;
                if (selectedAccount != null)
                {
                    _accountSelector.SelectedAccount = selectedAccount;
                }
            }
        }

        public AWSViewModel RootViewModel { get; set; }

        public ObservableCollection<AccountViewModel> Accounts
        {
            get
            {
                if (RootViewModel == null)
                {
                    LOGGER.Warn("Skipping loading accounts because the main toolkit instance hasn't initialized yet.");
                    return null;
                }

                if (_accounts == null)
                {
                    _accounts = new ObservableCollection<AccountViewModel>();

                    foreach (var account in RootViewModel.RegisteredAccounts)
                    {
                        _accounts.Add(account);
                    }
                }

                return _accounts;
            }
        }

        public AccountViewModel SelectedAccount
        {
            get => _accountSelector.SelectedAccount;
        }
    }
}

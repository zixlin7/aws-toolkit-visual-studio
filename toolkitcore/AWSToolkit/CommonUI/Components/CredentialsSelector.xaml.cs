using System.Collections;
using System.Windows;
using System.Windows.Data;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.Converters;

namespace Amazon.AWSToolkit.CommonUI.Components
{
    /// <summary>
    /// Control that allows users to select from a list of accounts (Credentials).
    /// The listing is presented so that the accounts are grouped by credential source
    /// (example SDK Credentials, Shared Credentials).
    ///
    /// To use this control, data bind to the <see cref="Account"/> and <see cref="Accounts"/>
    /// properties.
    /// </summary>
    public partial class CredentialsSelector
    {
        public CredentialsSelector()
        {
            InitializeComponent();

            Loaded += OnLoaded;

            // Initialize the account grouping
            if (FindResource("AccountsView") is CollectionViewSource accountsView)
            {
                var accountGroup = new PropertyGroupDescription(null, new AccountViewModelGroupConverter());

                accountsView.GroupDescriptions.Clear();
                accountsView.GroupDescriptions.Add(accountGroup);
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;

            // Initialize the account sorting
            CollectionViewSource accountsView = FindResource("AccountsView") as CollectionViewSource;
            if (accountsView?.View is ListCollectionView listView)
            {
                listView.CustomSort = new GroupedAccountComparer();
            }
        }

        /// <summary>
        ///  Gets or sets the currently selected Account
        /// </summary>
        public AccountViewModel Account
        {
            get => (AccountViewModel) GetValue(AccountProperty);
            set => SetValue(AccountProperty, value);
        }

        /// <summary>
        /// Gets or sets the Accounts to choose from
        /// </summary>
        public IEnumerable Accounts
        {
            get => (IEnumerable) GetValue(AccountsProperty);
            set => SetValue(AccountsProperty, value);
        }

        public static readonly DependencyProperty AccountProperty = DependencyProperty.Register(
            nameof(Account), typeof(AccountViewModel), typeof(CredentialsSelector),
            new PropertyMetadata(null));

        public static readonly DependencyProperty AccountsProperty = DependencyProperty.Register(
            nameof(Accounts), typeof(IEnumerable), typeof(CredentialsSelector),
            new PropertyMetadata(null));
    }
}

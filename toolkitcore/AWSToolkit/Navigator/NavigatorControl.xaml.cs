using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Account.Controller;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.State;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Settings;

using log4net;

namespace Amazon.AWSToolkit.Navigator
{
    /// <summary>
    /// Interaction logic for NavigatorControl.xaml
    /// </summary>
    public partial class NavigatorControl
    {
        ILog _logger = LogManager.GetLogger(typeof(NavigatorControl));
        AWSViewModel _viewModel;
        private readonly IAwsConnectionManager _awsConnectionManager;
        private readonly ICredentialManager _credentialManager;
        private readonly ICredentialSettingsManager _credentialSettingsManager;
        private readonly IRegionProvider _regionProvider;
        private readonly NavigatorViewModel _navigatorViewModel;
        private bool isInitialized = false;

        public NavigatorControl(IRegionProvider regionProvider, ICredentialManager credentialManager,
            IAwsConnectionManager awsConnectionManager, ICredentialSettingsManager settingsManager) : this(
            regionProvider)
        {
            _credentialManager = credentialManager;
            _awsConnectionManager = awsConnectionManager;
            _awsConnectionManager.ConnectionSettingsChanged += OnConnectionManagerSettingsChanged;
            _awsConnectionManager.ConnectionStateChanged += OnConnectionStateChanged;
            _credentialSettingsManager = settingsManager;
        }

        public NavigatorControl(IRegionProvider regionProvider)
        {
            // We currently aren't using an UnLoad event to de-register events
            // because closing the Navigator's ToolWindow triggers the control's unload.
            // While unloaded, we want to continue reacting to credentials changes,
            // in the event that the Navigator is made visible again (and re-Loaded).
            // If you decide to support UnLoad and Load, you'll have to ensure
            // that you re-synchronize the explorer to the current credentials/region/resources
            // within the Load event.

            _regionProvider = regionProvider;
            _navigatorViewModel = new NavigatorViewModel(regionProvider);

            InitializeComponent();
            this.DataContext = _navigatorViewModel;
            _navigatorViewModel.PropertyChanged += NavigatorViewModelOnPropertyChanged;
            _navigatorViewModel.AddAccountCommand = new RelayCommand(AddAccount);
            _navigatorViewModel.DeleteAccountCommand = new RelayCommand(DeleteAccount);
            _navigatorViewModel.EditAccountCommand = new RelayCommand(EditEnabledCallback, EditAccount);
        }
     
        private void OnConnectionManagerSettingsChanged(object sender, ConnectionSettingsChangeArgs e)
        {
            if (isInitialized)
            {
                UpdateNavigatorSelection(e.CredentialIdentifier, e.Region);
            }
        }

        private void OnConnectionStateChanged(object sender, ConnectionStateChangeArgs e)
        {
            try
            {
                _navigatorViewModel.IsConnectionValid = _awsConnectionManager.IsValidConnectionSettings();
                UpdateViewModelConnectionProperties(_awsConnectionManager.ConnectionState);
            }
            catch (Exception ex)
            {
                //on any error set the navigator state to invalid
                var errorMessage = "Error validating connection.";
                _logger.Error(errorMessage, ex);
                _navigatorViewModel.IsConnectionValid = false;
                UpdateViewModelConnectionProperties(new ConnectionState.InvalidConnection(errorMessage));
            }
        }

        private void UpdateViewModelConnectionProperties(ConnectionState state)
        {
            _navigatorViewModel.ConnectionMessage = state.Message;
            _navigatorViewModel.IsConnectionTerminal = state.IsTerminal;
            _navigatorViewModel.ConnectionStatus = GetConnectionStatus(state);
        }

        private void NavigatorViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_navigatorViewModel.Region))
            {
                OnRegionChanged();
            }
            else if (e.PropertyName == nameof(_navigatorViewModel.PartitionId))
            {
                OnPartitionIdChanged();
            }
            else if (e.PropertyName == nameof(_navigatorViewModel.Account))
            {
                OnAccountChanged();
            }
            else if (e.PropertyName == nameof(_navigatorViewModel.IsConnectionValid))
            {
                OnConnectionValidChanged();
            }
            else if (e.PropertyName == nameof(_navigatorViewModel.IsConnectionTerminal))
            {
                OnConnectionTerminalChanged();
            }
        }

        private void OnConnectionTerminalChanged()
        {
            this.Dispatcher.Invoke(() =>
            {
                _navigatorViewModel.NavigatorCommands.Clear();
                if (_navigatorViewModel.IsConnectionTerminal && !_navigatorViewModel.IsConnectionValid)
                {
                    _navigatorViewModel.NavigatorCommands.Add(new NavigatorCommand("Retry", OnRetry, true));
                    if (IsAccountBasic())
                    {
                        _navigatorViewModel.NavigatorCommands.Add(new NavigatorCommand("Edit Account", OnEditProfile, true));
                      
                    }
                }
            });
        }

        private void OnConnectionValidChanged()
        {
            RefreshResourceTree();
        }

        private void RefreshResourceTree()
        {
            this.Dispatcher.Invoke(() =>
            {
                if (_navigatorViewModel.IsConnectionValid)
                {
                    _navigatorViewModel.Account?.CreateServiceChildren();
                }
                else
                {
                    _navigatorViewModel.Account?.Children?.Clear();
                }

                this._ctlResourceTree.ItemsSource = _navigatorViewModel.Account?.Children;
            });
        }

        private void OnAccountChanged()
        {
            this.Dispatcher.Invoke(() =>
            {
                var account = _navigatorViewModel.Account;
                this._ctlResourceTree.DataContext = account;
                this._ctlEditAccount.IsEnabled = IsAccountBasic();
                if (!string.Equals(_awsConnectionManager.ActiveCredentialIdentifier?.Id, account?.Identifier.Id))
                {
                    _awsConnectionManager.ChangeCredentialProvider(account?.Identifier);
                }
                _navigatorViewModel.PartitionId = account?.PartitionId;
               
                setToolbarState(true);
            });
        }

        public void Initialize(AWSViewModel viewModel)
        {
            this._viewModel = viewModel;
            this._viewModel.PropertyChanged += _viewModel_PropertyChanged;
            this._ctlResourceTree.DataContext = this._viewModel;

            try
            {
                this.PopulateAccounts();

                this._ctlResourceTree.MouseRightButtonDown += new MouseButtonEventHandler(OnContextMenuOpening);
                this._ctlResourceTree.MouseDoubleClick += new MouseButtonEventHandler(OnDoubleClick);
                isInitialized = true;

                UpdateNavigatorSelection(_awsConnectionManager.ActiveCredentialIdentifier,
                    _awsConnectionManager.ActiveRegion);
            }
            catch (Exception ex)
            {
                _logger.Error("Error initializing AWS Explorer.", ex);
            }
        }

        private void UpdateNavigatorSelection(ICredentialIdentifier identifier, ToolkitRegion region)
        {
            _navigatorViewModel.Account = identifier == null
                ? null
                : _navigatorViewModel.Accounts.FirstOrDefault(x => string.Equals(x.Identifier?.Id, identifier?.Id));

            _navigatorViewModel.Region = region == null
                ? null
                : _navigatorViewModel.GetRegion(region?.Id);
        }

        private void _viewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.Dispatcher.Invoke(this.PopulateAccounts);
        }

        private void PopulateAccounts()
        {
            _navigatorViewModel.UpdateAccounts(_viewModel.RegisteredAccounts.ToList());
        }

        public IViewModel SelectedNode
        {
            get => this._ctlResourceTree.SelectedItem as IViewModel;
            set
            {
                var node = value as ITreeNodeProperties;
                if (node == null)
                    return;

                this.Dispatcher.Invoke((Action) (() =>
                {
                    expandTillVisible(value);
                    node.IsSelected = true;
                }));
            }
        }

        private static NavigatorAccountConnectionStatus GetConnectionStatus(ConnectionState connectionState)
        {
            if (connectionState is ConnectionState.ValidConnection || !connectionState.IsTerminal)
            {
                return NavigatorAccountConnectionStatus.Info;
            }

            if (connectionState is ConnectionState.InvalidConnection ||
                connectionState is ConnectionState.IncompleteConfiguration)
            {
                return NavigatorAccountConnectionStatus.Error;
            }

            return NavigatorAccountConnectionStatus.Warning;
        }

        void expandTillVisible(IViewModel node)
        {
            var parent = node.Parent;
            while (parent != null)
            {
                var treeParent = parent as ITreeNodeProperties;
                if (treeParent == null)
                    break;

                treeParent.IsExpanded = true;
                parent = parent.Parent;
            }
        }

        void setToolbarState(bool enabled)
        {
            this.Dispatcher.Invoke((Action) (() =>
            {
                this._ctlEditAccount.IsEnabled = enabled;
                this._ctlDeleteAccount.IsEnabled = enabled;
            }));
        }

        void AddAccount(object parameter)
        {
            RegisterAccountController command =
                new RegisterAccountController(_credentialManager, _credentialSettingsManager, _awsConnectionManager,
                    _regionProvider);
            ActionResults results = command.Execute();
        }

        
        public string SelectedRegionId => _navigatorViewModel.Region?.Id;
        public ToolkitRegion SelectedRegion => _navigatorViewModel.Region;

        void onNavigatorRefreshClick(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshResourceTree();
            }
            catch (Exception ex)
            {
                _logger.Error("Error refreshing navigator", ex);
            }
        }

        private void OnPartitionIdChanged()
        {
            if (_navigatorViewModel.PartitionId == null)
            {
                return;
            }

            _navigatorViewModel.ShowRegionsForPartition(_navigatorViewModel.PartitionId);

            if (!_navigatorViewModel.Regions.Any())
            {
                return;
            }

            // When the Partition changes the list of Regions, the currently selected Region
            // is likely cleared (from databinding).
            // Make a reasonable region selection, if the currently selected region is not available.

            //Resolve region in following order: Last selected, fallback, profile region, default region, or first 
            var defaultRegion = RegionEndpoint.USEast1;

            var previousRegion = _regionProvider.GetRegion(ToolkitSettings.Instance.LastSelectedRegion);
            var accountRegion = _navigatorViewModel.Account?.Region;

            var selectedRegion =
                (previousRegion != null ? _navigatorViewModel.GetRegion(previousRegion.Id) : null) ??
                _navigatorViewModel.GetRegion(GetFallbackRegionId(_navigatorViewModel.PartitionId)) ??
                (accountRegion != null ? _navigatorViewModel.GetRegion(accountRegion.Id) : null) ??
                (defaultRegion != null ? _navigatorViewModel.GetRegion(defaultRegion.SystemName) : null) ??
                _navigatorViewModel.Regions.FirstOrDefault();

            _navigatorViewModel.Region = selectedRegion;
        }

        /// <summary>
        /// Suggests a region to select for the queried partition.
        /// Returns null if no suggestion is available.
        /// </summary>
        private string GetFallbackRegionId(string partitionId)
        {
            // If this partition was used earlier in the Toolkit session, try using the 
            // previously selected region for this partition.
            var previousRegionId = _navigatorViewModel.GetMostRecentRegionId(partitionId);

            return !string.IsNullOrWhiteSpace(previousRegionId) ? previousRegionId : null;
        }

        private void OnRegionChanged()
        {
            try
            {
                updateActiveRegion();
            }
            catch (Exception ex)
            {
                _logger.Error("Error handling region change", ex);
            }
        }

        void updateActiveRegion()
        {
            if (ToolkitFactory.Instance == null || ToolkitFactory.Instance.RootViewModel == null)
                return;

            if (_navigatorViewModel.Region == null) { return; }

            if (!string.Equals(_awsConnectionManager.ActiveRegion.Id, _navigatorViewModel.Region.Id))
            {
                _awsConnectionManager.ChangeRegion(_navigatorViewModel.Region);
            }
        }

        public AccountViewModel SelectedAccount => _navigatorViewModel.Account;

        public void RefreshAccounts()
        {
            this._viewModel.Refresh();
            PopulateAccounts();
        }

        private bool EditEnabledCallback(object arg)
        {
            return IsAccountBasic();
        }

        private void EditAccount(object parameter)
        {
            AccountViewModel viewModel = _navigatorViewModel.Account;
            if (viewModel == null)
                return;

            try
            {
                var command = new EditAccountController(_credentialManager, _credentialSettingsManager,
                    _awsConnectionManager, _regionProvider);
                var results = command.Execute(viewModel);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error", e.Message);
            }
        }

        void DeleteAccount(object parameter)
        {
            AccountViewModel viewModel = _navigatorViewModel.Account;
            if (viewModel == null)
                return;

            string msg = string.Format("Are you sure you want to delete the '{0}' profile?", viewModel.Name);
            if (!ToolkitFactory.Instance.ShellProvider.Confirm("Delete Account Profile", msg))
            {
                return;
            }

            var command =
                new UnregisterAccountController(_credentialSettingsManager);
            var results = command.Execute(viewModel);
            if (results.Success)
            {
                var ide = _credentialManager.GetCredentialIdentifiers()
                    .FirstOrDefault(x => x.ProfileName.Equals("default"));
                _awsConnectionManager.ChangeCredentialProvider(ide);
            }
        }

        void OnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TreeView tv = (TreeView) sender;
            if (tv == null)
                return;

            e.Handled = true;

            // Check to see if it is a sign up error and if so navigate to 
            // marketing website
            if (tv.SelectedItem is ErrorViewModel)
            {
                var error = tv.SelectedItem as ErrorViewModel;
                if (!error.IsSignUpError)
                    return;

                var service = error.FindAncestor<ServiceRootViewModel>();
                if (service == null)
                    return;

                var serviceMeta = service.MetaNode as ServiceRootViewMetaNode;
                if (string.IsNullOrEmpty(serviceMeta.MarketingWebSite))
                    return;

                Process.Start(new ProcessStartInfo(serviceMeta.MarketingWebSite));
            }

            AbstractViewModel node = tv.SelectedItem as AbstractViewModel;
            if (node == null)
                return;

            ActionHandlerWrapper action =
                node.MetaNode.Actions.SingleOrDefault(item => item != null && item.IsDefault);
            if (action == null || action.Handler == null)
                return;
            NodeClickExecutor executor = new NodeClickExecutor(this, action);
            executor.OnClick(sender, e);
        }


        #region Context Menu

        // This code came from http://social.msdn.microsoft.com/forums/en-US/wpf/thread/e18a5660-3fa2-480c-acce-3b34efeeeaa7/
        // which handles the fact that right clicking in the tree vie
        // does not select the node.
        TreeView makeSureRightClickNodeIsSelected(object sender, MouseButtonEventArgs e)
        {
            TreeView tv = (TreeView) sender;
            IInputElement element = tv.InputHitTest(e.GetPosition(tv));
            while (!((element is TreeView) || element == null))
            {
                if (element is TreeViewItem)
                    break;

                if (element is FrameworkElement)
                {
                    FrameworkElement fe = (FrameworkElement) element;
                    element = (IInputElement) (fe.Parent ?? fe.TemplatedParent);
                }
                else
                    break;
            }

            if (element is TreeViewItem)
            {
                element.Focus();
            }

            return tv;
        }

        void OnContextMenuOpening(object sender, MouseButtonEventArgs e)
        {
            AbstractViewModel node = null;
            try
            {
                TreeView tv = makeSureRightClickNodeIsSelected(sender, e);
                if (tv == null)
                    return;

                e.Handled = true;

                node = tv.SelectedItem as AbstractViewModel;
                if (node == null)
                    return;

                bool isNotLoaded = node.FailedToLoadChildren;

                IList<ActionHandlerWrapper> actions = node.GetVisibleActions();

                ContextMenu menu = new ContextMenu();
                bool lastAddWasSeparator = false;
                foreach (ActionHandlerWrapper action in actions)
                {
                    if (action == null)
                    {
                        // this avoids pairing up separators now plugins can control visibility
                        // of their actions at runtime
                        if (!lastAddWasSeparator)
                        {
                            menu.Items.Add(new Separator());
                            lastAddWasSeparator = true;
                        }
                    }
                    else
                    {
                        if (action.Handler == null)
                            continue;

                        ActionHandlerWrapper.ActionVisibility actionVis = ActionHandlerWrapper.ActionVisibility.enabled;
                        if (action.VisibilityHandler != null)
                        {
                            actionVis = action.VisibilityHandler(node);
                            if ((actionVis & ActionHandlerWrapper.ActionVisibility.hidden) ==
                                ActionHandlerWrapper.ActionVisibility.hidden)
                                continue;
                        }

                        MenuItem mi = new MenuItem();
                        mi.Header = action.Name;
                        mi.Icon = action.Icon;

                        mi.Click += new RoutedEventHandler(new NodeClickExecutor(this, action).OnClick);

                        mi.IsEnabled = !isNotLoaded && actionVis == ActionHandlerWrapper.ActionVisibility.enabled;
                        menu.Items.Add(mi);

                        lastAddWasSeparator = false;
                    }
                }

                if (node.MetaNode.SupportsRefresh)
                {
                    MenuItem mi = new MenuItem();
                    mi.Header = "Refresh";
                    mi.Icon = IconHelper.GetIcon("refresh.png");
                    mi.Click += new RoutedEventHandler(onRefreshClick);

                    if (!lastAddWasSeparator && menu.Items.Count > 0)
                    {
                        menu.Items.Add(new Separator());
                    }

                    menu.Items.Add(mi);
                }

                this._ctlResourceTree.ContextMenu = menu.Items.Count > 0 ? menu : null;
            }
            catch (Exception ex)
            {
                if (node == null)
                    this._logger.Error("Error displaying context menu", ex);
                else
                    this._logger.Error("Error displaying context menu for type " + node.GetType().FullName, ex);

                ToolkitFactory.Instance.ShellProvider.ShowError("Error displaying context menu: " + ex.Message);
            }
        }

        void onRefreshClick(object sender, RoutedEventArgs e)
        {
            IViewModel node = this._ctlResourceTree.SelectedItem as IViewModel;
            
            node.Refresh(true);
        }

        #endregion


        Point _lastMouseDown;

        void resourceTree_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _lastMouseDown = e.GetPosition(this._ctlResourceTree);
            }
        }

        IViewModel draggedItem;

        void resourceTree_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    // Only start DnD when it was started over a node in the tree. 
                    if (((FrameworkElement) e.OriginalSource).Parent == null)
                        return;

                    Point currentPosition = e.GetPosition(_ctlResourceTree);

                    if ((Math.Abs(currentPosition.X - _lastMouseDown.X) > 10.0) ||
                        (Math.Abs(currentPosition.Y - _lastMouseDown.Y) > 10.0))
                    {
                        draggedItem = (IViewModel) _ctlResourceTree.SelectedItem;
                        if (draggedItem != null)
                        {
                            DataObject dataObject = new DataObject();
                            dataObject.SetData(_ctlResourceTree.SelectedValue.GetType(),
                                _ctlResourceTree.SelectedValue);
                            IViewModel viewModel = _ctlResourceTree.SelectedValue as IViewModel;
                            if (viewModel != null)
                            {
                                viewModel.LoadDnDObjects(dataObject);
                            }

                            DragDrop.DoDragDrop(_ctlResourceTree,
                                dataObject,
                                DragDropEffects.Move);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }


        private void onFeedback(object sender, RequestNavigateEventArgs e)
        {
            ToolkitFactory.Instance.ShellProvider.ShowModal(new Feedback(), MessageBoxButton.OK);
        }

        private void onLink(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.ToString()));
                e.Handled = true;
            }
            catch (Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error navigating to link: " + ex.Message);
            }
        }

        private bool IsAccountBasic()
        {
            var account = _navigatorViewModel.Account;
            return account != null && IsAccountBasic(account.Identifier);
        }

        private bool IsAccountBasic(ICredentialIdentifier identifier)
        {
            return _credentialSettingsManager.GetCredentialType(identifier) ==
                   CredentialType.StaticProfile;
        }

        private void OnRetry()
        {
            _awsConnectionManager.RefreshConnectionState();
        }

        private void OnEditProfile()
        {
            EditAccount(this);
        }
    }
}

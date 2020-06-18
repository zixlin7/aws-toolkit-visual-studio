using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Account.Controller;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.VersionInfo;
using Amazon.Runtime.Internal.Settings;
using Amazon.Runtime.CredentialManagement.Internal;

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

        public NavigatorControl()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(onNavigatorLoad);
            this._ctlAccounts.PropertyChanged += _ctlAccounts_PropertyChanged;
        }

        private void _ctlAccounts_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var account = this._ctlAccounts.SelectedAccount;
            this._ctlResourceTree.DataContext = account;

            // The toolkit only supports editing credential profiles of just access and secret key.
            this._ctlEditAccount.IsEnabled = account != null && CredentialProfileUtils.GetProfileType(account.Profile) == CredentialProfileType.Basic;

            this.setInitialRegionSelection();

            if (account == null)
            {
                PersistenceManager.Instance.SetSetting(ToolkitSettingsConstants.LastAcountSelectedKey, null);
            }
            else
            {
                PersistenceManager.Instance.SetSetting(ToolkitSettingsConstants.LastAcountSelectedKey, account.SettingsUniqueKey);
                if (account.Children != null)
                {
                    this._ctlResourceTree.ItemsSource = account.Children;
                }
            }

            OnSelectedAccountChanged();
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

                var lastAccountId = PersistenceManager.Instance.GetSetting(ToolkitSettingsConstants.LastAcountSelectedKey);
                AccountViewModel accountViewModel = null;
                if (!string.IsNullOrEmpty(lastAccountId))
                {
                    foreach (var account in this._viewModel.RegisteredAccounts)
                    {
                        if (account.SettingsUniqueKey == lastAccountId)
                        {
                            accountViewModel = account;
                            break;
                        }
                    }
                }

                if (accountViewModel == null && this._viewModel.RegisteredAccounts.Count > 0)
                {
                    accountViewModel = this._viewModel.RegisteredAccounts[0];
                }

                this._ctlAccounts.SelectedAccount = accountViewModel;
                if (accountViewModel == null)
                {
                    setToolbarState(false);
                }
            }
            catch(Exception ex)
            {
                _logger.Error("Error initializing AWS Explorer.", ex);
            }
        }

        private void _viewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.PopulateAccounts();
        }

        private void PopulateAccounts()
        {
            var accounts = new List<AccountViewModel>();
            foreach(var accountModel in this._viewModel.RegisteredAccounts)
            {
                accounts.Add(accountModel);
            }

            this._ctlAccounts.PopulateComboBox(accounts);
        }

        public IViewModel SelectedNode
        {
            get => this._ctlResourceTree.SelectedItem as IViewModel;
            set
            {
                var node = value as ITreeNodeProperties;
                if (node == null)
                    return;

                this.Dispatcher.Invoke((Action)(() =>
                    {
                        expandTillVisible(value);
                        node.IsSelected = true;
                    }));
            }
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
            this.Dispatcher.Invoke((Action)(() =>
                {
                    this._ctlEditAccount.IsEnabled = enabled;
                    this._ctlDeleteAccount.IsEnabled = enabled;
                }));
        }

        void onNavigatorLoad(object sender, RoutedEventArgs e)
        {
            var shellVersion = ToolkitFactory.Instance.ShellProvider.ShellVersion;
            if (shellVersion == "2013" || shellVersion == "2015")
            {
                VersionManager vm = new VersionManager();
                ThreadPool.QueueUserWorkItem(new WaitCallback(vm.CheckVersion));
            }
        }

        void addAccount(object sender, RoutedEventArgs e)
        {
            RegisterAccountController command = new RegisterAccountController();
            ActionResults results = command.Execute();
            if (results.Success)
            {
                UpdateAccountSelection(command.Model.UniqueKey, true);
            }
        }

        public RegionEndPointsManager.RegionEndPoints SelectedRegionEndPoints => this._ctlRegions.SelectedItem as RegionEndPointsManager.RegionEndPoints;

        void setInitialRegionSelection()
        {
            if (this.SelectedAccount == null)
                return;

            if (RegionEndPointsManager.GetInstance().FailedToLoad)
            {
                if (RegionEndPointsManager.GetInstance().ErrorLoading != null)
                {
                    this._ctlErrorMessage.Text = "Failed to connect to AWS";
                    this._ctlErrorMessage.Height = double.NaN;
                }
                this._ctlRegions.IsEnabled = false;

                return;
            }

            this._ctlRegions.IsEnabled = true;
            this._ctlErrorMessage.Height = 0;
            this._ctlErrorMessage.Text = "";

            if (this.SelectedAccount.HasRestrictions)
            {
                List<RegionEndPointsManager.RegionEndPoints> regions = new List<RegionEndPointsManager.RegionEndPoints>();
                foreach (var region in RegionEndPointsManager.GetInstance().Regions)
                {
                    if (region.ContainAnyRestrictions(this.SelectedAccount.Restrictions))
                    {
                        regions.Add(region);
                    }
                }

                this._ctlRegions.ItemsSource = regions;
            }
            else
            {
                List<RegionEndPointsManager.RegionEndPoints> regions = new List<RegionEndPointsManager.RegionEndPoints>();
                foreach (var region in RegionEndPointsManager.GetInstance().Regions)
                {
                    if (!region.HasRestrictions)
                        regions.Add(region);
                }

                this._ctlRegions.ItemsSource = regions;
            }

            if(this._ctlRegions.Items.Count == 0)
                return;

            bool foundInList = false;
            var defaultRegion = RegionEndPointsManager.GetInstance().GetDefaultRegionEndPoints();
            foreach (RegionEndPointsManager.RegionEndPoints r in this._ctlRegions.ItemsSource)
            {
                if (r == defaultRegion)
                {
                    foundInList = true;
                    this._ctlRegions.SelectedItem = defaultRegion;
                    break;
                }
            }

            if (foundInList)
                this._ctlRegions.SelectedItem = defaultRegion;
            else
                this._ctlRegions.SelectedItem = this._ctlRegions.Items[0];
        }

        void onNavigatorRefreshClick(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshAccounts();

                RegionEndPointsManager.GetInstance().Refresh();
                setInitialRegionSelection();
                updateActiveRegion();
                _ctlAccounts_PropertyChanged(this, null);
            }
            catch(Exception ex)
            {
                _logger.Error("Error refreshing navigator", ex);
            }
        }

        void onRegionChanged(object sender, SelectionChangedEventArgs e)
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

            var region = this._ctlRegions.SelectedItem as RegionEndPointsManager.RegionEndPoints;
            if (region == null)
            {
                return;
            }


            RegionEndPointsManager.GetInstance().SetDefaultRegionEndPoints(region);

            foreach (AccountViewModel account in ToolkitFactory.Instance.RootViewModel.RegisteredAccounts)
            {
                account.CreateServiceChildren();
            }
        }

        public AccountViewModel SelectedAccount => this._ctlAccounts.SelectedAccount;
        public event EventHandler SelectedAccountChanged;

        public AccountViewModel UpdateAccountSelection(Guid uniqueKey, bool refreshAccounts)
        {
            AccountViewModel viewModel = null;
            this.Dispatcher.Invoke((Action)(() =>
                {
                    try
                    {
                        if (refreshAccounts)
                        {
                            RefreshAccounts();
                        }

                        foreach (var vm in this._viewModel.RegisteredAccounts)
                        {
                            if (new Guid(vm.SettingsUniqueKey).Equals(uniqueKey))
                            {
                                this.Dispatcher.Invoke((Action)(() =>
                                    {
                                        this._ctlAccounts.SelectedAccount = vm;
                                    }));
                                viewModel = vm;
                                break;
                            }
                        }

                        setToolbarState(true);
                        setInitialRegionSelection();
                    }
                    catch(Exception ex)
                    {
                        _logger.Error("Error updating account selection.", ex);
                    }
                }));

            return viewModel;
        }

        public void RefreshAccounts()
        {
            this._viewModel.Refresh();
            PopulateAccounts();
        }

        public void UpdateRegionSelection(string regionSystemName)
        {
            var region = RegionEndPointsManager.GetInstance().GetRegion(regionSystemName);
            UpdateRegionSelection(region);
        }

        public void UpdateRegionSelection(RegionEndPointsManager.RegionEndPoints region)
        {
            if (region != null)
            {
                this.Dispatcher.Invoke((Action)(() =>
                    {
                        this._ctlRegions.SelectedItem = region;
                    }));
            }
        }

        void editAccount(object sender, RoutedEventArgs evnt)
        {
            AccountViewModel viewModel = this._ctlAccounts.SelectedAccount;
            if(viewModel == null)
                return;

            try
            {
                var command = new EditAccountController();
                var results = command.Execute(viewModel);
                if (results.Success)
                {
                    UpdateAccountSelection(command.Model.UniqueKey, true);
                }
            }
            catch(Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error", e.Message);
            }
        }

        void deleteAccount(object sender, RoutedEventArgs e)
        {
            AccountViewModel viewModel = this._ctlAccounts.SelectedAccount;
            if (viewModel == null)
                return;

            string msg = string.Format("Are you sure you want to delete the '{0}' profile?", viewModel.Name);
            if (!ToolkitFactory.Instance.ShellProvider.Confirm("Delete Account Profile", msg))
            {
                return;
            }

            var command = new UnregisterAccountController();
            if (command.Execute(viewModel).Success)
            {
                this._viewModel.RegisteredAccounts.Remove(viewModel);

                Guid selectedId = Guid.Empty;
                if (this._viewModel.RegisteredAccounts.Count > 0)
                {
                    selectedId = Guid.Parse(this._viewModel.RegisteredAccounts[0].SettingsUniqueKey);
                }

                UpdateAccountSelection(selectedId, true);
            }
        }

        void OnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TreeView tv = (TreeView)sender;
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
            TreeView tv = (TreeView)sender;
            IInputElement element = tv.InputHitTest(e.GetPosition(tv));
            while (!((element is TreeView) || element == null))
            {
                if (element is TreeViewItem)
                    break;

                if (element is FrameworkElement)
                {
                    FrameworkElement fe = (FrameworkElement)element;
                    element = (IInputElement)(fe.Parent ?? fe.TemplatedParent);
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
                            if ((actionVis & ActionHandlerWrapper.ActionVisibility.hidden) == ActionHandlerWrapper.ActionVisibility.hidden)
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
                    if (((FrameworkElement)e.OriginalSource).Parent == null)
                        return;

                    Point currentPosition = e.GetPosition(_ctlResourceTree);

                    if ((Math.Abs(currentPosition.X - _lastMouseDown.X) > 10.0) ||
                        (Math.Abs(currentPosition.Y - _lastMouseDown.Y) > 10.0))
                    {
                        draggedItem = (IViewModel)_ctlResourceTree.SelectedItem;
                        if (draggedItem != null)
                        {
                            DataObject dataObject = new DataObject();
                            dataObject.SetData(_ctlResourceTree.SelectedValue.GetType(), _ctlResourceTree.SelectedValue);
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

        private void OnSelectedAccountChanged()
        {
            SelectedAccountChanged?.Invoke(this, new EventArgs());
        }
    }
}

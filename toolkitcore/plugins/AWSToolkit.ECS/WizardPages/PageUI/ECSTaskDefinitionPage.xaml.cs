using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.ECS.Controller;
using Amazon.AWSToolkit.ECS.WizardPages.PageControllers;
using log4net;
using System;
using Amazon.AWSToolkit.Account;
using System.ComponentModel;
using System.Windows.Controls;

using Task = System.Threading.Tasks.Task;

using Amazon.ECS;
using Amazon.ECS.Model;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CommonUI.Components;
using Amazon.ECS.Tools;
using Amazon.Common.DotNetCli.Tools;

namespace Amazon.AWSToolkit.ECS.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for ECSTaskDefinitionPage.xaml
    /// </summary>
    public partial class ECSTaskDefinitionPage : BaseAWSUserControl, INotifyPropertyChanged
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ECSTaskDefinitionPage));

        bool _initialTaskDefinitionLoad = true;

        public ECSTaskDefinitionPageController PageController { get; private set; }

        public ECSTaskDefinitionPage()
        {
            InitializeComponent();

            PortMappings = new ObservableCollection<PortMappingItem>();
            EnvironmentVariables = new ObservableCollection<EnvironmentVariableItem>();

            DataContext = this;
        }

        public ECSTaskDefinitionPage(ECSTaskDefinitionPageController pageController)
            : this()
        {
            PageController = pageController;

            if(PageController.HostingWizard[PublishContainerToAWSWizardProperties.IsWebProject] is bool &&
                (bool)PageController.HostingWizard[PublishContainerToAWSWizardProperties.IsWebProject])
            {
                EnvironmentVariables.Add(new EnvironmentVariableItem { Variable = "ASPNETCORE_ENVIRONMENT", Value = "Production" });

                if (PageController.HostingWizard[PublishContainerToAWSWizardProperties.TargetGroup] != null)
                    PortMappings.Add(new PortMappingItem { HostPort = 0, ContainerPort = 80 });
                else
                    PortMappings.Add(new PortMappingItem { HostPort = 80, ContainerPort = 80 });
            }

            UpdateExistingTaskDefinition();
            LoadPreviousValues(PageController.HostingWizard);

            string role = null;
            if (PageController.HostingWizard.IsPropertySet(PublishContainerToAWSWizardProperties.TaskRole))
                role = PageController.HostingWizard[PublishContainerToAWSWizardProperties.TaskRole] as string;

            IntializeIAMPickerForAccountAsync(role);
        }

        public Visibility MemorySettingsVisibility
        {
            get
            {
                if (this.PageController.HostingWizard.IsFargateLaunch())
                    return Visibility.Collapsed;

                return Visibility.Visible;
            }
        }

        public void PageActivated()
        {
            if(this.PageController.HostingWizard.IsFargateLaunch())
            {
                this._ctlFargatePortMappings.Visibility = Visibility.Visible;
                this._ctlEC2PortMappings.Visibility = Visibility.Collapsed;
                this._ctlExecutionRole.Visibility = Visibility.Visible;
                this._ctlExecutionRoleDescription.Visibility = Visibility.Visible;
            }
            else
            {
                this._ctlFargatePortMappings.Visibility = Visibility.Collapsed;
                this._ctlEC2PortMappings.Visibility = Visibility.Visible;
                this._ctlExecutionRole.Visibility = Visibility.Collapsed;
                this._ctlExecutionRoleDescription.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadPreviousValues(IAWSWizard hostWizard)
        {
            if (hostWizard[PublishContainerToAWSWizardProperties.MemoryHardLimit] is int?)
                this.MemoryHardLimit = (int)hostWizard[PublishContainerToAWSWizardProperties.MemoryHardLimit];
            if (hostWizard[PublishContainerToAWSWizardProperties.MemorySoftLimit] is int?)
                this.MemorySoftLimit = (int)hostWizard[PublishContainerToAWSWizardProperties.MemorySoftLimit];

            if(!this.MemoryHardLimit.HasValue && !this.MemorySoftLimit.HasValue && hostWizard[PublishContainerToAWSWizardProperties.AllocatedTaskMemory] is string)
            {
                var taskMemoryStr = hostWizard[PublishContainerToAWSWizardProperties.AllocatedTaskMemory] as string;
                int taskMemory;
                if (int.TryParse(taskMemoryStr, out taskMemory))
                {
                    this.MemoryHardLimit = taskMemory;
                }
            }

            if(hostWizard[PublishContainerToAWSWizardProperties.TaskExecutionRole] is string)
            {
                this.TaskExecutionRole = hostWizard[PublishContainerToAWSWizardProperties.TaskExecutionRole] as string;
            }

            var previousMappings = hostWizard[PublishContainerToAWSWizardProperties.PortMappings] as IList<PortMappingItem>;
            if (previousMappings != null)
            {
                this.PortMappings.Clear();
                foreach(var mapping in previousMappings)
                {
                    this.PortMappings.Add(mapping);
                }
            }

            var previousEnvironmentVariables = hostWizard[PublishContainerToAWSWizardProperties.EnvironmentVariables] as IList<EnvironmentVariableItem>;
            if (previousEnvironmentVariables != null)
            {
                this.EnvironmentVariables.Clear();
                foreach (var item in previousEnvironmentVariables)
                {
                    this.EnvironmentVariables.Add(item);
                }
            }
        }

        private void IntializeIAMPickerForAccountAsync(string selectedRole)
        {
            // could check here if we're already bound to this a/c and region
            var account = PageController.HostingWizard[PublishContainerToAWSWizardProperties.UserAccount] as AccountViewModel;
            var region = PageController.HostingWizard[PublishContainerToAWSWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;

            if (account == null || region == null)
                return;

            using (var iamClient = account.CreateServiceClient<Amazon.IdentityManagement.AmazonIdentityManagementServiceClient>(region))
            {
                var taskRole = RoleHelper.FindExistingRolesAsync(iamClient, RoleHelper.ECS_TASK_ASSUME_ROLE_PRINCIPAL, int.MaxValue);
                var taskPolicies = RoleHelper.FindManagedPoliciesAsync(iamClient, RoleHelper.DEFAULT_ITEM_MAX);
                IList<Amazon.IdentityManagement.Model.Role> roles = null;
                IList<Amazon.IdentityManagement.Model.ManagedPolicy> policies = null;

                var errorMessages = new List<string>();
                try
                {
                    Task.WaitAll(taskRole, taskPolicies);
                    roles = taskRole.Result;
                }
                catch (AggregateException e)
                {
                    foreach (var inner in e.InnerExceptions)
                    {
                        if (!(inner is AggregateException))
                        {
                            errorMessages.Add(inner.Message);
                        }
                    }

                }
                catch (Exception e)
                {
                    errorMessages.Add(e.Message);
                }

                if (taskRole.IsCompleted && taskRole.Exception == null)
                {
                    roles = taskRole.Result;
                }
                if (taskPolicies.IsCompleted && taskPolicies.Exception == null)
                {
                    policies = taskPolicies.Result;
                }


                if (roles != null)
                {
                    this._ctlIAMRolePicker.Initialize(roles, policies, selectedRole);

                    this._ctlExecutionRole.Items.Clear();
                    this._ctlExecutionRole.Items.Add(ECSWizardUtils.CREATE_NEW_TEXT);
                    foreach(var role in roles)
                    {
                        this._ctlExecutionRole.Items.Add(role.RoleName);
                    }

                    string defaultRole = null;
                    var previousExecutionRole = this.PageController.HostingWizard[PublishContainerToAWSWizardProperties.TaskExecutionRole] as string;
                    if(!string.IsNullOrEmpty(previousExecutionRole))
                    {
                        var role = roles.FirstOrDefault(x => x.Arn.EndsWith(previousExecutionRole));
                        if (role != null)
                            defaultRole = role.RoleName;
                    }
                    else if(defaultRole == null)
                    {
                        var role = roles.FirstOrDefault(x => string.Equals(x.RoleName, ECSWizardUtils.DEFAULT_ECS_TASK_EXECUTION_ROLE, StringComparison.Ordinal));
                        if (role != null)
                            defaultRole = role.RoleName;
                    }

                    if(defaultRole != null)
                    {
                        this._ctlExecutionRole.SelectedItem = defaultRole;
                    }
                    else
                    {
                        this._ctlExecutionRole.SelectedIndex = 0;
                    }
                }
                else
                {
                    var finalErrorMessage = "Failed to retrieve list of IAM roles and policies. Your profile must have the permissions iam:ListRoles and iam:ListPolicies.";

                    foreach (var message in errorMessages)
                    {
                        finalErrorMessage += $"\n\n  {message}";
                    }

                    ToolkitFactory.Instance.ShellProvider.ShowError("Loading Roles Error", finalErrorMessage);
                }
            }
        }

        private IAMRolePicker IAMPicker
        {
            get { return this._ctlIAMRolePicker; }
        }

        public Amazon.IdentityManagement.Model.Role SelectedRole
        {
            get
            {
                if (IAMPicker == null)
                    return null;

                return IAMPicker.SelectedRole;
            }
        }

        public Amazon.IdentityManagement.Model.ManagedPolicy SelectedManagedPolicy
        {
            get
            {
                if (IAMPicker == null || IAMPicker.SelectedManagedPolicy == null)
                    return null;

                return IAMPicker.SelectedManagedPolicy;
            }
        }

        Dictionary<string, TaskDefinition> _existingTaskDefinitions = new Dictionary<string, TaskDefinition>();
        void UpdateExistingTaskDefinition()
        {
            var currentTaskDefinitionText = !string.IsNullOrWhiteSpace(this._ctlTaskDefinitionPicker.Text) && this._ctlTaskDefinitionPicker.SelectedValue == null ? this._ctlTaskDefinitionPicker.Text : null;
            this._ctlTaskDefinitionPicker.Items.Clear();
            this._ctlTaskDefinitionPicker.Items.Add(ECSWizardUtils.CREATE_NEW_TEXT);
            this._existingTaskDefinitions.Clear();

            try
            {
                var account = PageController.HostingWizard[PublishContainerToAWSWizardProperties.UserAccount] as AccountViewModel;
                var region = PageController.HostingWizard[PublishContainerToAWSWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;

                Task task1 = Task.Run(() =>
                {
                    var items = new List<string>();
                    try
                    {
                        using (var ecsClient = account.CreateServiceClient<AmazonECSClient>(region.GetEndpoint(RegionEndPointsManager.ECS_ENDPOINT_LOOKUP)))
                        {
                            var response = new ListTaskDefinitionFamiliesResponse();
                            do
                            {
                                var request = new ListTaskDefinitionFamiliesRequest() { NextToken = response.NextToken, Status = TaskDefinitionFamilyStatus.ACTIVE };

                                response = ecsClient.ListTaskDefinitionFamilies(request);

                                foreach (var family in response.Families)
                                {
                                    items.Add(family);
                                }
                            } while (!string.IsNullOrEmpty(response.NextToken));
                        }
                    }
                    catch(Exception e)
                    {
                        this.PageController.HostingWizard.SetPageError("Error listing existing task definition families: " + e.Message);
                    }

                    ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() =>
                    {
                        foreach (var family in items.OrderBy(x => x))
                        {
                            this._ctlTaskDefinitionPicker.Items.Add(family);
                        }

                        var previousValue = this.PageController.HostingWizard[PublishContainerToAWSWizardProperties.TaskDefinition] as string;
                        if (!string.IsNullOrWhiteSpace(previousValue) && items.Contains(previousValue))
                            this._ctlTaskDefinitionPicker.SelectedItem = previousValue;
                        else
                        {
                            if (_initialTaskDefinitionLoad && PageController.HostingWizard[PublishContainerToAWSWizardProperties.SafeProjectName] is string)
                            {
                                _initialTaskDefinitionLoad = false;
                                this._ctlTaskDefinitionPicker.Text = ((string)PageController.HostingWizard[PublishContainerToAWSWizardProperties.SafeProjectName]);
                            }
                            else
                            {
                                if (currentTaskDefinitionText != null && items.Contains(currentTaskDefinitionText))
                                    this._ctlTaskDefinitionPicker.SelectedValue = currentTaskDefinitionText;
                                else
                                    this._ctlTaskDefinitionPicker.SelectedIndex = 0;
                            }
                        }
                    }));
                });
            }
            catch (Exception e)
            {
                this.PageController.HostingWizard.SetPageError("Error refreshing existing ECS Task Definition: " + e.Message);
                LOGGER.Error("Error refreshing existing ECS Task Definition.", e);
            }
        }

        Dictionary<string, ContainerDefinition> _existingContainerDefinitions = new Dictionary<string, ContainerDefinition>();
        private void UpdateExistingContainers()
        {
            var currentTextValue = !string.IsNullOrWhiteSpace(this._ctlContainerPicker.Text) &&
                this._ctlContainerPicker.SelectedValue == null ? this._ctlContainerPicker.Text : null;

            this._ctlContainerPicker.Items.Clear();
            this._ctlContainerPicker.Items.Add(ECSWizardUtils.CREATE_NEW_TEXT);
            this._existingContainerDefinitions.Clear();

            try
            {
                if (this.CreateTaskDefinition)
                    return;

                var taskDefinitionFamily = this._ctlTaskDefinitionPicker.SelectedItem as string;

                var account = PageController.HostingWizard[PublishContainerToAWSWizardProperties.UserAccount] as AccountViewModel;
                var region = PageController.HostingWizard[PublishContainerToAWSWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;

                Task task1 = Task.Run(() =>
                {
                    var items = new List<string>();
                    TaskDefinition taskDefinition = null;
                    try
                    {
                        using (var ecsClient = account.CreateServiceClient<AmazonECSClient>(region.GetEndpoint(RegionEndPointsManager.ECS_ENDPOINT_LOOKUP)))
                        {
                            taskDefinition = ecsClient.DescribeTaskDefinition(new DescribeTaskDefinitionRequest
                            {
                                TaskDefinition = taskDefinitionFamily
                            }).TaskDefinition;


                            foreach (var container in taskDefinition.ContainerDefinitions)
                            {
                                items.Add(container.Name);
                                this._existingContainerDefinitions[container.Name] = container;
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        this.PageController.HostingWizard.SetPageError("Error describing existing task definition: " + e.Message);
                    }

                    ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() =>
                    {
                        foreach (var container in items.OrderBy(x => x))
                        {
                            this._ctlContainerPicker.Items.Add(container);
                        }

                        var previousValue = this.PageController.HostingWizard[PublishContainerToAWSWizardProperties.Container] as string;
                        if (!string.IsNullOrWhiteSpace(previousValue) && items.Contains(previousValue))
                            this._ctlContainerPicker.SelectedItem = previousValue;
                        else
                        {
                            if (currentTextValue != null && items.Contains(currentTextValue))
                                this._ctlContainerPicker.SelectedValue = currentTextValue;
                            else
                                this._ctlContainerPicker.SelectedIndex = 0;
                        }

                        if (taskDefinition != null)
                        {
                            this._ctlIAMRolePicker.SelectExistingRole(taskDefinition.TaskRoleArn);
                        }
                    }));
                });
            }
            catch (Exception e)
            {
                this.PageController.HostingWizard.SetPageError("Error refreshing existing ECS Task Definition Container: " + e.Message);
                LOGGER.Error("Error refreshing existing ECS Task Definition Container.", e);
            }
        }

        public bool AllRequiredFieldsAreSet
        {
            get
            {
                if (!this.CreateTaskDefinition && string.IsNullOrWhiteSpace(this.TaskDefinition))
                    return false;
                if (this.CreateTaskDefinition && string.IsNullOrWhiteSpace(this.NewTaskDefinitionName))
                    return false;
                if (!this.CreateContainer && string.IsNullOrWhiteSpace(this.Container))
                    return false;
                if (this.CreateContainer && string.IsNullOrWhiteSpace(this.NewContainerName))
                    return false;
                if (this.PageController.HostingWizard.IsFargateLaunch() && string.IsNullOrEmpty(this.TaskExecutionRole) && !this.CreateNewTaskExecutionRole)
                    return false;

                if (!this.PageController.HostingWizard.IsFargateLaunch() && !this.MemoryHardLimit.HasValue && !this.MemorySoftLimit.HasValue)
                    return false;

                return true;
            }
        }

        public string TaskDefinition
        {
            get
            {
                if (this.CreateTaskDefinition)
                    return null;

                return this._ctlTaskDefinitionPicker.SelectedValue as string;
            }
            set { this._ctlTaskDefinitionPicker.SelectedValue = value; }
        }

        public bool CreateTaskDefinition
        {
            get { return this._ctlTaskDefinitionPicker.SelectedIndex == 0; }
        }

        string _newTaskDefinitionName;
        public string NewTaskDefinitionName
        {
            get { return this._newTaskDefinitionName; }
            set
            {
                this._newTaskDefinitionName = value;
                NotifyPropertyChanged("NewTaskDefinitionName");
            }
        }


        private void _ctlTaskDefinitionPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("TaskDefinition");
            UpdateExistingContainers();

            if (this._ctlTaskDefinitionPicker.SelectedIndex > 0)
            {
                this._ctlNewTaskDefinition.Visibility = Visibility.Collapsed;
                this._ctlNewTaskDefinition.IsEnabled = false;
                this._ctlContainerPicker.IsEnabled = true;
            }
            else
            {
                this._ctlNewTaskDefinition.Visibility = Visibility.Visible;
                this._ctlNewTaskDefinition.IsEnabled = true;

                if(!this._ctlTaskDefinitionPicker.Items.Contains(PageController.HostingWizard[PublishContainerToAWSWizardProperties.SafeProjectName] as string))
                    this._ctlNewTaskDefinition.Text = PageController.HostingWizard[PublishContainerToAWSWizardProperties.SafeProjectName] as string;

                this._ctlContainerPicker.SelectedIndex = 0;
                this._ctlContainerPicker.IsEnabled = false;

                if (!this._ctlContainerPicker.Items.Contains(PageController.HostingWizard[PublishContainerToAWSWizardProperties.SafeProjectName] as string))
                    this._ctlNewContainer.Text = PageController.HostingWizard[PublishContainerToAWSWizardProperties.SafeProjectName] as string;
            }
        }

        public string Container
        {
            get
            {
                if (this.CreateContainer)
                    return null;

                return this._ctlContainerPicker.SelectedValue as string;
            }
            set { this._ctlContainerPicker.SelectedValue = value; }
        }

        public bool CreateContainer
        {
            get { return this._ctlContainerPicker.SelectedIndex == 0; }
        }

        string _newContainerName;
        public string NewContainerName
        {
            get { return this._newContainerName; }
            set
            {
                this._newContainerName = value;
                NotifyPropertyChanged("NewContainerName");
            }
        }

        private void _ctlContainerPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string containerName = null;
            if (e.AddedItems.Count == 1)
                containerName = e.AddedItems[0] as string;

            NotifyPropertyChanged("Container");

            if (this._ctlContainerPicker.SelectedIndex > 0)
            {
                this._ctlNewContainer.Visibility = Visibility.Collapsed;
                this._ctlNewContainer.IsEnabled = false;

                ContainerDefinition container;
                if(this._existingContainerDefinitions.TryGetValue(containerName, out container))
                {
                    if (container.Memory > 0)
                        this._ctlMemoryHardLimit.Text = container.Memory.ToString();
                    else
                        this._ctlMemoryHardLimit.Text = null;

                    if (container.MemoryReservation > 0)
                        this._ctlMemorySoftLimit.Text = container.MemoryReservation.ToString();
                    else
                        this._ctlMemorySoftLimit.Text = null;

                    this.PortMappings.Clear();
                    foreach(var mapping in container.PortMappings)
                    {
                        this.PortMappings.Add(new PortMappingItem {HostPort = mapping.HostPort, ContainerPort = mapping.ContainerPort });
                    }

                    this.EnvironmentVariables.Clear();
                    foreach (var variable in container.Environment)
                    {
                        this.EnvironmentVariables.Add(new EnvironmentVariableItem { Variable = variable.Name, Value = variable.Value });
                    }
                }
            }
            else
            {
                this._ctlNewContainer.Visibility = Visibility.Visible;
                this._ctlNewContainer.IsEnabled = true;
                if (!this._ctlContainerPicker.Items.Contains(PageController.HostingWizard[PublishContainerToAWSWizardProperties.SafeProjectName] as string))
                    this._ctlNewContainer.Text = PageController.HostingWizard[PublishContainerToAWSWizardProperties.SafeProjectName] as string;
            }
        }

        int? _memorySoftLimit;
        public int? MemorySoftLimit
        {
            get { return _memorySoftLimit; }
            set
            {
                _memorySoftLimit = value;
                NotifyPropertyChanged("MemorySoftLimit");
            }
        }

        int? _memoryHardLimit;
        public int? MemoryHardLimit
        {
            get { return _memoryHardLimit; }
            set
            {
                _memoryHardLimit = value;
                NotifyPropertyChanged("MemoryHardLimit");
            }
        }


        public ObservableCollection<PortMappingItem> PortMappings { get; private set; }

        private void AddPortMapping_Click(object sender, RoutedEventArgs e)
        {
            PortMappings.Add(new PortMappingItem());

            var grid = this.PageController.HostingWizard.IsFargateLaunch() ? this._ctlFargatePortMappings : this._ctlEC2PortMappings;
            DataGridHelper.PutCellInEditMode(grid, this.PortMappings.Count - 1, 0);

            // todo: usability tweak here - put focus into the new key cell...
        }

        private void RemovePortMapping_Click(object sender, RoutedEventArgs e)
        {
            var grid = this.PageController.HostingWizard.IsFargateLaunch() ? this._ctlFargatePortMappings : this._ctlEC2PortMappings;
            PortMappingItem cellData = grid.CurrentCell.Item as PortMappingItem;
            for (int i = PortMappings.Count - 1; i >= 0; i--)
            {
                if (PortMappings[i].HostPort == cellData.HostPort)
                {
                    PortMappings.RemoveAt(i);
                    NotifyPropertyChanged("PortMappings");
                    return;
                }
            }
        }

        // used to trap attempts to create a duplicate variable
        private void _ctlPortMappings_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Cancel)
                return;

            TextBox editBox = e.EditingElement as TextBox;
            if (editBox == null)
            {
                LOGGER.ErrorFormat("Expected but did not receive TextBox EditingElement type for CellEditEnding event at row {0} column {1}; cannot validate for dupes.",
                                    e.Row.GetIndex(), e.Column.DisplayIndex);
                return;
            }

            string pendingEntry = editBox.Text;

            int pendingPort;
            if(!int.TryParse(pendingEntry, out pendingPort))
            {
                e.Cancel = true;
                MessageBox.Show(string.Format("A port must be a non-zero integer '{0}'.", pendingEntry),
                                "Invalid Port", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var grid = this.PageController.HostingWizard.IsFargateLaunch() ? this._ctlFargatePortMappings : this._ctlEC2PortMappings;
            PortMappingItem cellData = grid.CurrentCell.Item as PortMappingItem;
            if (cellData != null)
            {
                foreach (PortMappingItem ev in PortMappings)
                {
                    if (ev != cellData && ev.HostPort == pendingPort)
                    {
                        e.Cancel = true;
                        MessageBox.Show(string.Format("A value already exists for variable '{0}'.", pendingEntry),
                                        "Duplicate Variable", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            NotifyPropertyChanged("PortMappings");
        }

        public ObservableCollection<EnvironmentVariableItem> EnvironmentVariables { get; private set; }

        private void AddEnvironmentVariable_Click(object sender, RoutedEventArgs e)
        {
            EnvironmentVariables.Add(new EnvironmentVariableItem());
            DataGridHelper.PutCellInEditMode(this._ctlEnvironmentVariables, this.EnvironmentVariables.Count - 1, 0);
        }

        private void RemoveEnvironmentVariable_Click(object sender, RoutedEventArgs e)
        {
            EnvironmentVariableItem cellData = _ctlEnvironmentVariables.CurrentCell.Item as EnvironmentVariableItem;
            for (int i = EnvironmentVariables.Count - 1; i >= 0; i--)
            {
                if (EnvironmentVariables[i].Variable == cellData.Variable)
                {
                    EnvironmentVariables.RemoveAt(i);
                    NotifyPropertyChanged("EnvironmentVariables");
                    return;
                }
            }
        }

        // used to trap attempts to create a duplicate variable
        private void _ctlEnvironmentVariables_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Cancel)
                return;

            TextBox editBox = e.EditingElement as TextBox;
            if (editBox == null)
            {
                LOGGER.ErrorFormat("Expected but did not receive TextBox EditingElement type for CellEditEnding event at row {0} column {1}; cannot validate for dupes.",
                                    e.Row.GetIndex(), e.Column.DisplayIndex);
                return;
            }

            string pendingEntry = editBox.Text;

            EnvironmentVariableItem cellData = _ctlEnvironmentVariables.CurrentCell.Item as EnvironmentVariableItem;
            if (cellData != null)
            {
                foreach (EnvironmentVariableItem ev in EnvironmentVariables)
                {
                    if (ev != cellData && ev.Variable == pendingEntry)
                    {
                        e.Cancel = true;
                        MessageBox.Show(string.Format("A value already exists for variable '{0}'.", pendingEntry),
                                        "Duplicate Variable", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            NotifyPropertyChanged("EnvironmentVariable3s");
        }

        public bool CreateNewTaskExecutionRole
        {
            get { return this._ctlExecutionRole.SelectedIndex == 0; }
        }

        public string TaskExecutionRole
        {
            get
            {
                if (this.CreateNewTaskExecutionRole)
                    return null;

                return this._ctlExecutionRole.SelectedValue as string;
            }
            set
            {
                this._ctlExecutionRole.SelectedValue = value;
            }
        }


        private void _ctlExecutionRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("CreateNewTaskExecutionRole");
            NotifyPropertyChanged("TaskExecutionRole");
        }
    }

    public class PortMappingItem
    {
        public int? HostPort { get; set; }
        public int? ContainerPort { get; set; }
    }

    public class EnvironmentVariableItem
    {
        public string Variable { get; set; }
        public string Value { get; set; }
    }
}

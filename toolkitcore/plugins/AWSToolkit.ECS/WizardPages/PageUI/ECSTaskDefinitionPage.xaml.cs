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

namespace Amazon.AWSToolkit.ECS.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for ECSTaskDefinitionPage.xaml
    /// </summary>
    public partial class ECSTaskDefinitionPage : BaseAWSUserControl, INotifyPropertyChanged
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ECSTaskDefinitionPage));

        public ECSTaskDefinitionPageController PageController { get; private set; }

        public ECSTaskDefinitionPage()
        {
            InitializeComponent();

            PortMappings = new ObservableCollection<PortMappingItem>();
            PortMappings.Add(new PortMappingItem { HostPort = 80, ContainerPort = 80 });

            DataContext = this;
        }

        public ECSTaskDefinitionPage(ECSTaskDefinitionPageController pageController)
            : this()
        {
            PageController = pageController;

            UpdateExistingTaskDefinition();
        }

        void UpdateExistingTaskDefinition()
        {
            this._ctlTaskDefinitionPicker.Items.Clear();
            this._ctlContainerPicker.Items.Clear();

            try
            {
                var account = PageController.HostingWizard[PublishContainerToAWSWizardProperties.UserAccount] as AccountViewModel;
                var region = PageController.HostingWizard[PublishContainerToAWSWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;

                Task task1 = Task.Run(() =>
                {
                    var items = new List<string>();
                    using (var ecsClient = account.CreateServiceClient<AmazonECSClient>(region.GetEndpoint(Constants.ECS_ENDPOINT_LOOKUP)))
                    {
                        var response = new ListTaskDefinitionFamiliesResponse();
                        do
                        {
                            var request = new ListTaskDefinitionFamiliesRequest() { NextToken = response.NextToken };

                            response = ecsClient.ListTaskDefinitionFamilies(request);

                            foreach (var family in response.Families)
                            {
                                items.Add(family);
                            }
                        } while (!string.IsNullOrEmpty(response.NextToken));
                    }

                    ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() =>
                    {
                        foreach (var family in items.OrderBy(x => x))
                        {
                            this._ctlTaskDefinitionPicker.Items.Add(family);
                        }

                        this._ctlTaskDefinitionPicker.Text = "";
                    }));
                });
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing existing ECS Task Definition.", e);
            }
        }

        private void UpdateExistingContainers()
        {
            this._ctlContainerPicker.Items.Clear();
            try
            {
                if (this._ctlTaskDefinitionPicker.SelectedItem == null)
                    return;

                var taskDefinitionFamily = this._ctlTaskDefinitionPicker.SelectedItem as string;

                var account = PageController.HostingWizard[PublishContainerToAWSWizardProperties.UserAccount] as AccountViewModel;
                var region = PageController.HostingWizard[PublishContainerToAWSWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;

                Task task1 = Task.Run(() =>
                {
                    var items = new List<string>();
                    using (var ecsClient = account.CreateServiceClient<AmazonECSClient>(region.GetEndpoint(Constants.ECS_ENDPOINT_LOOKUP)))
                    {
                        var response = ecsClient.DescribeTaskDefinition(new DescribeTaskDefinitionRequest
                        {
                            TaskDefinition = taskDefinitionFamily
                        });

                        foreach(var container in response.TaskDefinition.ContainerDefinitions)
                        {
                            items.Add(container.Name);
                        }
                    }

                    ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() =>
                    {
                        foreach (var container in items.OrderBy(x => x))
                        {
                            this._ctlContainerPicker.Items.Add(container);
                        }

                        this._ctlContainerPicker.Text = "";
                    }));
                });
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing existing ECS Task Definition Container.", e);
            }
        }

        public bool AllRequiredFieldsAreSet
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.TaskDefinition))
                    return false;
                if (string.IsNullOrWhiteSpace(this.Container))
                    return false;

                if (!this.MemoryHardLimit.HasValue && !this.MemorySoftLimit.HasValue)
                    return false;

                return true;
            }
        }

        public string TaskDefinition
        {
            get { return this._ctlTaskDefinitionPicker.Text; }
            set { this._ctlTaskDefinitionPicker.Text = value; }
        }

        private void _ctlTaskDefinitionPicker_TextChanged(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged("TaskDefinition");
        }

        private void _ctlTaskDefinitionPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("TaskDefinition");
            UpdateExistingContainers();
        }

        public string Container
        {
            get { return this._ctlContainerPicker.Text; }
            set { this._ctlContainerPicker.Text = value; }
        }

        private void _ctlContainerPicker_TextChanged(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged("Container");
        }

        private void _ctlContainerPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("Container");
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
            // todo: usability tweak here - put focus into the new key cell...
        }

        private void RemovePortMapping_Click(object sender, RoutedEventArgs e)
        {
            PortMappingItem cellData = _ctlPortMappings.CurrentCell.Item as PortMappingItem;
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


            PortMappingItem cellData = _ctlPortMappings.CurrentCell.Item as PortMappingItem;
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
    }

    public class PortMappingItem
    {
        public int? HostPort { get; set; }
        public int? ContainerPort { get; set; }
    }
}

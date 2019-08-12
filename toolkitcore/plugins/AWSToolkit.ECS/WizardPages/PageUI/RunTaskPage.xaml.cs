using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.ECS.WizardPages.PageControllers;
using log4net;
using System.ComponentModel;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.ECS.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for RunTaskPage.xaml
    /// </summary>
    public partial class RunTaskPage : BaseAWSUserControl, INotifyPropertyChanged
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ScheduleTaskPage));

        public RunTaskPageController PageController { get; }

        public RunTaskPage()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public RunTaskPage(RunTaskPageController pageController)
            : this()
        {
            PageController = pageController;

            this._ctlPlacementTemplate.ItemsSource = ECSWizardUtils.PlacementTemplates.Options;
            this._ctlPlacementTemplate.SelectedIndex = 0;
            LoadPreviousValues();

        }

        private void LoadPreviousValues()
        {
            if (this.PageController.HostingWizard[PublishContainerToAWSWizardProperties.DesiredCount] is int)
                this.DesiredCount = (int)this.PageController.HostingWizard[PublishContainerToAWSWizardProperties.DesiredCount];
            else
                this.DesiredCount = 1;

            if (this.PageController.HostingWizard[PublishContainerToAWSWizardProperties.TaskGroup] is string)
                this.TaskGroup = (string)this.PageController.HostingWizard[PublishContainerToAWSWizardProperties.TaskGroup];
        }

        public void PageActivated()
        {
            if (!this.PageController.HostingWizard.IsFargateLaunch())
            {
                this._ctlPlacementTemplate.IsEnabled = true;
                this._ctlPlacementTemplate.Visibility = Visibility.Visible;
                this._ctlPlacementDescription.Visibility = Visibility.Visible;
                this._ctlPlacementLabel.Visibility = Visibility.Visible;
            }
            else
            {
                this._ctlPlacementTemplate.IsEnabled = false;
                this._ctlPlacementTemplate.Visibility = Visibility.Collapsed;
                this._ctlPlacementDescription.Visibility = Visibility.Collapsed;
                this._ctlPlacementLabel.Visibility = Visibility.Collapsed;
            }
        }

        public bool AllRequiredFieldsAreSet
        {
            get
            {
                if (this.DesiredCount.GetValueOrDefault() <= 0)
                    return false;

                return true;
            }
        }

        int? _desiredCount;
        public int? DesiredCount
        {
            get => this._desiredCount;
            set
            {
                this._desiredCount = value;
                NotifyPropertyChanged("DesiredCount");
            }
        }

        string _taskGroup;
        public string TaskGroup
        {
            get => this._taskGroup;
            set
            {
                this._taskGroup = value;
                NotifyPropertyChanged("TaskGroup");
            }
        }

        public ECSWizardUtils.PlacementTemplates PlacementTemplate => this._ctlPlacementTemplate.SelectedItem as ECSWizardUtils.PlacementTemplates;

        public bool IsPlacementTemplateEnabled
        {
            get => this._ctlPlacementTemplate.IsEnabled;
            set => this._ctlPlacementTemplate.IsEnabled = value;
        }



        private void _ctlPlacementTemplate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("PlacementTemplate");
            if(e.AddedItems.Count > 0)
            {
                var item = e.AddedItems[0] as ECSWizardUtils.PlacementTemplates;
                this._ctlPlacementDescription.Text = item.Description;
            }
        }
    }
}

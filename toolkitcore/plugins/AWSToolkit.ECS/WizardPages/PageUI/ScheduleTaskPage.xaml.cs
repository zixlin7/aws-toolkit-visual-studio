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
using Amazon.AWSToolkit.CommonUI.WizardFramework;

using static Amazon.AWSToolkit.ECS.WizardPages.ECSWizardUtils;
using System.Windows.Navigation;
using System.Diagnostics;

namespace Amazon.AWSToolkit.ECS.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for ScheduleTaskPage.xaml
    /// </summary>
    public partial class ScheduleTaskPage : BaseAWSUserControl, INotifyPropertyChanged
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ECSServicePage));

        public ScheduleTaskPageController PageController { get; private set; }

        public ScheduleTaskPage()
        {
            InitializeComponent();
            DataContext = this;
        }

        public ScheduleTaskPage(ScheduleTaskPageController pageController)
            : this()
        {
            PageController = pageController;


            this.RunIntervalValue = 10;
            
            this._ctlRunIntervalUnit.Items.Add("minute(s)");
            this._ctlRunIntervalUnit.Items.Add("hour(s)");
            this._ctlRunIntervalUnit.Items.Add("day(s)");
            this._ctlRunIntervalUnit.SelectedIndex = 0;

            this.CronExpression = "cron(0 10 * * ? *)";

            IsRunTypeFixedInterval = true;
            IsRunTypeCronExpression = false;
        }

        public bool AllRequiredFieldsAreSet
        {
            get
            {
                return true;
            }
        }

        public string ScheduleRule
        {
            get { return this._ctlScheduleRule.Text; }
            set { this._ctlScheduleRule.Text = value; }
        }

        private void _ctlScheduleRule_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("ScheduleRule");
        }

        public bool CreateNewScheduleRule
        {
            get { return this._ctlScheduleRule.SelectedIndex == 0; }
        }

        string _newScheduleRule;
        public string NewScheduleRule
        {
            get { return this._newScheduleRule; }
            set
            {
                this._newScheduleRule = value;
                NotifyPropertyChanged("NewScheduleRule");
            }
        }

        bool? _isRunTypeCronExpression;
        public bool? IsRunTypeCronExpression
        {
            get { return this._isRunTypeCronExpression; }
            set
            {
                this._isRunTypeCronExpression = value;
                NotifyPropertyChanged("IsRunTypeCronExpression");
            }
        }

        bool? _isRunTypeFixedInterval;
        public bool? IsRunTypeFixedInterval
        {
            get { return this._isRunTypeFixedInterval; }
            set
            {
                this._isRunTypeFixedInterval = value;
                NotifyPropertyChanged("IsRunTypeFixedInterval");
            }
        }

        int? _runIntervalValue;
        public int? RunIntervalValue
        {
            get { return this._runIntervalValue; }
            set
            {
                this._runIntervalValue = value;
                NotifyPropertyChanged("RunIntervalValue");
            }
        }

        string _runIntervalUnit;
        public string RunIntervalUnit
        {
            get { return this._runIntervalUnit; }
            set
            {
                this._runIntervalUnit = value;
                NotifyPropertyChanged("RunIntervalUnit");
            }
        }

        string _cronExpression;
        public string CronExpression
        {
            get { return this._cronExpression; }
            set
            {
                this._cronExpression = value;
                NotifyPropertyChanged("CronExpression");
            }
        }


        private void onCronLearnMoreClick(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                var uri = e.Uri.ToString();
                if (uri.EndsWith("*"))
                    uri = uri.Substring(0, uri.Length - 1);
                Process.Start(new ProcessStartInfo(uri));
                e.Handled = true;
            }
            catch (Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error navigating to CloudWatch Events user guid: " + ex.Message);
            }
        }


        public string Target
        {
            get { return this._ctlTarget.Text; }
            set { this._ctlTarget.Text = value; }
        }

        private void _ctlTarget_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("Target");
        }

        public bool CreateNewTarget
        {
            get { return this._ctlTarget.SelectedIndex == 0; }
        }

        string _newTarget;
        public string NewTarget
        {
            get { return this._newTarget; }
            set
            {
                this._newTarget = value;
                NotifyPropertyChanged("NewTarget");
            }
        }

        public string CloudWatchEventIAMRole
        {
            get { return this._ctlCloudWatchEventIAMRole.Text; }
            set { this._ctlCloudWatchEventIAMRole.Text = value; }
        }

        private void _ctlCloudWatchEventIAMRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("CloudWatchEventIAMRole");
        }

        public bool CreateNewIAMRole
        {
            get { return this._ctlCloudWatchEventIAMRole.SelectedIndex == 0; }
        }

        Visibility _fixedIntervalVisiblity = Visibility.Visible;
        public Visibility FixedIntervalVisiblity
        {
            get { return this._fixedIntervalVisiblity; }
            set
            {
                this._fixedIntervalVisiblity = value;
                NotifyPropertyChanged("FixedIntervalVisiblity");
            }
        }

        Visibility _cronExpressionVisiblity = Visibility.Collapsed;
        public Visibility CronExpressionVisiblity
        {
            get { return this._cronExpressionVisiblity; }
            set
            {
                this._cronExpressionVisiblity = value;
                NotifyPropertyChanged("CronExpressionVisiblity");
            }
        }

        private void RunType_Click(object sender, RoutedEventArgs e)
        {
            this.FixedIntervalVisiblity = this._ctlRunTypeFixedInterval.IsChecked.GetValueOrDefault() ? Visibility.Visible : Visibility.Collapsed;
            this.CronExpressionVisiblity = this._ctlRunTypeCronExpression.IsChecked.GetValueOrDefault() ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}

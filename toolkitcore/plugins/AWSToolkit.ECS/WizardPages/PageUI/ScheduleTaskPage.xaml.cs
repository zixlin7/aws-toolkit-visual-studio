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
using Amazon.IdentityManagement.Model;

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

            foreach (var item in RunIntervalUnitItem.ValidValues)
                this._ctlRunIntervalUnit.Items.Add(item);

            this.RunIntervalUnit = this._ctlRunIntervalUnit.Items[0] as RunIntervalUnitItem;

            this.CronExpression = "cron(0 10 * * ? *)";

            IsRunTypeFixedInterval = true;
            IsRunTypeCronExpression = false;

            this.DesiredCount = 1;
        }



        public bool AllRequiredFieldsAreSet
        {
            get
            {
                return true;
            }
        }

        public void InitializeWithNewCluster()
        {
            UpdateExistingResources();
        }

        public string ScheduleRule
        {
            get { return this._ctlScheduleRule.Text; }
            set { this._ctlScheduleRule.Text = value; }
        }

        private void _ctlScheduleRule_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("ScheduleRule");
            string ruleName = null;
            if (e.AddedItems.Count == 1)
                ruleName = e.AddedItems[0] as string;

            UpdateResourcesForRuleSelectionChange(ruleName);
            if (this._ctlScheduleRule.SelectedIndex != 0)
            {
                this._ctlNewScheduleRuleName.Visibility = Visibility.Collapsed;
                this._ctlNewScheduleRuleName.IsEnabled = false;

                this._ctlTarget.IsEnabled = true;
            }
            else
            {
                this._ctlNewScheduleRuleName.Visibility = Visibility.Visible;
                this._ctlNewScheduleRuleName.IsEnabled = true;
                this._ctlNewScheduleRuleName.Text = PageController.HostingWizard[PublishContainerToAWSWizardProperties.SafeProjectName] as string;

                this._ctlNewTarget.Visibility = Visibility.Visible;
                this._ctlNewTarget.IsEnabled = true;
                this._ctlTarget.IsEnabled = false;
                this._ctlNewTarget.Text = PageController.HostingWizard[PublishContainerToAWSWizardProperties.SafeProjectName] as string;
            }
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

        RunIntervalUnitItem _runIntervalUnit;
        public RunIntervalUnitItem RunIntervalUnit
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
            if (this._ctlTarget.SelectedIndex != 0)
            {
                this._ctlNewTarget.Visibility = Visibility.Collapsed;
                this._ctlNewTarget.IsEnabled = false;
            }
            else
            {
                this._ctlNewTarget.Visibility = Visibility.Visible;
                this._ctlNewTarget.IsEnabled = true;
                this._ctlNewTarget.Text = PageController.HostingWizard[PublishContainerToAWSWizardProperties.SafeProjectName] as string;
            }
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

        int? _desiredCount;
        public int? DesiredCount
        {
            get { return this._desiredCount; }
            set
            {
                this._desiredCount = value;
                NotifyPropertyChanged("NewTarget");
            }
        }

        public string CloudWatchEventIAMRole
        {
            get { return this._ctlCloudWatchEventIAMRole.Text; }
            set { this._ctlCloudWatchEventIAMRole.Text = value; }
        }

        public string CloudWatchEventIAMRoleArn
        {
            get
            {
                Role role;
                if (this._existingRoles.TryGetValue(this.CloudWatchEventIAMRole, out role))
                    return role.Arn;

                return null;
            }
        }

        private void _ctlCloudWatchEventIAMRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("CloudWatchEventIAMRole");
        }

        public bool CreateCloudWatchEventIAMRole
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

        Dictionary<string, Role> _existingRoles = new Dictionary<string, Role>();

        CloudWatchEventHelper.ScheduleRulesState _scheduleRulesState;
        private void UpdateExistingResources()
        {
            this._ctlCloudWatchEventIAMRole.Items.Clear();
            this._ctlCloudWatchEventIAMRole.Items.Add(CREATE_NEW_TEXT);
            this._ctlCloudWatchEventIAMRole.SelectedIndex = 0;
            this._existingRoles.Clear();

            this._ctlScheduleRule.Items.Clear();
            this._ctlScheduleRule.Items.Add(CREATE_NEW_TEXT);

            try
            {
                Task.Run<List<string>>(() =>
                {
                    using (var client = CreateIAMClient(this.PageController.HostingWizard))
                    {
                        var roles = new List<string>();
                        var response = new ListRolesResponse();
                        do
                        {
                            var request = new ListRolesRequest() { Marker = response.Marker };
                            response = client.ListRoles(request);

                            var validRoles = RolePolicyFilter.FilterByAssumeRoleServicePrincipal(response.Roles, "events.amazonaws.com");
                            foreach (var role in validRoles)
                            {
                                roles.Add(role.RoleName);
                                this._existingRoles[role.RoleName] = role;
                            }
                        } while (!string.IsNullOrEmpty(response.Marker));
                        return roles;
                    }
                }).ContinueWith(t =>
                {
                    ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((System.Action)(() =>
                    {
                        foreach (var item in t.Result.OrderBy(x => x))
                        {
                            this._ctlCloudWatchEventIAMRole.Items.Add(item);
                        }

                        var previousValue = this.PageController.HostingWizard[PublishContainerToAWSWizardProperties.CloudWatchEventIAMRole] as string;
                        if (!string.IsNullOrWhiteSpace(previousValue) && t.Result.Contains(previousValue))
                            this._ctlCloudWatchEventIAMRole.SelectedItem = previousValue;
                        else
                        {
                            this._ctlCloudWatchEventIAMRole.SelectedIndex = this._ctlCloudWatchEventIAMRole.Items.Count > 1 ? 1 : 0;
                        }
                    }));
                });
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing existing IAM Roles.", e);
            }

            try
            {
                Task task1 = Task.Run(() =>
                {
                    var items = new List<string>();
                    using (var cweClient = CreateCloudWatchEventsClient(PageController.HostingWizard))
                    {
                        this._scheduleRulesState = CloudWatchEventHelper.FetchScheduleRuleState(cweClient, this.PageController.Cluster);
                    }

                    ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() =>
                    {
                        foreach (var ruleName in this._scheduleRulesState.RuleNames.OrderBy(x => x))
                        {
                            this._ctlScheduleRule.Items.Add(ruleName);
                        }


                        var previousValue = this.PageController.HostingWizard[PublishContainerToAWSWizardProperties.ScheduleTaskRuleName] as string;
                        if (!string.IsNullOrWhiteSpace(previousValue) && items.Contains(previousValue))
                            this._ctlScheduleRule.SelectedItem = previousValue;
                        else
                        {
                            this._ctlScheduleRule.SelectedIndex = this._ctlScheduleRule.Items.Count > 1 ? 1 : 0;
                        }
                    }));
                });

            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing existing CloudWatch Event rules.", e);
            }
        }

        public void UpdateResourcesForRuleSelectionChange(string ruleName)
        {
            this._ctlTarget.Items.Clear();
            this._ctlTarget.Items.Add(CREATE_NEW_TEXT);

            if (string.IsNullOrWhiteSpace(ruleName) || string.Equals(ruleName, CREATE_NEW_TEXT))
            {
                this._ctlTarget.SelectedIndex = 0;
                return;
            }

            var targets = this._scheduleRulesState.GetRuleTargets(ruleName);

            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((System.Action)(() =>
            {
                foreach (var item in targets.OrderBy(x => x.Id))
                {
                    this._ctlTarget.Items.Add(item.Id);
                }
            }));
        }
    }

    public class RunIntervalUnitItem
    {
        public static readonly RunIntervalUnitItem[] ValidValues = new RunIntervalUnitItem[]
        {
            new RunIntervalUnitItem("minute(s)", "minutes"),
            new RunIntervalUnitItem("hour(s)", "hours"),
            new RunIntervalUnitItem("day(s)", "days")
        };

        public RunIntervalUnitItem(string displayName, string systemName)
        {
            this.DisplayName = displayName;
            this.SystemName = systemName;
        }

        public string DisplayName { get; set; }
        public string SystemName { get; set; }

        public override string ToString() => this.DisplayName;
    }
}

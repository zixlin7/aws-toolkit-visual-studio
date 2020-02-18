using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.ECS.WizardPages.PageControllers;
using log4net;
using System;
using System.ComponentModel;
using System.Windows.Controls;

using Task = System.Threading.Tasks.Task;
using System.Collections.Generic;
using System.Linq;
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
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ScheduleTaskPage));

        public ScheduleTaskPageController PageController { get; }

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
                if (this.CreateNewScheduleRule && string.IsNullOrWhiteSpace(this.NewScheduleRule))
                    return false;
                if (!this.CreateNewScheduleRule && string.IsNullOrWhiteSpace(this.ScheduleRule))
                    return false;
                if (string.IsNullOrWhiteSpace(this.Target))
                    return false;
                if (this.DesiredCount.GetValueOrDefault() <= 0)
                    return false;
                if (this.IsRunTypeFixedInterval.GetValueOrDefault() && this.RunIntervalValue.GetValueOrDefault() <= 0)
                    return false;
                if (this.IsRunTypeCronExpression.GetValueOrDefault() && string.IsNullOrWhiteSpace(this.CronExpression))
                    return false;


                return true;
            }
        }

        public void InitializeWithNewCluster()
        {
            UpdateExistingResources();
        }

        public string ScheduleRule => this._ctlScheduleRule.SelectedItem as string;

        private void _ctlScheduleRule_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
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

            NotifyPropertyChanged("ScheduleRule");
        }

        public bool CreateNewScheduleRule => this._ctlScheduleRule.SelectedIndex == 0;

        string _newScheduleRule;
        public string NewScheduleRule
        {
            get => this._newScheduleRule;
            set
            {
                this._newScheduleRule = value;
                NotifyPropertyChanged("NewScheduleRule");
            }
        }

        bool? _isRunTypeCronExpression;
        public bool? IsRunTypeCronExpression
        {
            get => this._isRunTypeCronExpression;
            set
            {
                this._isRunTypeCronExpression = value;
                NotifyPropertyChanged("IsRunTypeCronExpression");
            }
        }

        bool? _isRunTypeFixedInterval;
        public bool? IsRunTypeFixedInterval
        {
            get => this._isRunTypeFixedInterval;
            set
            {
                this._isRunTypeFixedInterval = value;
                NotifyPropertyChanged("IsRunTypeFixedInterval");
            }
        }

        int? _runIntervalValue;
        public int? RunIntervalValue
        {
            get => this._runIntervalValue;
            set
            {
                this._runIntervalValue = value;
                NotifyPropertyChanged("RunIntervalValue");
            }
        }

        RunIntervalUnitItem _runIntervalUnit;
        public RunIntervalUnitItem RunIntervalUnit
        {
            get => this._runIntervalUnit;
            set
            {
                this._runIntervalUnit = value;
                NotifyPropertyChanged("RunIntervalUnit");
            }
        }

        string _cronExpression;
        public string CronExpression
        {
            get => this._cronExpression;
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
                ToolkitFactory.Instance.ShellProvider.ShowError("Error navigating to CloudWatch Events user guide: " + ex.Message);
            }
        }


        public string Target
        {
            get => this._ctlTarget.SelectedItem as string;
            set => this._ctlTarget.SelectedItem = value;
        }

        private void _ctlTarget_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
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
            NotifyPropertyChanged("Target");
        }

        public bool CreateNewTarget => this._ctlTarget.SelectedIndex == 0;

        string _newTarget;
        public string NewTarget
        {
            get => this._newTarget;
            set
            {
                this._newTarget = value;
                NotifyPropertyChanged("NewTarget");
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

        public string CloudWatchEventIAMRole
        {
            get => this._ctlCloudWatchEventIAMRole.SelectedItem as string;
            set => this._ctlCloudWatchEventIAMRole.SelectedItem = value;
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

        public bool CreateCloudWatchEventIAMRole => this._ctlCloudWatchEventIAMRole.SelectedIndex == 0;

        Visibility _fixedIntervalVisiblity = Visibility.Visible;
        public Visibility FixedIntervalVisiblity
        {
            get => this._fixedIntervalVisiblity;
            set
            {
                this._fixedIntervalVisiblity = value;
                NotifyPropertyChanged("FixedIntervalVisiblity");
            }
        }

        Visibility _cronExpressionVisiblity = Visibility.Collapsed;
        public Visibility CronExpressionVisiblity
        {
            get => this._cronExpressionVisiblity;
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
                        try
                        {
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
                        }
                        catch(Exception e)
                        {
                            this.PageController.HostingWizard.SetPageError("Error listing existing IAM roles: " + e.Message);
                        }
                        return roles;
                    }
                }).ContinueWith(t =>
                {
                    ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((System.Action)(() =>
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
                this.PageController.HostingWizard.SetPageError("Error listing existing IAM roles: " + e.Message);
                LOGGER.Error("Error refreshing existing IAM Roles.", e);
            }

            try
            {
                Task task1 = Task.Run(() =>
                {
                    var items = new List<string>();
                    try
                    {
                        using (var cweClient = CreateCloudWatchEventsClient(PageController.HostingWizard))
                        {
                            this._scheduleRulesState = CloudWatchEventHelper.FetchScheduleRuleState(cweClient, this.PageController.Cluster);
                            if(this._scheduleRulesState.LastException != null)
                            {
                                this.PageController.HostingWizard.SetPageError("Error fetching existing CloudWatch Events schedule rules: " + this._scheduleRulesState.LastException.Message);
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        this.PageController.HostingWizard.SetPageError("Error fetching existing CloudWatch Events schedule rules: " + e.Message);

                        // Create an empty version to avoid null pointer exceptions
                        this._scheduleRulesState = new CloudWatchEventHelper.ScheduleRulesState();
                    }

                    ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
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
                this.PageController.HostingWizard.SetPageError("Error refreshing existing CloudWatch Event rules: " + e.Message);
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

            ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((System.Action)(() =>
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
            new RunIntervalUnitItem("minute(s)", "minutes", "minute"),
            new RunIntervalUnitItem("hour(s)", "hours", "hour"),
            new RunIntervalUnitItem("day(s)", "days", "day")
        };

        public RunIntervalUnitItem(string displayName, string pluralSystemName, string singluarSystemName)
        {
            this.DisplayName = displayName;
            this.PluralSystemName = pluralSystemName;
            this.SingluarSystemName = singluarSystemName;
        }

        public string DisplayName { get; set; }
        private string PluralSystemName { get; set; }
        private string SingluarSystemName { get; set; }

        public string GetUnitName(int value)
        {
            if (value == 1)
                return this.SingluarSystemName;
            else
                return this.PluralSystemName;
        }

        public override string ToString() => this.DisplayName;
    }
}

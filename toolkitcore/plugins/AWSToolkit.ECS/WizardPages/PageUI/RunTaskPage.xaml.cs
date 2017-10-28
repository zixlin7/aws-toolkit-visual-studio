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
    /// Interaction logic for RunTaskPage.xaml
    /// </summary>
    public partial class RunTaskPage : BaseAWSUserControl, INotifyPropertyChanged
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ScheduleTaskPage));

        public RunTaskPageController PageController { get; private set; }

        public RunTaskPage()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public RunTaskPage(RunTaskPageController pageController)
            : this()
        {
            PageController = pageController;

            this.DesiredCount = 1;

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
            get { return this._desiredCount; }
            set
            {
                this._desiredCount = value;
                NotifyPropertyChanged("DesiredCount");
            }
        }

        string _taskGroup;
        public string TaskGroup
        {
            get { return this._taskGroup; }
            set
            {
                this._taskGroup = value;
                NotifyPropertyChanged("TaskGroup");
            }
        }
    }
}

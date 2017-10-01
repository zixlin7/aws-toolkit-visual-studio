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

namespace Amazon.AWSToolkit.ECS.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for ECSConfigurationPage.xaml
    /// </summary>
    public partial class ECSConfigurationPage : BaseAWSUserControl, INotifyPropertyChanged
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ECSConfigurationPage));

        public ECSConfigurationPageController PageController { get; private set; }

        public ECSConfigurationPage()
        {
            InitializeComponent();
            DataContext = this;
        }

        public ECSConfigurationPage(ECSConfigurationPageController pageController)
            : this()
        {
            PageController = pageController;
        }

        public bool AllRequiredFieldsAreSet
        {
            get
            {
                return true;
            }
        }
    }
}

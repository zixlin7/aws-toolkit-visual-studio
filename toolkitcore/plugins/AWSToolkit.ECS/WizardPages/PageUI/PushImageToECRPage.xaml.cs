using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.ECS.Controller;
using Amazon.AWSToolkit.ECS.WizardPages.PageControllers;
using log4net;
using System;

namespace Amazon.AWSToolkit.ECS.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for PushImageToECRPage.xaml
    /// </summary>
    public partial class PushImageToECRPage : BaseAWSUserControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(PushImageToECRPage));

        public PushImageToECRPageController PageController { get; private set; }

        public PushImageToECRPage()
        {
            InitializeComponent();
        }

        public PushImageToECRPage(PushImageToECRPageController pageController)
            : this()
        {
            PageController = pageController;
        }
    }
}

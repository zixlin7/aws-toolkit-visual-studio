using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.LaunchWizard.PageUI
{
    /// <summary>
    /// Interaction logic for InstanceTagsPage.xaml
    /// </summary>
    public partial class InstanceTagsPage 
    {
        public InstanceTagsPage()
        {
            InitializeComponent();
        }

        public InstanceTagsPage(IAWSWizardPageController controller)
            : this()
        {
            this.PageController = controller;
        }

        public IAWSWizardPageController PageController { get; set; }

        public ICollection<Tag> InstanceTags 
        {
            get { return _instanceTagsEditor.InstanceTags; }
            set { _instanceTagsEditor.InstanceTags = value; } 
        }

    }
}

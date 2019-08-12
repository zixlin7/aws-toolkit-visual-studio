using System.Collections.Generic;
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
            get => _instanceTagsEditor.InstanceTags;
            set => _instanceTagsEditor.InstanceTags = value;
        }

    }
}

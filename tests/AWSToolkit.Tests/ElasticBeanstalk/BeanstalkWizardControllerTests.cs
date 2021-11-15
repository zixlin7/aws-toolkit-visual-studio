using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Tests.Common.Wizard;

namespace AWSToolkit.Tests.ElasticBeanstalk
{
    public abstract class BeanstalkWizardControllerTests
    {
        protected IAWSWizard Wizard = new InMemoryAWSWizard();

        protected void SetProjectTypeTo(string projectType)
        {
            Wizard[DeploymentWizardProperties.SeedData.propkey_ProjectType] = projectType;
        }

        protected void TransferStateFromPageToWizard(IAWSWizardPageController controller,
            AWSWizardConstants.NavigationReason navigationReason = AWSWizardConstants.NavigationReason.finishPressed)
        {
            controller.PageDeactivating(navigationReason);
        }
    }
}

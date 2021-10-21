using System.ComponentModel;

using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Events;

namespace Amazon.AWSToolkit.Publish.Banner
{
    public class PublishBannerPropertyChangedHandler
    {
        private readonly PublishBannerViewModel _publishBanner;
        private readonly IAWSWizard _wizard;

        public PublishBannerPropertyChangedHandler(PublishBannerViewModel publishBanner, IAWSWizard wizard)
        {
            _publishBanner = publishBanner;
            _wizard = wizard;

            _publishBanner.PropertyChanged += LogExceptionAndForgetChangedHandler.Create(PublishBannerStateChanged);
        }

        private void PublishBannerStateChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_publishBanner.CloseCurrentPublishExperience):
                    HandleCloseCurrentPublishExperienceChange();
                    break;
            }
        }

        private void HandleCloseCurrentPublishExperienceChange()
        {
            if (_publishBanner.CloseCurrentPublishExperience)
            {
                _wizard.CancelRun();
            }
        }
    }
}

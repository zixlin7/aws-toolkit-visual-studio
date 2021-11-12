using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

using Amazon.AWSToolkit.CommonUI.WizardFramework;

using log4net;

namespace Amazon.AWSToolkit.Tests.Common.Wizard
{
    public class InMemoryAWSWizard : IAWSWizard
    {
        public string WizardID { get; }
        public string Title { get; set; }
        public string ActivePageTitle { get; }
        public string ActivePageDescription { get; }
        public bool DisplayHeader { get; set; }
        public bool AutoPollFinishEnablement { get; set; }

        public void SetNavigationEnablement(IAWSWizardPageController requestorPage,
            AWSWizardConstants.NavigationButtons button, bool enable)
        {
            throw new NotImplementedException();
        }

        public bool RequestFinishEnablement(IAWSWizardPageController requestorPage)
        {
            throw new NotImplementedException();
        }

        public void RegisterPageGroups(IEnumerable<string> groupNames)
        {
            throw new NotImplementedException();
        }

        public void RegisterPageControllers(IEnumerable<IAWSWizardPageController> pageControllers, int priority)
        {
            throw new NotImplementedException();
        }

        public object GetProperty(string key)
        {
            throw new NotImplementedException();
        }

        public T GetProperty<T>(string key)
        {
            throw new NotImplementedException();
        }

        public void SetProperty(string key, object value)
        {
            CollectedProperties[key] = value;
        }

        public void SetProperties(Dictionary<string, object> properties)
        {
            throw new NotImplementedException();
        }

        public bool IsPropertySet(string key) => CollectedProperties.ContainsKey(key);

        public bool Run()
        {
            throw new NotImplementedException();
        }

        public object this[string propertyKey]
        {
            get => CollectedProperties.TryGetValue(propertyKey, out var value) ? value : null;
            set => CollectedProperties[propertyKey] = value;
        }

        public Dictionary<string, object> CollectedProperties { get; } = new Dictionary<string, object>();

        public void SetNavigationButtonText(AWSWizardConstants.NavigationButtons button, string buttonText)
        {
            throw new NotImplementedException();
        }

        public void SetNavigationButtonUI(AWSWizardConstants.NavigationButtons button, string buttonText,
            Image buttonGlyph)
        {
            throw new NotImplementedException();
        }

        public void SetNavigationButtonVisibility(AWSWizardConstants.NavigationButtons button, bool isVisible)
        {
            throw new NotImplementedException();
        }

        public void SetNavigationPanelBackground(Brush navPanelBrush)
        {
            throw new NotImplementedException();
        }

        public void SetShortCircuitPage(string shortCircuitPageID)
        {
            throw new NotImplementedException();
        }

        public void ResetFirstActivePage()
        {
            throw new NotImplementedException();
        }

        public ILog Logger { get; }
        public Func<bool> CommitAction { get; set; }

        public void NotifyPropertyChanged(string propertyName)
        {
            throw new NotImplementedException();
        }

        public void CancelRun()
        {
            throw new NotImplementedException();
        }

        public void CancelRun(string propertyKey, object propertyValue)
        {
            throw new NotImplementedException();
        }

        public void SetPageError(string errorText)
        {
            throw new NotImplementedException();
        }

        public void NotifyForwardPagesReset(IAWSWizardPageController requestorPage)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Controls;

using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.EC2.LaunchWizard.PageUI;

using Amazon.AWSToolkit.EC2.Utils;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit.EC2.Model;

using Amazon.EC2;
using Amazon.EC2.Model;

using AMIImage = Amazon.EC2.Model.Image;

namespace Amazon.AWSToolkit.EC2.LaunchWizard.PageControllers
{
    public class AMIPageController : IAWSWizardPageController
    {
        AMIPage _pageUI;
        readonly Dictionary<CommonImageFilters, List<AMIImage>> _describeCache = new Dictionary<CommonImageFilters, List<AMIImage>>();
        ViewAMIsModel _model;
        bool _initialPageActivation = true;

        #region IAWSWizardPageController Members

        public string PageID
        {
            get { return GetType().FullName; }
        }

        public IAWSWizard HostingWizard { get; set; }

        public string PageGroup
        {
            get { return AWSWizardConstants.DefaultPageGroup; }
        }

        public string PageTitle
        {
            get { return "Image Selection"; }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public string PageDescription
        {
            get { return "Choose an Amazon Machine Image (AMI) to launch."; }
        }

        public void ResetPage()
        {

        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return !HostingWizard.IsPropertySet(LaunchWizardProperties.AMIOptions.propkey_SeedAMI);
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                CommonImageFilters initialFilter = CommonImageFilters.OWNED_BY_ME;
                if (HostingWizard[LaunchWizardProperties.AMIOptions.propkey_SelectedAMI] is ImageWrapper)
                    initialFilter = CommonImageFilters.AMAZON;

                _pageUI = new AMIPage(this, initialFilter);
                _pageUI.PropertyChanged += _pageUI_PagePropertyChanged;
                _pageUI.OnRequestImageListRefresh = this.RefreshImages;
                this.EC2Client = (HostingWizard[LaunchWizardProperties.Global.propkey_EC2RootModel] as FeatureViewModel).EC2Client;
                _pageUI.BindModel(Model);
            }

            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_initialPageActivation)
            {
                RefreshImages(true);
                _initialPageActivation = false;
            }

            HostingWizard.SetNavigationButtonText(AWSWizardConstants.NavigationButtons.Forward, "Next");
            TestForwardTransitionEnablement();
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason != AWSWizardConstants.NavigationReason.movingBack)
                StorePageData();
            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            // two cases - quick launch requested finish be enabled because of selection or wizard
            // launched onto one particular ami
            if ((HostingWizard.IsPropertySet(LaunchWizardProperties.AMIOptions.propkey_IsQuickLaunch)
                        && (bool)HostingWizard[LaunchWizardProperties.AMIOptions.propkey_IsQuickLaunch])
                    || HostingWizard.IsPropertySet(LaunchWizardProperties.AMIOptions.propkey_SeedAMI))
                return true;

            if (_pageUI != null)
                return IsForwardsNavigationAllowed;
            else
                return true;
        }

        public void TestForwardTransitionEnablement()
        {
            bool pageComplete = IsForwardsNavigationAllowed;
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, pageComplete);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, pageComplete);
        }

        public bool AllowShortCircuit()
        {
            // user may have gone forwards enough for Finish to be enabled, then come back
            // and changed something so re-save
            StorePageData();
            return true;
        }

        #endregion

        void StorePageData()
        {
            HostingWizard[LaunchWizardProperties.AMIOptions.propkey_SelectedAMI] = _pageUI.SelectedAMI;
        }

        bool IsForwardsNavigationAllowed
        {
            get
            {
                if (_pageUI == null)
                    return true;

                return _pageUI.SelectedAMI != null;
            }
        }

        ViewAMIsModel Model
        {
            get
            {
                if (_model == null)
                    _model = new ViewAMIsModel();
                return _model;
            }
        }

        IAmazonEC2 EC2Client { get; set; }

        void RefreshImages(bool fullRefresh)
        {
            if (fullRefresh)
                this._describeCache.Clear();

            CommonImageFilters commonFilter = this.Model.CommonImageFilter;
            List<AMIImage> images;
            if (!this._describeCache.TryGetValue(commonFilter, out images))
            {
                var request = new DescribeImagesRequest();

                if (this.Model.CommonImageFilter == CommonImageFilters.OWNED_BY_ME)
                    request.Owners.Add("self");
                else
                    request.Filters.AddRange(this.Model.CommonImageFilter.Filters);

                request.Filters.Add(new Filter() { Name = "image-type", Values = new List<string>() { "machine" } });
                try
                {
                    var response = this.EC2Client.DescribeImages(request);
                    images = response.Images;
                    this._describeCache[commonFilter] = images;
                }
                catch (Exception exc)
                {
                    HostingWizard.Logger.Error(GetType().FullName + ", exception in RefreshImages", exc);
                }
            }

            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() =>
            {
                this.Model.Images.Clear();
                if (images != null)
                {
                    var previousSelectedAMI = HostingWizard[LaunchWizardProperties.AMIOptions.propkey_SelectedAMI] as ImageWrapper;
                    ImageWrapper selectedAMI = null;
                    foreach (var image in images.OrderBy(x => x.ImageId.ToLower()))
                    {
                        var wrapper = new ImageWrapper(image);
                        if (passClientFilter(wrapper))
                        {
                            this.Model.Images.Add(wrapper);
                            if (previousSelectedAMI != null && wrapper.ImageId.Equals(previousSelectedAMI.ImageId))
                                selectedAMI = wrapper;
                        }
                    }

                    if (selectedAMI != null)
                    {
                        this._pageUI.SetSelectedAMI(selectedAMI);
                    }
                }
            }));
        }

        bool passClientFilter(ImageWrapper image)
        {
            if (this.Model.PlatformFilter == PlatformPicker.WINDOWS)
            {
                if (!image.IsWindowsPlatform)
                    return false;

            }
            else if (this.Model.PlatformFilter == PlatformPicker.LINUX)
            {
                if (image.IsWindowsPlatform)
                    return false;
            }

            if (!string.IsNullOrEmpty(this.Model.TextFilter))
            {
                try
                {
                    string textFilter = this.Model.TextFilter.ToLower();
                    if (image.NativeImage.ImageId.ToLower().Contains(textFilter))
                        return true;
                    if (image.Name.ToLower().Contains(textFilter))
                        return true;
                    if (!String.IsNullOrEmpty(image.NativeImage.Description) && image.NativeImage.Description.ToLower().Contains(textFilter))
                        return true;
                    if (image.FormattedPlatform.ToLower().Contains(textFilter))
                        return true;
                }
                catch (Exception exc)
                {
                    HostingWizard.Logger.Error(GetType().FullName + ", exception in passClientFilter", exc);
                }

                return false;
            }

            return true;
        }

        void _pageUI_PagePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }
    }
}

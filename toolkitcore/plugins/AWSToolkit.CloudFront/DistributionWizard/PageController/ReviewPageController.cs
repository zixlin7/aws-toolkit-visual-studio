using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.CloudFront.Model;
using Amazon.AWSToolkit.CloudFront.Controller;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CloudFront.DistributionWizard.PageUI;

namespace Amazon.AWSToolkit.CloudFront.DistributionWizard.PageController
{
    class ReviewPageController : IAWSWizardPageController
    {
        BaseDistributionConfigEditorController _controller;
        ReviewPage _pageUI;

        public ReviewPageController(BaseDistributionConfigEditorController controller)
        {
            this._controller = controller;
        }

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
            get { return "Review"; }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public string PageDescription
        {
            get
            {
                return this.IsStreamingDistribution 
                    ? "Review your selections and click Create to create your streaming distribution."
                    : "Review your selections and click Create to create your distribution.";
            }
        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
                _pageUI = new ReviewPage(this);

            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            HostingWizard.SetNavigationButtonVisibility(AWSWizardConstants.NavigationButtons.Back, true);
            this._pageUI.ClearPanels();

            if (this.IsStreamingDistribution)
                this.AddStreamingOriginReviewPanel();
            else
                this.AddDistributionOriginReviewPanel();

            this.AddLoggingReviewPanel();
            this.AddCNAMEReviewPanel();
            this.AddPrivateSettingsReviewPanel();

            HostingWizard.SetNavigationButtonText(AWSWizardConstants.NavigationButtons.Finish, "Create");
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason == AWSWizardConstants.NavigationReason.movingBack)
                HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, true);

            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            return true;
        }

        public void TestForwardTransitionEnablement()
        {
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, false);
        }

        public bool AllowShortCircuit()
        {
            return true;
        }

        #endregion

        public bool IsStreamingDistribution
        {
            get { return this._controller.BaseModel is StreamingDistributionConfigModel; }
        }

        void AddDistributionOriginReviewPanel()
        {
            StringBuilder sb = new StringBuilder();

            DistributionConfigModel model = this._controller.BaseModel as DistributionConfigModel;
            if (model.S3OriginSelected)
            {
                sb.AppendFormat("S3 Bucket: {0}\n", model.S3BucketOrigin);
                sb.AppendFormat("Require HTTPS: {0}", model.RequireHTTPS);
            }
            else
            {
                sb.AppendFormat("DNS Name: {0}\n", model.CustomOriginDNSName);
                sb.AppendFormat("HTTP Port: {0}\n", model.HttpPort);
                sb.AppendFormat("HTTPS Port: {0}\n", model.HttpsPort);
                if (model.MatchViewer)
                    sb.AppendFormat("Protocol Policy: Match Viewer\n");
                else
                    sb.AppendFormat("Protocol Policy: HTTP Only\n");
            }

            AddReviewPanel("Origin", sb.ToString());
        }

        void AddStreamingOriginReviewPanel()
        {
            StringBuilder sb = new StringBuilder();

            StreamingDistributionConfigModel model = this._controller.BaseModel as StreamingDistributionConfigModel;
            sb.AppendFormat("S3 Bucket: {0}\n", model.S3BucketOrigin);            

            AddReviewPanel("Origin", sb.ToString());
        }


        void AddLoggingReviewPanel()
        {
            if (!this._controller.BaseModel.IsLoggingEnabled)
                return;

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("Logging S3 Bucket: {0}\n", this._controller.BaseModel.LoggingTargetBucket);
            sb.AppendFormat("Logging Prefix: {0}\n", this._controller.BaseModel.LoggingTargetPrefix);

            AddReviewPanel("Logging", sb.ToString());
        }

        void AddCNAMEReviewPanel()
        {
            if (this._controller.BaseModel.CNAMEs.Count == 0)
                return;

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("CNAMEs: {0}\n", StringUtils.CreateCommaDelimitedList(this._controller.BaseModel.CNAMEs));

            AddReviewPanel("CNAME", sb.ToString());
        }

        void AddPrivateSettingsReviewPanel()
        {
            if (!this._controller.BaseModel.IsPrivateDistributionEnabled)
                return;

            StringBuilder sb = new StringBuilder();

            if(this._controller.BaseModel.SelectedOriginAccessIdentityWrapper == null)
                sb.AppendFormat("Origin Identity: not set\n");
            else
                sb.AppendFormat("Origin Identity: {0}\n", this._controller.BaseModel.SelectedOriginAccessIdentityWrapper.DisplayName);

            sb.AppendFormat("Allow owner to create signed URLs: {0}\n", this._controller.BaseModel.TrustedSignerSelf);
            sb.AppendFormat("Trusted Signers: {0}\n", StringUtils.CreateCommaDelimitedList(this._controller.BaseModel.TrustedSignerAWSAccountIds));

            AddReviewPanel("Private Distribution Settings", sb.ToString());
        }


        void AddReviewPanel(string title, string description)
        {
            TextBlock tb = new TextBlock();
            tb.TextWrapping = System.Windows.TextWrapping.Wrap;
            tb.Text = description;

            _pageUI.AddReviewPanel(title, tb);
        }

        void StorePageData()
        {
        }

        bool IsForwardsNavigationAllowed
        {
            get
            {
                return true;
            }
        }
    }

}

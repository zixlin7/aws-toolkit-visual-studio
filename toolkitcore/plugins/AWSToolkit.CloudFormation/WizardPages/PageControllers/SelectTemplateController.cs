using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.AWSToolkit.CloudFormation.Nodes;
using Amazon.AWSToolkit.CloudFormation.Model;
using Amazon.AWSToolkit.CloudFormation.WizardPages.PageUI;

using log4net;

namespace Amazon.AWSToolkit.CloudFormation.WizardPages.PageControllers
{
    internal class SelectTemplateController : IAWSWizardPageController
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(SelectTemplateController));
        static readonly string SAMPLE_TEMPLATE_MANIFEST = "CloudFormationTemplates/SampleTemplateManifest.xml";

        CloudFormationRootViewModel _cloudFormationRootModel;
        SelectTemplatePage _pageUI;
        SelectTemplateModel _model;

        #region IAWSWizardPageController Members

        public SelectTemplateController(CloudFormationRootViewModel cloudFormationRootModel)
        {
            this._cloudFormationRootModel = cloudFormationRootModel;
        }

        public string PageID => GetType().FullName;

        public IAWSWizard HostingWizard { get; set; }

        public string PageGroup => AWSWizardConstants.DefaultPageGroup;

        public string PageTitle => "Select Template";

        public string ShortPageTitle => null;

        public string PageDescription => "To create a stack, fill in the name for your stack and select a template. You may choose one of the sample templates to get started quickly or on your local hard drive.";

        public void ResetPage()
        {

        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                if (this._model == null)
                {
                    this._model = new SelectTemplateModel();
                    findSampleTemplates();
                    this._model.PropertyChanged += onModelPropertyChanged;
                }

                _pageUI = new SelectTemplatePage(this._cloudFormationRootModel);
                this._pageUI.DataContext = this._model;
            }

            return _pageUI;
        }

        void onModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            HostingWizard.RequestFinishEnablement(this);
            TestForwardTransitionEnablement();
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason != AWSWizardConstants.NavigationReason.movingBack)
                return StorePageData();
            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            return IsForwardsNavigationAllowed;
        }

        public void TestForwardTransitionEnablement()
        {
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, IsForwardsNavigationAllowed);
        }

        public bool AllowShortCircuit()
        {
            return IsForwardsNavigationAllowed;
        }

        #endregion

        bool StorePageData()
        {
            HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] = this._model.StackName;
            HostingWizard[CloudFormationDeploymentWizardProperties.SelectTemplateProperties.propkey_UseLocalTemplateFile] = this._model.UseLocalFile;
            HostingWizard[CloudFormationDeploymentWizardProperties.SelectTemplateProperties.propKey_DisableLoadPreviousValues] = true;
            try
            {
                CloudFormationTemplateWrapper wrapper;
                if (this._model.UseLocalFile)
                {
                    wrapper = CloudFormationTemplateWrapper.FromLocalFile(this._model.LocalFile);
                    HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplateName] = Path.GetFileName(this._model.LocalFile);
                }
                else
                {
                    wrapper = CloudFormationTemplateWrapper.FromPublicS3Location(this._model.SampleTemplate.Location);
                    HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplateName] = null;
                }

                wrapper.LoadAndParse();

                HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate] = wrapper;

                HostingWizard[CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_SNSTopic] = this._model.SNSTopic;

                int timeout = 0;
                if (int.TryParse(this._model.CreationTimeout, out timeout))
                    HostingWizard[CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_CreationTimeout] = timeout;
                else
                    HostingWizard[CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_CreationTimeout] = -1;

                HostingWizard[CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_RollbackOnFailure] = this._model.RollbackOnFailure;
                return true;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error loading template: " + e.Message);
                return false;
            }
        }


        public bool IsForwardsNavigationAllowed
        {
            get
            {
                if (!this._model.HasValidStackName)
                    return false;
                if (this._model.UseLocalFile && !this._model.HasValidLocalFile)
                    return false;
                else if(!this._model.UseLocalFile && this._model.SampleTemplate == null)
                    return false;

                return true;
            }
        }

        void _pageUI_PagePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }

        void findSampleTemplates()
        {
            string content = S3FileFetcher.Instance.GetFileContent(SAMPLE_TEMPLATE_MANIFEST, S3FileFetcher.CacheMode.PerInstance);
            if (content == null)
                return;

            try
            {
                string region = this._cloudFormationRootModel.CurrentEndPoint.RegionSystemName;
                var xdoc = XDocument.Load(new StringReader(content));

                var query = from s in xdoc.Root.Elements("region").Elements("template")
                            where s.Parent.Attribute("systemname").Value == region
                            select new SelectTemplateModel.TemplateLocation()
                                {
                                    Location = s.Attribute("location").Value,
                                    Name = s.Value
                                };

                this._model.Templates.Clear();
                foreach (var template in query)
                {
                    this._model.Templates.Add(template);
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error parsing sample template manifest", e);
            }
        }
    }
}

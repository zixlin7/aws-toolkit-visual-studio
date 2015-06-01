using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.ComponentModel;
using System.Xml.Linq;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.SimpleWorkers;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CloudFormation.WizardPages.PageUI;

using Amazon.EC2;
using Amazon.EC2.Model;

using log4net;
using Amazon.AWSToolkit.PluginServices.Deployment;

namespace Amazon.AWSToolkit.CloudFormation.WizardPages.PageControllers
{
    internal class AWSOptionsPageController : IAWSWizardPageController
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(AWSOptionsPageController));

        AWSOptionsPage _pageUI;
        //string _lastSeenAccount = string.Empty;
        bool _keypairsFetchPending = false;
        bool _securityGroupsFetchPending = false;
        // keeps track of which security groups have had a port check done on them, to avoid
        // re-prompting user if they backtrack through the wizard. We also track whether the
        // user elected to auto-open port 80 if they were prompted.
        readonly Dictionary<string, bool> _portCheckedSecurityGroups = new Dictionary<string, bool>();

        // used to avoid server trips to find x86/x64 architecture and thus instance types if
        // user flailing around with templates/custom amis
        readonly Dictionary<string, Amazon.EC2.Model.Image> _amiArchitectures = new Dictionary<string, Amazon.EC2.Model.Image>();
        readonly Dictionary<string, IAmazonEC2> _ec2ClientsByAccountAndRegion = new Dictionary<string, IAmazonEC2>();

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
            get { return "AWS Options"; }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public string PageDescription
        {
            get { return "Set Amazon EC2 and AWS CloudFormation options for the deployed application."; }
        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (IsWizardInCloudFormationMode)
                return !((bool)HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy]);
            else
                return false;
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new AWSOptionsPage(this);
                _pageUI.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(onPropertyChanged);

                if (HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_CustomAMIID))
                    _pageUI.CustomAMIID = HostingWizard[DeploymentWizardProperties.SeedData.propkey_CustomAMIID] as string;

                if (HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_SeedName))
                    _pageUI.StackName = HostingWizard[DeploymentWizardProperties.SeedData.propkey_SeedName] as string;
            }

            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason != AWSWizardConstants.NavigationReason.movingBack)
            {
                var containersList = LoadAvailableContainers().ToList();
                string defaultContainerName = ToolkitAMIManifest.Instance.QueryDefaultWebDeploymentContainer(ToolkitAMIManifest.HostService.CloudFormation);

                _pageUI.SetContainers(containersList, defaultContainerName);

                LoadExistingKeyPairs();
                LoadExistingSecurityGroups();

                string defaultAMIID = null;
                if (containersList.Any())
                {
                    foreach (var c in containersList)
                    {
                        if (string.Compare(c.ContainerName, defaultContainerName, StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            defaultAMIID = c.ID;
                            break;
                        }
                    }    
                }

                // fallback to using the ami encoded in the template, if any, to set the image. Size can always
                // come from the template
                if (string.IsNullOrEmpty(defaultAMIID) && Template.Parameters.ContainsKey("AmazonMachineImage"))
                    defaultAMIID = Template.Parameters["AmazonMachineImage"].DefaultValue;
                var defaultInstanceSize = Template.Parameters.ContainsKey("InstanceType") ? Template.Parameters["InstanceType"].DefaultValue : "t1.micro";

                if (!string.IsNullOrEmpty(defaultAMIID))
                    SetInstanceTypesForAMI(defaultAMIID, defaultInstanceSize);
            }

            TestForwardTransitionEnablement();
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason != AWSWizardConstants.NavigationReason.movingBack)
            {
                if (!_portCheckedSecurityGroups.ContainsKey(_pageUI.SecurityGroupName))
                {
                    bool autoOpenPort = false;
                    if (!CheckSecurityGroupPortOpen(_pageUI.SecurityGroupName, 80))
                    {
                        var msg = string.Format("Port 80 (HTTP) is not open in the selected security group '{0}'. This port is required for AWS CloudFormation deployment.\r\rWould you like port 80 to be opened for this group during deployment?\r\rNote - this will open the port in all instances using this security group.",
                                                _pageUI.SecurityGroupName);
                        if (!ToolkitFactory.Instance.ShellProvider.Confirm("Required Port Not Open", msg))
                            return false;
                        else
                            autoOpenPort = true;
                    }

                    _portCheckedSecurityGroups.Add(_pageUI.SecurityGroupName, autoOpenPort);
                }

                HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_AutoOpenPort80] = _portCheckedSecurityGroups[_pageUI.SecurityGroupName];
            }

            StorePageData();
            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            return _pageUI != null && IsForwardsNavigationAllowed;
        }

        public void TestForwardTransitionEnablement()
        {
            bool pageComplete = IsForwardsNavigationAllowed;
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, pageComplete);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, pageComplete);
        }

        public bool AllowShortCircuit()
        {
            return false; // page has mandatory fields
        }

        #endregion

        bool IsWizardInCloudFormationMode
        {
            get
            {
                var service = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner] as string;
                return service == DeploymentServiceIdentifiers.CloudFormationServiceName;
            }
        }

        bool IsForwardsNavigationAllowed
        {
            get
            {
                if (!IsWizardInCloudFormationMode)
                    return true;

                if (_pageUI == null)
                    return false; // page has mandatory fields

                return !_keypairsFetchPending
                            && !_securityGroupsFetchPending
                            && _pageUI.HasKeyPairSelection
                            && !string.IsNullOrEmpty(_pageUI.SecurityGroupName)
                            && _pageUI.SelectedInstanceType != null 
                            && _pageUI.IsStackNameValid;
            }
        }

        CloudFormationTemplateWrapper Template
        {
            get
            {
                var template = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate] as CloudFormationTemplateWrapper;
                try
                {
                    template.LoadAndParse(new CloudFormationTemplateWrapper.OnTemplateParseComplete(TemplateParseCompleted)); // noop if already parsed
                }
                catch(Exception e)
                {
                    LOGGER.Error("Failed to parse template", e);
                }

                return template;
            }
        }

        void TemplateParseCompleted(DeploymentTemplateWrapperBase templateBase)
        {
            var template = templateBase as CloudFormationTemplateWrapper;
            if (template == null)
                return;

            // mark parameters not set as 'Hidden' in the template that we want controlled by
            // this page and not the generic templates collection
            template.Parameters["InstanceType"].Hidden = true;
            template.Parameters["KeyPair"].Hidden = true;
            template.Parameters["AmazonMachineImage"].Hidden = true;
            template.Parameters["SecurityGroup"].Hidden = true;
        }

        void StorePageData()
        {
            if (!IsWizardInCloudFormationMode)
                return;

            ToolkitAMIManifest.ContainerAMI container = _pageUI.SelectedContainer;
            if (container != null)
            {
                HostingWizard[CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_ContainerAMI] =
                    container.ID;
                HostingWizard[CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_ContainerName] =
                    container.ContainerName;
                HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_CustomAMIID] = null;
            }
            else
            {
                HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_CustomAMIID] = _pageUI.CustomAMIID;
                HostingWizard[CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_ContainerAMI] = null;
                HostingWizard[CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_ContainerName] = null;
            }

            var instanceType = _pageUI.SelectedInstanceType;
            if (instanceType != null)
            {
                HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_InstanceTypeID] = instanceType.Id;
                HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_InstanceTypeName] = instanceType.Name;
            }

            string keypairName;
            bool createNew;
            _pageUI.QueryKeyPairSelection(out keypairName, out createNew);
            HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_KeyPairName] = keypairName;
            if (!string.IsNullOrEmpty(keypairName))
                HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_CreateKeyPair] = createNew;

            HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_SecurityGroupName] = _pageUI.SecurityGroupName;

            HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] = _pageUI.StackName;

            if (!string.IsNullOrEmpty(_pageUI.SNSTopic))
                HostingWizard[CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_SNSTopic] = _pageUI.SNSTopic;
            else
                HostingWizard[CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_SNSTopic] = null;

            int timeout = -1;
            if (System.String.Compare(_pageUI.CreationTimeout, "none", StringComparison.InvariantCultureIgnoreCase) != 0)
                int.TryParse(_pageUI.CreationTimeout, out timeout);
            HostingWizard[CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_CreationTimeout] = timeout;

            HostingWizard[CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_RollbackOnFailure] = _pageUI.RollbackOnFailure;
        }

        IEnumerable<ToolkitAMIManifest.ContainerAMI> LoadAvailableContainers()
        {
            var region = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedRegion] as RegionEndPointsManager.RegionEndPoints;
            try
            {
                return ToolkitAMIManifest.Instance.QueryWebDeploymentContainers(ToolkitAMIManifest.HostService.CloudFormation, region.SystemName);
            }
            catch (Exception e)
            {
                LOGGER.Error(string.Format("Failed to obtain deployment container types for region {0}, error {1}", region.SystemName, e.Message));
            }

            return new List<ToolkitAMIManifest.ContainerAMI>();
        }

        void LoadExistingKeyPairs()
        {
            _keypairsFetchPending = true;
            new QueryKeyPairNamesWorker(((Account.AccountViewModel)HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount]),
                                        ((RegionEndPointsManager.RegionEndPoints)HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedRegion]).SystemName,
                                        EC2Client,
                                        HostingWizard.Logger,
                                        new QueryKeyPairNamesWorker.DataAvailableCallback(OnKeyPairsAvailable));
        }

        void OnKeyPairsAvailable(ICollection<string> keypairNames, ICollection<string> keyPairsStoredInToolkit)
        {
            _keypairsFetchPending = false;
            _pageUI.SetExistingKeyPairs(keypairNames, keyPairsStoredInToolkit, string.Empty);
        }

        void LoadExistingSecurityGroups()
        {
            _securityGroupsFetchPending = true;
            new QuerySecurityGroupsWorker(EC2Client,
                                          null,
                                          HostingWizard.Logger,
                                          new QuerySecurityGroupsWorker.DataAvailableCallback(OnSecurityGroupsAvailable));
        }

        void OnSecurityGroupsAvailable(ICollection<SecurityGroup> securityGroups)
        {
            _securityGroupsFetchPending = false;
            this._pageUI.SetExistingSecurityGroups(securityGroups, string.Empty);
        }


        void SetInstanceTypesForAMI(string amiID, string defaultInstanceType)
        {
            if (_amiArchitectures.ContainsKey(amiID))
                _pageUI.SetInstanceTypes(Amazon.AWSToolkit.EC2.InstanceType.GetValidTypes(_amiArchitectures[amiID]), defaultInstanceType);
            else
            {
                var bw = new BackgroundWorker();

                bw.DoWork += new DoWorkEventHandler(DescribeImageWorker);
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(DescribeImageWorkerCompleted);

                bw.RunWorkerAsync(new object[] 
                                  { 
                                      EC2Client,
                                      amiID,
                                      defaultInstanceType,
                                      LOGGER
                                   });
            }
        }

        IAmazonEC2 EC2Client
        {
            get
            {
                var rep = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedRegion] as RegionEndPointsManager.RegionEndPoints;
                var selectedAccount = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel;

                var accountAndRegion = string.Concat(selectedAccount.AccessKey, rep.SystemName);
                IAmazonEC2 ec2Client;
                if (!this._ec2ClientsByAccountAndRegion.ContainsKey(accountAndRegion))
                {
                    var ec2Config = new AmazonEC2Config {ServiceURL = rep.GetEndpoint(RegionEndPointsManager.EC2_SERVICE_NAME).Url};
                    ec2Client = new AmazonEC2Client(selectedAccount.AccessKey, selectedAccount.SecretKey, ec2Config);
                    this._ec2ClientsByAccountAndRegion.Add(accountAndRegion, ec2Client);
                }
                else
                    ec2Client = this._ec2ClientsByAccountAndRegion[accountAndRegion];

                return ec2Client;
            }
        }

        void DescribeImageWorker(object sender, DoWorkEventArgs e)
        {
            e.Result = null;
            try
            {
                var workerData = e.Argument as object[];

                var ec2Client = workerData[0] as IAmazonEC2;
                var amiID = workerData[1] as string;
                var defaultInstanceType = workerData[2] as string;

                var response = ec2Client.DescribeImages(new DescribeImagesRequest() { ImageIds = new List<string>() { amiID } });
                e.Result = new object[] { response.Images[0], defaultInstanceType };
            }
            catch
            {
            }
        }

        void DescribeImageWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result != null)
            {
                var results = e.Result as object[];
                var image = results[0] as Amazon.EC2.Model.Image;
                _amiArchitectures.Add(image.ImageId, image);
                _pageUI.SetInstanceTypes(EC2.InstanceType.GetValidTypes(image), results[1] as string);
            }
            else
            {
                System.Windows.MessageBox.Show("Failed to obtain image data for the selected AMI; please use another (if custom) or select a different template.", 
                                               "AMI Details", 
                                               System.Windows.MessageBoxButton.OK, 
                                               System.Windows.MessageBoxImage.Error);
            }
        }

        private void onPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.Compare(e.PropertyName, AWSOptionsPage.uiProperty_CustomAMIID, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                if (!string.IsNullOrEmpty(_pageUI.CustomAMIID))
                    SetInstanceTypesForAMI(_pageUI.CustomAMIID, null);
                else
                    SetInstanceTypesForAMI(Template.Parameters["AmazonMachineImage"].DefaultValue, Template.Parameters["InstanceType"].DefaultValue);
            }

            TestForwardTransitionEnablement();
        }

        private bool CheckSecurityGroupPortOpen(string securityGroup, int port)
        {
            try
            {
                var request = new DescribeSecurityGroupsRequest() { GroupNames = new List<string>() { securityGroup } };
                var response = EC2Client.DescribeSecurityGroups(request);

                // If we can't find the group then situation is undefined so we will ignore it
                // and return true
                if (response.SecurityGroups.Count == 0)
                    return true;

                foreach (var rule in response.SecurityGroups[0].IpPermissions)
                {
                    if (rule.UserIdGroupPairs != null && rule.UserIdGroupPairs.Count > 0)
                        continue;

                    int fromPort = (int)rule.FromPort;
                    int toPort = (int)rule.ToPort;

                    if (fromPort == port)
                        return true;
                    if (fromPort < port && port <= toPort)
                        return true;
                }
            }
            catch (Exception e)
            {
                LOGGER.ErrorFormat("Failed to determine if port '{0}' open for selected group '{1}'; exception message '{2}'", port, securityGroup, e.Message);
            }

            return false;
        }
    }
}

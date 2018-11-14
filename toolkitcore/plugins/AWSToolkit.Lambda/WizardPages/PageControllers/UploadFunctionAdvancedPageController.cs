using System;
using log4net;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Controls;

using Amazon.AWSToolkit.SimpleWorkers;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.EC2.Workers;
using Amazon.AWSToolkit.Lambda.WizardPages.PageUI;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Amazon.AWSToolkit.Lambda.Model;

using Amazon.SimpleNotificationService;
using Amazon.SQS;

namespace Amazon.AWSToolkit.Lambda.WizardPages.PageControllers
{
    /// <summary>
    /// Second page of the Upload Function wizard, gathering 'advanced'
    /// settings governing the hosting of the function.
    /// </summary>
    public class UploadFunctionAdvancedPageController : IAWSWizardPageController
    {
        ILog LOGGER = LogManager.GetLogger(typeof(UploadFunctionDetailsPageController));
        private readonly object _syncLock = new object();

        private int _backgroundWorkersActive = 0;
        private int BackgroundWorkerCount
        {
            get
            {
                int count;
                lock (_syncLock)
                {
                    count = _backgroundWorkersActive;
                }
                return count;
            }
        }


        private UploadFunctionAdvancedPage _pageUI;

        public IAWSWizard HostingWizard { get; set; }

        public string PageDescription
        {
            get { return "Configure additional settings for your function."; }
        }

        public string PageGroup
        {
            get { return AWSWizardConstants.DefaultPageGroup; }
        }

        public string PageID
        {
            get { return GetType().FullName; }
        }

        public string PageTitle
        {
            get { return "Advanced Function Details"; }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public bool AllowShortCircuit()
        {
            return IsForwardsNavigationAllowed;
        }

        public void ResetPage()
        {

        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            _pageUI.RefreshPageContent();

            LoadExistingVpcSubnets();
            LoadExistingKMSKeys();

            LoadDLQTargets();

            _pageUI.SetXRayAvailability((bool)HostingWizard.GetProperty(UploadFunctionWizardProperties.XRayAvailable));

            TestForwardTransitionEnablement();
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new UploadFunctionAdvancedPage(this);
                _pageUI.PropertyChanged += _pageUI_PropertyChanged;
            }

            return _pageUI;
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

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public void TestForwardTransitionEnablement()
        {
            // todo: as there are no more pages, try and indicate that actually uploading is next by disabling Next
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, false);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, IsForwardsNavigationAllowed);
        }

        public bool IsForwardsNavigationAllowed
        {
            get
            {
                if (_pageUI == null)    // todo: this is not always the case (say if the function exists)
                    return false;

                if (BackgroundWorkerCount > 0)
                    return false;

                return _pageUI.AllRequiredFieldsAreSet;
            }
        }

        bool StorePageData()
        {
            var shouldProgress = true;

            // if we've not been called to render page, we'll have transferred whatever seed
            // values we have into the wizard properties already
            if (_pageUI != null)
            {
                if (_pageUI.IsValid)
                {
                    HostingWizard[UploadFunctionWizardProperties.Role] = _pageUI.SelectedRole;
                    HostingWizard[UploadFunctionWizardProperties.ManagedPolicy] = _pageUI.SelectedManagedPolicy;
                    
                    HostingWizard[UploadFunctionWizardProperties.MemorySize] = _pageUI.Memory;
                    HostingWizard[UploadFunctionWizardProperties.Timeout] = _pageUI.Timeout;

                    HostingWizard[UploadFunctionWizardProperties.Subnets] = _pageUI.SelectedSubnets;
                    HostingWizard[UploadFunctionWizardProperties.SecurityGroups] = _pageUI.SelectedSecurityGroups;

                    HostingWizard[UploadFunctionWizardProperties.EnvironmentVariables] = _pageUI.SelectedEnvironmentVariables;

                    HostingWizard[UploadFunctionWizardProperties.DeadLetterTargetArn] = _pageUI.SelectedDLQTargetArn ?? "";

                    if ((bool) HostingWizard[UploadFunctionWizardProperties.XRayAvailable])
                        HostingWizard[UploadFunctionWizardProperties.TracingMode] = _pageUI.IsEnableActiveTracing
                            ? Amazon.Lambda.TracingMode.Active.ToString()
                            : Amazon.Lambda.TracingMode.PassThrough.ToString();
                    else
                        HostingWizard[UploadFunctionWizardProperties.TracingMode] = Amazon.Lambda.TracingMode.PassThrough.ToString();

                    // Lambda's api treats 'no key specified' as 'use service default key'
                    var kmsKey = _pageUI.SelectedKMSKey;
                    if (kmsKey == KeyAndAliasWrapper.LambdaDefaultKMSKey.Key)
                        HostingWizard[UploadFunctionWizardProperties.KMSKey] = null;
                    else
                        HostingWizard[UploadFunctionWizardProperties.KMSKey] = kmsKey.KeyArn;
                }
                else
                    shouldProgress = false;
            }

            return shouldProgress;
        }

        private void _pageUI_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "VpcSubnets")
            {
                if (_pageUI.SubnetsSpanVPCs)
                {
                    this._pageUI.SetAvailableSecurityGroups(null, null, null);
                }
                else
                {
                    var subnets = _pageUI.SelectedSubnets;
                    string vpcId = null;
                    if (subnets != null)
                    {
                        // relying here that the page has rejected attempt to select subnets
                        // across multiple vpcs
                        foreach (var subnet in subnets)
                        {
                            vpcId = subnet.VpcId;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(vpcId))
                        LoadExistingSecurityGroups(vpcId);
                    else
                        this._pageUI.SetAvailableSecurityGroups(null, null, null);
                }
            }

            TestForwardTransitionEnablement();
        }

        void LoadExistingVpcSubnets()
        {
            Interlocked.Increment(ref _backgroundWorkersActive);
            new QueryVpcsAndSubnetsWorker(EC2Client, HostingWizard.Logger, new QueryVpcsAndSubnetsWorker.DataAvailableCallback(OnVpcSubnetsAvailable));
        }

        void OnVpcSubnetsAvailable(IEnumerable<Vpc> vpcs, IEnumerable<Subnet> subnets)
        {
            try
            {
                this._pageUI.SetAvailableVpcSubnets(vpcs, subnets,
                    HostingWizard[UploadFunctionWizardProperties.SeedSubnetIds] as string[]);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error adding vpc subnets onto the page", e);
            }
            finally
            {
                Interlocked.Decrement(ref _backgroundWorkersActive);
                TestForwardTransitionEnablement();
            }
        }

        void LoadExistingSecurityGroups(string vpcId)
        {
            Interlocked.Increment(ref _backgroundWorkersActive);
            new QuerySecurityGroupsWorker(EC2Client,
                                          vpcId,
                                          HostingWizard.Logger,
                                          OnSecurityGroupsAvailable);
        }

        void OnSecurityGroupsAvailable(IEnumerable<SecurityGroup> securityGroups)
        {
            try
            {
                this._pageUI.SetAvailableSecurityGroups(securityGroups, string.Empty,
                    HostingWizard[UploadFunctionWizardProperties.SeedSecurityGroupIds] as string[]);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error adding security groups onto the page", e);
            }
            finally
            {
                Interlocked.Decrement(ref _backgroundWorkersActive);
                TestForwardTransitionEnablement();
            }
        }

        void LoadExistingKMSKeys()
        {
            Interlocked.Increment(ref _backgroundWorkersActive);
            new QueryKMSKeysWorker(KMSClient,
                                   HostingWizard.Logger,
                                   OnKMSKeysAvailable);
        }

        void OnKMSKeysAvailable(IEnumerable<KeyListEntry> keys, IEnumerable<AliasListEntry> aliases)
        {
            try
            {
                string defaultKeyArn = null;
                if (HostingWizard.IsPropertySet(UploadFunctionWizardProperties.KMSKey))
                    defaultKeyArn = HostingWizard[UploadFunctionWizardProperties.KMSKey] as string;

                this._pageUI.SetAvailableKMSKeys(keys, aliases, defaultKeyArn);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error adding KMS keys onto the page", e);
            }
            finally
            {
                Interlocked.Decrement(ref _backgroundWorkersActive);
                TestForwardTransitionEnablement();
            }
        }

        void LoadDLQTargets()
        {
            Interlocked.Increment(ref _backgroundWorkersActive);
            new QueryDLQTargetsWorker(
                                SNSClient,
                                SQSClient,
                                HostingWizard.Logger,
                                OnDLQTargetsAvailable);
        }

        void OnDLQTargetsAvailable(QueryDLQTargetsWorker.QueryResults results)
        {
            try
            {
                if (!string.IsNullOrEmpty(results.TopicsErrorMessage))
                    this.HostingWizard.SetPageError(results.TopicsErrorMessage);
                if (!string.IsNullOrEmpty(results.QueuesErrorMessage))
                    this.HostingWizard.SetPageError(results.QueuesErrorMessage);

                this._pageUI.SetAvailableDLQTargets(results.TopicArns, results.QueueArns);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error adding DLQ targets onto the page", e);
            }
            finally
            {
                Interlocked.Decrement(ref _backgroundWorkersActive);
                TestForwardTransitionEnablement();
            }
        }

        private IAmazonEC2 EC2Client
        {
            get
            {
                var account = HostingWizard[UploadFunctionWizardProperties.UserAccount] as AccountViewModel;
                var region = HostingWizard[UploadFunctionWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;

                return account.CreateServiceClient<AmazonEC2Client>(region);
            }
        }

        private IAmazonKeyManagementService KMSClient
        {
            get
            {
                var account = HostingWizard[UploadFunctionWizardProperties.UserAccount] as AccountViewModel;
                var region = HostingWizard[UploadFunctionWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;

                return account.CreateServiceClient<AmazonKeyManagementServiceClient>(region);
            }
        }

        private IAmazonSimpleNotificationService SNSClient
        {
            get
            {
                var account = HostingWizard[UploadFunctionWizardProperties.UserAccount] as AccountViewModel;
                var region = HostingWizard[UploadFunctionWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;

                return account.CreateServiceClient<AmazonSimpleNotificationServiceClient>(region);
            }
        }

        private IAmazonSQS SQSClient
        {
            get
            {
                var account = HostingWizard[UploadFunctionWizardProperties.UserAccount] as AccountViewModel;
                var region = HostingWizard[UploadFunctionWizardProperties.Region] as RegionEndPointsManager.RegionEndPoints;

                return account.CreateServiceClient<AmazonSQSClient>(region);
            }
        }
    }
}

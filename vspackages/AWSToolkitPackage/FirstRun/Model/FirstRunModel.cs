using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows.Media;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Settings;
using log4net;
using Amazon.AWSToolkit.Account.Model;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.VisualStudio.FirstRun.Model
{
    public class FirstRunModel : INotifyPropertyChanged
    {
        public string IamConsoleEndpoint => "https://console.aws.amazon.com/iam/home?region=us-east-1#/users";

        public string PrivacyPolicyEndpoint => "https://aws.amazon.com/privacy/";

        public string UsingToolkitEndpoint => "https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/welcome.html";

        public string DeveloperBlogEndpoint => "https://aws.amazon.com/blogs/developer/category/net/";

        public string DeployingLambdaEndpoint => "https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/lambda.html";

        public string DeployingBeanstalkEndpoint => "https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/create_deploy_NET.html";

        private readonly ToolkitContext _toolkitContext;

        public FirstRunModel(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
            _collectAnalytics = CheckAnalyticsCollectionPermission();
        }

        public string ProfileName
        {
            get => _profileName;
            set { _profileName = value; OnPropertyChanged(); }
        }

        public string AccessKey
        {
            get => _accessKey;
            set { _accessKey = value; OnPropertyChanged(); }
        }

        public string SecretKey
        {
            get => _secretKey;
            set { _secretKey = value; OnPropertyChanged(); }
        }

        public bool IsValid
        {
            get => _isValid;
            set { _isValid = value; OnPropertyChanged(); }
        }

        public bool CollectAnalytics
        {
            get => _collectAnalytics;
            set
            {
                if (_collectAnalytics != value)
                {
                    _collectAnalytics = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool OpenAWSExplorerOnClosing
        {
            get => _openAwsExplorerOnClose;
            set { _openAwsExplorerOnClose = value; OnPropertyChanged(); }
        }

        internal void Save()
        {
            if (!IsValid)
                throw new InvalidOperationException("Model save requested but state not valid");
            ICredentialIdentifier identifier = null;
            ToolkitRegion region = null;
            ManualResetEvent mre = new ManualResetEvent(false);
            EventHandler<EventArgs> HandleCredentialUpdate = (sender, args) =>
            {
                var ide = _toolkitContext.CredentialManager.GetCredentialIdentifierById(identifier?.Id);
                if (ide != null && region != null)
                {
                    mre.Set();
                    ToolkitFactory.Instance.AwsConnectionManager.ChangeConnectionSettings(identifier, region);
                }
            };
            try
            {
                var uniqueKey = Guid.NewGuid();
                identifier = new SharedCredentialIdentifier(ProfileName.Trim());
                var regionId = RegionEndpoint.USEast1.SystemName;
                var properties = new ProfileProperties
                {
                    Name = ProfileName.Trim(),
                    AccessKey = AccessKey?.Trim(),
                    SecretKey = SecretKey?.Trim(),
                    UniqueKey = uniqueKey.ToString(),
                    Region = regionId
                };
                region = _toolkitContext.RegionProvider.GetRegion(regionId);
                _toolkitContext.CredentialManager.CredentialManagerUpdated += HandleCredentialUpdate;

                // create profile ensures profile has unique key and registers it
                _toolkitContext.CredentialSettingsManager.CreateProfile(identifier, properties);

                WriteAnalyticsCollectionPermission();
                mre.WaitOne(2000);
          
            }
            catch(Exception e)
            {
                LOGGER.Error(e);
            }
            finally
            {
                _toolkitContext.CredentialManager.CredentialManagerUpdated -= HandleCredentialUpdate;
            }
        }

        public List<ShowMeHowToListItem> ShowMeHowToListItems
        {
            get
            {
                var l = new List<ShowMeHowToListItem>
                {
                    new ShowMeHowToListItem
                    {
                        Title = "View and explore my AWS resources",
                        HintGraphicResourceName = "Amazon.AWSToolkit.VisualStudio.Resources.FirstRun.explorerlocation_light.png"
                    },
                    new ShowMeHowToListItem
                    {
                        Title = "Publish my C# Lambda function",
                        HintGraphicResourceName = "Amazon.AWSToolkit.VisualStudio.Resources.FirstRun.publishlambda_light.png"
                    },
                    new ShowMeHowToListItem
                    {
                        Title = "Publish my ASP.NET application",
                        HintGraphicResourceName = "Amazon.AWSToolkit.VisualStudio.Resources.FirstRun.publishbeanstalk_light.png"
                    }
                };

                return l;
            }
        }

        internal bool AwsCredentialsFromCsv(string csvCredentialsFile)
        {
            string accessKey, secretKey;
            if (RegisterAccountModel.ReadAwsCredentialsFromCsv(csvCredentialsFile, out accessKey, out secretKey))
            {
                AccessKey = accessKey;
                SecretKey = secretKey;
                return true;
            }

            return false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// The user may have already set this option
        /// </summary>
        /// <returns>
        /// True: user has enabled telemetry, or has never been prompted
        /// False: user has disabled telemetry
        /// </returns>
        private bool CheckAnalyticsCollectionPermission()
        {
            try
            {
                return ToolkitSettings.Instance.TelemetryEnabled;
            }
            catch (Exception e)
            {
                LOGGER.Error(e);
            }

            return ToolkitSettings.DefaultValues.TelemetryEnabled;
        }

        /// <summary>
        /// Writes the user election for analytics into the misc settings file. Other associated
        /// data, such as an anonymized customer id and the Cognito id will be generated and
        /// persisted on-the-fly if the user has consented.
        /// </summary>
        private void WriteAnalyticsCollectionPermission()
        {
            try
            {
                ToolkitSettings.Instance.TelemetryEnabled = CollectAnalytics;
            }
            catch (Exception e)
            {
                LOGGER.Error(e);
            }
        }

        private string _profileName = "default";
        private string _accessKey;
        private string _secretKey;
        private bool _collectAnalytics = ToolkitSettings.DefaultValues.TelemetryEnabled;
        private bool _isValid;
        private bool _openAwsExplorerOnClose = true;

        internal static ILog LOGGER = LogManager.GetLogger(typeof(FirstRunModel));
    }

    public class ShowMeHowToListItem
    {
        public string Title { get; internal set; }

        public string HintGraphicResourceName { get; internal set; }

        public ImageSource HintGraphic => IconHelper.GetIcon(this.GetType().Assembly, HintGraphicResourceName).Source;

        public ImageSource Arrow =>
            IconHelper.GetIcon(this.GetType().Assembly,
                    "Amazon.AWSToolkit.VisualStudio.Resources.FirstRun.rightbluearrow.png")
                .Source;
    }

}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Media;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Persistence;
using log4net;
using ThirdParty.Json.LitJson;
using Amazon.AWSToolkit.Account.Model;

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

        public FirstRunModel()
        {
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

        public string AccountNumber
        {
            get => _accountNumber;
            set { _accountNumber = value; OnPropertyChanged(); }
        }

        public bool IsValid
        {
            get => _isValid;
            set { _isValid = value; OnPropertyChanged(); }
        }

        public AccountTypes.AccountType SelectedAccountType
        {
            get => _selectedAccountType ?? (_selectedAccountType = AllAccountTypes[0]);
            set => _selectedAccountType = value;
        }

        public bool CollectAnalytics
        {
            get => _collectAnalytics;
            set { _collectAnalytics = value;OnPropertyChanged(); }
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

            var settings = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.RegisteredProfiles);
            var uniqueKey = Guid.NewGuid();

            var os = settings.NewObjectSettings(uniqueKey.ToString());
            os[ToolkitSettingsConstants.DisplayNameField] = ProfileName.Trim();
            os[ToolkitSettingsConstants.AccessKeyField] = AccessKey.Trim();
            os[ToolkitSettingsConstants.SecretKeyField] = SecretKey.Trim();
            os[ToolkitSettingsConstants.AccountNumberField] = AccountNumber?.Trim();
            os[ToolkitSettingsConstants.Restrictions] = SelectedAccountType.SystemName;

            PersistenceManager.Instance.SaveSettings(ToolkitSettingsConstants.RegisteredProfiles, settings);

            ToolkitFactory.Instance.Navigator.UpdateAccountSelection(uniqueKey, true);

            WriteAnalyticsCollectionPermission();
        }

        public IList<AccountTypes.AccountType> AllAccountTypes => AccountTypes.AllAccountTypes;

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
        /// If the user has the 2013/2015 extension installed, they may have already declined analytics 
        /// so check and return appropriate flag setting for page defaults.
        /// </summary>
        /// <returns>
        /// True to request collection if the user has not declined previously
        /// or not been prompted
        /// </returns>
        private bool CheckAnalyticsCollectionPermission()
        {
            try
            {
                var localAppFolderLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AWSToolkit");
                var miscSettingsFile = Path.Combine(localAppFolderLocation, "MiscSettings.json");

                // if the file doesn't exist, the user only has VS2017 installed and we
                // therefore want to default to collection
                if (!File.Exists(miscSettingsFile))
                    return true;

                //grab the text from MiscSettings and convert it to a Json object
                var miscSettings = File.ReadAllText(miscSettingsFile);

                // if the file is empty, we can default to collection since the user
                // hasn't declined previously
                if (string.IsNullOrEmpty(miscSettings))
                    return true;

                var jsonObj = JsonMapper.ToObject(miscSettings);
                if (JsonPropertyExists(jsonObj, ToolkitSettingsConstants.MiscSettings) 
                    && JsonPropertyExists(jsonObj[ToolkitSettingsConstants.MiscSettings], ToolkitSettingsConstants.AnalyticsPermitted))
                {
                    var userPreferenceOnAnalytics = jsonObj[ToolkitSettingsConstants.MiscSettings][ToolkitSettingsConstants.AnalyticsPermitted].ToString();
                    bool collectionPermitted;
                    bool.TryParse(userPreferenceOnAnalytics, out collectionPermitted);
                    return collectionPermitted;
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error reading analytics setting from miscsettings.json file", e);
            }

            // if an error occurs, just reset to collect so we don't miss anything
            return true;
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
                var localAppFolderLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AWSToolkit");
                var miscSettingsFile = Path.Combine(localAppFolderLocation, "MiscSettings.json");

                JsonData miscSettings = null;
                if (File.Exists(miscSettingsFile) )
                    miscSettings = JsonMapper.ToObject(File.ReadAllText(miscSettingsFile));

                if (JsonPropertyExists(miscSettings, ToolkitSettingsConstants.MiscSettings))
                {
                    miscSettings[ToolkitSettingsConstants.MiscSettings][ToolkitSettingsConstants.AnalyticsPermitted] = CollectAnalytics.ToString();
                }
                else
                {
                    var insideObj = new JsonData();
                    insideObj.SetJsonType(JsonType.String);
                    insideObj[ToolkitSettingsConstants.AnalyticsPermitted] = CollectAnalytics.ToString();

                    var outsideObj = new JsonData();
                    outsideObj.SetJsonType(JsonType.Object);
                    outsideObj[ToolkitSettingsConstants.MiscSettings] = insideObj;

                    miscSettings = outsideObj;
                }

                var sb = new StringBuilder();
                var writer = new JsonWriter(sb) {PrettyPrint = true};
                JsonMapper.ToJson(miscSettings, writer);

                if (!Directory.Exists(localAppFolderLocation))
                    Directory.CreateDirectory(localAppFolderLocation);

                File.WriteAllText(miscSettingsFile, sb.ToString());
            }
            catch (Exception e)
            {
                LOGGER.Error("Error writing analytics setting to miscsettings.json file", e);
            }
        }

        public static bool JsonPropertyExists(JsonData jsonObject, string property)
        {
            if (jsonObject == null)
                return false;

            IEnumerable<string> propertyNames = jsonObject.PropertyNames;
            foreach (var propertyName in propertyNames)
            {
                if (string.Equals(propertyName, property, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }


        AccountTypes.AccountType _selectedAccountType;
        private string _profileName = "default";
        private string _accessKey;
        private string _secretKey;
        private string _accountNumber;
        private bool _collectAnalytics = true;
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

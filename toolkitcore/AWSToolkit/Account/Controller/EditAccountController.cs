using System;
using System.Linq;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Navigator;

using System.Threading;

using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.Account.Controller
{
    public class EditAccountController : RegisterAccountController, IContextCommand
    {
        public const string NAME_CHANGE_PARAMETER = "nameChange";
        public const string CREDENTIALS_CHANGE_PARAMETER = "credentialsChange";

        AccountViewModel _accountViewModel;

        public EditAccountController(ICredentialManager credentialManager,
            ICredentialSettingsManager credentialSettingsManager, IAwsConnectionManager connectionManger,
            IRegionProvider regionProvider) : base(
            credentialManager, credentialSettingsManager, connectionManger, regionProvider)
        {
        }

        public ActionResults Execute(IViewModel model)
        {
            this._accountViewModel = model as AccountViewModel;
            if (this._accountViewModel == null)
                return new ActionResults().WithSuccess(false);

            return base.Execute();
        }

        protected override void LoadModel()
        {
            this.Model.StorageLocationVisibility = System.Windows.Visibility.Collapsed;
            this.Model.CredentialId = this._accountViewModel.Identifier?.Id;
            if (this._accountViewModel.Identifier == null)
            {
                throw new Exception("Failed to load an empty profile to edit");
            }
            var profileProperties =
                _credentialSettingsManager.GetProfileProperties(this._accountViewModel
                    .Identifier);
            if (profileProperties == null)
            {
                throw new Exception($"Failed to find profile {this._accountViewModel.AccountDisplayName} to edit");
            }

            this.Model.DisplayName = this._accountViewModel.DisplayName;
            this.Model.AccessKey = this._accountViewModel.ProfileProperties?.AccessKey;
            this.Model.SecretKey = this._accountViewModel.ProfileProperties?.SecretKey;
            this.Model.ProfileName = this._accountViewModel.Identifier?.ProfileName;
            this.Model.Partition =
                this.Model.Partitions.FirstOrDefault(x => string.Equals(x.Id, this._accountViewModel.PartitionId));
            this.Model.Region = this._accountViewModel.Region;
            this.Model.UniqueKey = new Guid(this._accountViewModel.SettingsUniqueKey);
            this._control._ctlAccessKey.Password = this.Model.AccessKey ?? string.Empty;
            this._control._ctlSecretKey.Password = this.Model.SecretKey ?? string.Empty;
            DefaultProfileNameInUse = true;
        }

        public override void Persist()
        {
            bool nameChange = false;
            bool credentialsChange = false;
            ToolkitRegion region = null;
            ICredentialIdentifier identifier = null;
            ManualResetEvent mre = new ManualResetEvent(false);
            EventHandler<EventArgs> HandleCredentialUpdate = (sender, args) =>
            {
                var ide = _credentialManager.GetCredentialIdentifierById(identifier?.Id);
                if (ide != null && region != null)
                {
                    mre.Set();
                    this._accountViewModel.ReloadFromPersistence(this.Model.DisplayName.Trim());
                    _awsConnectionManager.ChangeConnectionSettings(identifier, region);
                }
            };

            try
            {
                if (string.Equals(this._accountViewModel.Identifier.FactoryId,
                    SDKCredentialProviderFactory.SdkProfileFactoryId))
                {
                    identifier = new SDKCredentialIdentifier(this.Model.ProfileName.Trim());
                }

                else
                {
                    identifier = new SharedCredentialIdentifier(this.Model.ProfileName.Trim());
                }

                var profileProperties =
                    _credentialSettingsManager.GetProfileProperties(this._accountViewModel
                        .Identifier);
                if (profileProperties == null)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError(
                        $"Failed to find profile {this._accountViewModel.AccountDisplayName} to edit");
                    this._results = new ActionResults().WithSuccess(false);
                    return;
                }

                var settingKey = profileProperties?.UniqueKey;
                credentialsChange = !string.Equals(profileProperties?.AccessKey,
                    this.Model.AccessKey, StringComparison.Ordinal) || !string.Equals(profileProperties?.SecretKey,
                    this.Model.SecretKey, StringComparison.Ordinal) || !string.Equals(profileProperties?.Region,
                    this.Model.Region.Id, StringComparison.Ordinal);
                nameChange = !string.Equals(this._accountViewModel.AccountDisplayName, this.Model.ProfileName,
                    StringComparison.Ordinal);

                this.Model.DisplayName = identifier?.DisplayName;
                var properties = new ProfileProperties
                {
                    AccessKey = this.Model.AccessKey?.Trim(),
                    SecretKey = this.Model.SecretKey?.Trim(),
                    Name = this.Model.ProfileName?.Trim(),
                    UniqueKey = settingKey,
                    Region = this.Model.Region?.Id?.Trim()
                };
                region = this.Model.Region;
                _credentialManager.CredentialManagerUpdated += HandleCredentialUpdate;

                if (nameChange)
                {
                    _credentialSettingsManager.RenameProfile(
                        this._accountViewModel.Identifier,
                        identifier);
                }

                if (credentialsChange)
                {
                    _credentialSettingsManager.UpdateProfile(identifier, properties);
                }


                mre.WaitOne(2000);
                this._accountViewModel.ReloadFromPersistence(this.Model.DisplayName.Trim());
                this._results = new ActionResults().WithSuccess(true)
                    .WithFocalname(this.Model.DisplayName)
                    .WithParameter(NAME_CHANGE_PARAMETER, nameChange)
                    .WithParameter(CREDENTIALS_CHANGE_PARAMETER, credentialsChange);
            }
            catch
            {
                this._results = new ActionResults().WithSuccess(false)
                    .WithFocalname(this.Model.DisplayName)
                    .WithParameter(NAME_CHANGE_PARAMETER, nameChange)
                    .WithParameter(CREDENTIALS_CHANGE_PARAMETER, credentialsChange);
            }
            finally
            {
                _credentialManager.CredentialManagerUpdated -= HandleCredentialUpdate;
            }
        }
    }
}

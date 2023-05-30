using System;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Account.View;
using Amazon.AWSToolkit.Account.Model;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;

using System.Threading;

using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Context;
using System.Linq;
using System.Windows;

using Amazon.AWSToolkit.Telemetry;
using Amazon.AWSToolkit.Telemetry.Model;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

namespace Amazon.AWSToolkit.Account.Controller
{
    public class LegacyRegisterAccountController
    {
        private readonly LegacyRegisterAccountModel _model;
        protected LegacyRegisterAccountControl _control;
        protected bool DefaultProfileNameInUse;
        protected ActionResults _results;
        protected readonly ToolkitContext ToolkitContext;

        public LegacyRegisterAccountController(ToolkitContext toolkitContext)
        {
            ToolkitContext = toolkitContext;
            this._model = new LegacyRegisterAccountModel(toolkitContext);
        }

        public LegacyRegisterAccountModel Model => this._model;

        /// <summary>
        /// Overload to record telemetry when the base class is invoked
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public ActionResults Execute(BaseMetricSource source)
        {
            ActionResults actionResults = null;

            void Invoke() => actionResults = Execute();

            void Record(ITelemetryLogger _) => RecordAddMetric(actionResults, source);

            ToolkitContext.TelemetryLogger.InvokeAndRecord(Invoke, Record);
            return actionResults;
        }

        public virtual ActionResults Execute()
        {
            this.Model.StorageLocationVisibility = System.Windows.Visibility.Visible;
            this._control = new LegacyRegisterAccountControl(this);
            this.LoadModel();
            CustomizeControl(this._control);
            if (ToolkitContext.ToolkitHost.ShowInModalDialogWindow(this._control, MessageBoxButton.OKCancel))
            {
                return this._results;
            }
       
            return ActionResults.CreateCancelled().WithSuccess(false);
        }

        protected virtual void CustomizeControl(LegacyRegisterAccountControl control)
        {
        }

        protected virtual void LoadModel()
        {
            // if this is the first account, seed the display name to 'default'
            // like the first-run experience
            var identifiers = ToolkitContext.CredentialManager.GetCredentialIdentifiers();
            if(identifiers.Count == 0)
            {
                _model.ProfileName = "default";
            }
            else
            {
                var defaultCredential = identifiers.FirstOrDefault(x=>string.Equals(x.ProfileName, "default"));
                if (defaultCredential != null)
                {
                    DefaultProfileNameInUse = true;
                }
            }
        
            this.Model.InitializeDefaultPartition();
        }

        public bool PromptToUseDefaultName => !DefaultProfileNameInUse;

        public virtual void Persist()
        {
            ICredentialIdentifier identifier = null;
            ToolkitRegion region = null;
            ManualResetEvent mre = new ManualResetEvent(false);
            EventHandler<EventArgs> HandleCredentialUpdate = (sender, args) =>
            {
                var ide = ToolkitContext.CredentialManager.GetCredentialIdentifierById(identifier?.Id);
                if (ide != null && region != null)
                {
                    mre.Set();
                    ToolkitContext.ConnectionManager.ChangeConnectionSettings(identifier, region);
                }
            };

            try
            {
                this.Model.UniqueKey = Guid.NewGuid();

                if (this.Model.SelectedStorageType == StorageTypes.DotNetEncryptedStore)
                {
                    identifier = new SDKCredentialIdentifier(this.Model.ProfileName.Trim());
                }
                else
                {
                    identifier = new SharedCredentialIdentifier(this.Model.ProfileName.Trim());
                }

                this.Model.CredentialId = identifier?.Id;
                this.Model.ProfileName = identifier?.ProfileName;
                this.Model.DisplayName = identifier?.DisplayName;
                region = this.Model.Region;
                var properties = new ProfileProperties
                {
                    Name = this.Model.ProfileName.Trim(),
                    AccessKey = this.Model.AccessKey?.Trim(),
                    SecretKey = this.Model.SecretKey?.Trim(),
                    UniqueKey = this.Model.UniqueKey.ToString(),
                    Region = this.Model.Region?.Id
                };

                ToolkitContext.CredentialManager.CredentialManagerUpdated += HandleCredentialUpdate;

                // create profile ensures profile has unique key and registers it
                ToolkitContext.CredentialSettingsManager.CreateProfile(identifier, properties);
                

                mre.WaitOne(2000);
                this._results = new ActionResults().WithSuccess(true).WithFocalname(this.Model.ProfileName);
            }
            catch(Exception e)
            {
                this._results = ActionResults.CreateFailed(e).WithFocalname(this.Model.ProfileName);
            }
            finally
            {
                ToolkitContext.CredentialManager.CredentialManagerUpdated -= HandleCredentialUpdate;
            }
        }

        private void RecordAddMetric(ActionResults actionResults, BaseMetricSource source)
        {
            ToolkitContext.TelemetryLogger.RecordAwsModifyCredentials(new AwsModifyCredentials()
            {
                AwsAccount = ToolkitContext.ConnectionManager?.ActiveAccountId ?? MetadataValue.NotSet,
                AwsRegion = MetadataValue.NotApplicable,
                Result = actionResults.AsTelemetryResult(),
                CredentialModification = CredentialModification.Add,
                Source = source.Location,
                ServiceType = source.Service,
                Reason = TelemetryHelper.GetMetricsReason(actionResults?.Exception)
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AWSToolkit.Util;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class ViewKeyPairsController : FeatureController<ViewKeyPairsModel>
    {
        private readonly ToolkitContext _toolkitContext;
        private ViewKeyPairsControl _control;

        public ViewKeyPairsController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        protected override void DisplayView()
        {
            this._control = new ViewKeyPairsControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);
        }

        public void LoadModel()
        {
            RefreshKeyPairs();
        }


        public void RefreshKeyPairs()
        {
            var response = this.EC2Client.DescribeKeyPairs(new DescribeKeyPairsRequest());            

            var account = this.FeatureViewModel.AccountViewModel;
            var region = this.FeatureViewModel.Region.Id;
            ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
            {
                this.Model.KeyPairs.Clear();
                foreach (var keyPair in response.KeyPairs.OrderBy(x => x.KeyName.ToLower()))
                {
                    this.Model.KeyPairs.Add(new KeyPairWrapper(account, region, keyPair));
                }
            }));
        }

        public void ResetSelection(IList<KeyPairWrapper> keyPairs)
        {
            this.Model.SelectedKeys.Clear();
            foreach (var keyPair in keyPairs)
            {
                this.Model.SelectedKeys.Add(keyPair);
            }
        }

        public ActionResults DeleteKeyPairs()
        {
            var controller = new DeleteKeyPairController();
            var results = controller.Execute((EC2KeyPairsViewModel)this.FeatureViewModel, this.Model.SelectedKeys);

            if (results.Success)
            {
                this.RefreshKeyPairs();
            }
            return results;
        }

        public ActionResults CreateKeyPair()
        {
            var controller = new CreateKeyPairController();
            var results = controller.Execute(this.FeatureViewModel as EC2KeyPairsViewModel);

            if (results.Success)
            {
                this.RefreshKeyPairs();
            }
            return results;
        }

        public ActionResults ClearPrivateKeys(IList<KeyPairWrapper> keys)
        {
            if (!ToolkitFactory.Instance.ShellProvider.Confirm("Clear Private Key(s)", "Are you sure you want to clear the private key(s)?"))
            {
                return ActionResults.CreateCancelled();
            }

            foreach (var key in keys)
            {
                if (key.IsStoredLocally)
                {
                    KeyPairLocalStoreManager.Instance.ClearPrivateKey(this.FeatureViewModel.AccountViewModel.SettingsUniqueKey,
                        this.FeatureViewModel.Region.Id, key.NativeKeyPair.KeyName);
                    key.RaiseStoredLocallyEvent();
                }
            }

            return new ActionResults().WithSuccess(true);
        }

        public ActionResults ImportPrivatekey(KeyPairWrapper key, string file)
        {
            string privateKey = null;
            using (StreamReader reader = new StreamReader(file))
            {
                privateKey = reader.ReadToEnd();
            }

            KeyPairLocalStoreManager.Instance.SavePrivateKey(this.FeatureViewModel.AccountViewModel.SettingsUniqueKey,
                this.FeatureViewModel.Region.Id, key.NativeKeyPair.KeyName, privateKey);
            key.RaiseStoredLocallyEvent();

            return new ActionResults().WithSuccess(true);
        }

        public ActionResults ExportPrivatekey(KeyPairWrapper key, string file)
        {
            string privateKey = KeyPairLocalStoreManager.Instance.GetPrivateKey(this.FeatureViewModel.AccountViewModel.SettingsUniqueKey,
                this.FeatureViewModel.Region.Id, key.NativeKeyPair.KeyName);

            using (StreamWriter writer = new StreamWriter(file))
            {
                writer.Write(privateKey);
            }

            return new ActionResults().WithSuccess(true);
        }

        public void RecordCreateKeyPair(ActionResults result)
        {
            var data = CreateMetricData<Ec2CreateKeyPair>(result);
            data.Result = result.AsTelemetryResult();
            _toolkitContext.TelemetryLogger.RecordEc2CreateKeyPair(data);
        }

        public void RecordDeleteKeyPair(int count, ActionResults result)
        {
            var data = CreateMetricData<Ec2DeleteKeyPair>(result);
            data.Result = result.AsTelemetryResult();
            data.Value = count;
            _toolkitContext.TelemetryLogger.RecordEc2DeleteKeyPair(data);
        }

        public void RecordClearPrivateKey(int count, ActionResults result)
        {
            var data = CreateMetricData<Ec2ClearPrivateKey>(result);
            data.Result = result.AsTelemetryResult();
            data.Value = count;
            _toolkitContext.TelemetryLogger.RecordEc2ClearPrivateKey(data);
        }

        public void RecordImportPrivateKey(ActionResults result)
        {
            var data = CreateMetricData<Ec2ImportPrivateKey>(result);
            data.Result = result.AsTelemetryResult();
            _toolkitContext.TelemetryLogger.RecordEc2ImportPrivateKey(data);
        }

        public void RecordExportPrivateKey(ActionResults result)
        {
            var data = CreateMetricData<Ec2ExportPrivateKey>(result);
            data.Result = result.AsTelemetryResult();
            _toolkitContext.TelemetryLogger.RecordEc2ExportPrivateKey(data);
        }

        /// <summary>
        /// Utility method to create a metric object and pre-populate it with standardized fields.
        /// </summary>
        /// <typeparam name="T">Metric object to instantiate</typeparam>
        /// <param name="result">Operation result, used to populate some of the metric fields</param>
        private T CreateMetricData<T>(ActionResults result) where T : BaseTelemetryEvent, new()
        {
            var metricData = new T();
            metricData.AwsAccount = AwsConnectionSettings?.GetAccountId(_toolkitContext.ServiceClientManager) ??
                                    MetadataValue.Invalid;
            metricData.AwsRegion = AwsConnectionSettings?.Region?.Id ?? MetadataValue.Invalid;
            metricData.Reason = TelemetryHelper.GetMetricsReason(result.Exception);

            return metricData;
        }
    }
}

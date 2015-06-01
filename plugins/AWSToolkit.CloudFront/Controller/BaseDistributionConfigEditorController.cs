using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.CloudFront.Nodes;
using Amazon.AWSToolkit.S3.Nodes;
using Amazon.AWSToolkit.CloudFront.View;
using Amazon.AWSToolkit.CloudFront.Model;
using Amazon.AWSToolkit;

using Amazon.CloudFront;
using Amazon.CloudFront.Model;

using log4net;

namespace Amazon.AWSToolkit.CloudFront.Controller
{
    public abstract class BaseDistributionConfigEditorController : BaseContextCommand
    {
        ILog _logger = LogManager.GetLogger(typeof(BaseDistributionConfigEditorController));

        protected IAmazonCloudFront _cfClient;
        AccountViewModel _accountModel;
        BaseConfigModel _baseModel;

        protected void Initialize(IAmazonCloudFront cfClient, AccountViewModel accountModel, BaseConfigModel baseModel, bool loadModel)
        {
            this._cfClient = cfClient;
            this._accountModel = accountModel;
            this._baseModel = baseModel;

            if (loadModel)
            {
                LoadModel();
            }
        }

        public BaseConfigModel BaseModel
        {
            get { return this._baseModel; }
        }

        public virtual void LoadModel()
        {
            foreach (IViewModel viewModel in this._accountModel.FindSingleChild<IS3RootViewModel>(false).Children)
            {
                IS3BucketViewModel bucketViewModel = viewModel as IS3BucketViewModel;
                if (bucketViewModel == null)
                    continue;

                this._baseModel.AllBucketNames.Add(bucketViewModel.Name);
            }

            try
            {
                var response = this._cfClient.ListCloudFrontOriginAccessIdentities();
                foreach (CloudFrontOriginAccessIdentitySummary oai in response.CloudFrontOriginAccessIdentityList.Items)
                {
                    this._baseModel.AddCloudFrontOriginAccessIdentity(oai);
                }
            }
            catch(Exception e)
            {
                this._logger.Error("Error loading identities", e);
            }
        }

        public void CreateDistributionBucket()
        {
            string bucketName = this.CreateBucket();
            if (!string.IsNullOrEmpty(bucketName))
            {
                this._baseModel.S3BucketOrigin = bucketName;
            }
        }

        public void CreateLoggingBucket()
        {
            string bucketName = this.CreateBucket();
            if (!string.IsNullOrEmpty(bucketName))
            {
                this._baseModel.LoggingTargetBucket = bucketName;
            }
        }

        private string CreateBucket()
        {
            if (this._accountModel != null)
            {
                var model = this._accountModel.FindSingleChild<IS3RootViewModel>(false);
                var meta = model.MetaNode as IS3RootViewMetaNode;
                ActionResults results = meta.OnCreate(model);

                if (results.Success)
                {
                    meta.OnCreateResponse(model, results);
                    return results.FocalName;
                }
            }

            return null;
        }

        public void CreateOriginAccessIdentity()
        {
            CreateOriginAccessIdentityController controller = new CreateOriginAccessIdentityController(this._cfClient);
            if (controller.Execute())
            {
                var oai = controller.Model.OriginAccessIdentity;
                var wrapper = new OriginAccessIdentitiesWrapper(oai.Id, oai.CloudFrontOriginAccessIdentityConfig.Comment, oai.S3CanonicalUserId);
                this._baseModel.AllOriginAccessIdentities.Add(wrapper);
                this._baseModel.SelectedOriginAccessIdentityWrapper = wrapper;
            }
        }

        protected internal static void OpenDistributionUrl(string domainName, string scheme, string port)
        {
            var u = new UriBuilder(domainName);
            u.Scheme = !string.IsNullOrEmpty(scheme) ? scheme : "http";
            if (!string.IsNullOrEmpty(port))
                u.Port = int.Parse(port);

            // use u.Uri so port, if specified but default for scheme, gets dropped
            Process.Start(new ProcessStartInfo(u.Uri.ToString()));
        }
    }
}

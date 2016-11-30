using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.CloudFront;
using Amazon.CloudFront.Model;

using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.CloudFront.Nodes;

using log4net;

namespace Amazon.AWSToolkit.CloudFront.Nodes
{
    public abstract class CloudFrontBaseDistributionViewModel : AbstractViewModel, ICloudFrontBaseDistributionViewModel
    {
        ILog logger = LogManager.GetLogger(typeof(CloudFrontBaseDistributionViewModel));
        IAmazonCloudFront _cfClient;
        string _distributionId;

        public CloudFrontBaseDistributionViewModel(BaseDistributeViewMetaNode metaNode, CloudFrontRootViewModel viewModel, string distributionId, string name)
            : base(metaNode, viewModel, name)
        {
            this._distributionId = distributionId;
            this._cfClient = viewModel.CFClient;
        }

        public CloudFrontRootViewModel CloudFrontRootViewModel
        {
            get { return this.Parent as CloudFrontRootViewModel; }
        }

        public string DistributionId
        {
            get { return this._distributionId; }
        }

        public IAmazonCloudFront CFClient
        {
            get { return this._cfClient; }
        }

        public string GetETag()
        {
            // ticket 0022623643, do not cache etag
            try
            {
                return FetchETag();
            }
            catch(Exception e)
            {
                logger.Error("Error fetch etag for distribution " + this.Name + ".", e);
                return null;
            }
        }

        public string DomainName
        {
            get;
            protected set;
        }

        public Aliases Aliases
        {
            get;
            protected set;
        }

        protected abstract string FetchETag();
    }
}

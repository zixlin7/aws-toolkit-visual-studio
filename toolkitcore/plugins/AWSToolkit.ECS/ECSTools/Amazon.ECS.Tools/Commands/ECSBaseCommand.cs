using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

using Amazon.CloudWatchEvents;
using Amazon.ECR;
using Amazon.ECS;

using System.Reflection;
using ThirdParty.Json.LitJson;
using System.Text;
using System.IO;
using Amazon.Common.DotNetCli.Tools.Commands;
using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;

namespace Amazon.ECS.Tools.Commands
{

    public abstract class ECSBaseCommand : BaseCommand<DockerToolsDefaults>
    {




        public ECSBaseCommand(IToolLogger logger, string workingDirectory)
            : base(logger, workingDirectory)
        {
        }

        public ECSBaseCommand(IToolLogger logger, string workingDirectory, IList<CommandOption> possibleOptions, string[] args)
            : base(logger, workingDirectory, possibleOptions, args)
        {
        }

        protected override string ToolName => "AWSECSToolsDotnet";

        IAmazonCloudWatchEvents _cweClient;
        public IAmazonCloudWatchEvents CWEClient
        {
            get
            {
                if (this._cweClient == null)
                {
                    SetUserAgentString();

                    var config = new AmazonCloudWatchEventsConfig();
                    config.RegionEndpoint = DetermineAWSRegion();

                    this._cweClient = new AmazonCloudWatchEventsClient(DetermineAWSCredentials(), config);
                }
                return this._cweClient;
            }
            set { this._cweClient = value; }
        }

        IAmazonECR _ecrClient;
        public IAmazonECR ECRClient
        {
            get
            {
                if (this._ecrClient == null)
                {
                    SetUserAgentString();

                    var config = new AmazonECRConfig();
                    config.RegionEndpoint = DetermineAWSRegion();

                    this._ecrClient = new AmazonECRClient(DetermineAWSCredentials(), config);
                }
                return this._ecrClient;
            }
            set { this._ecrClient = value; }
        }

        IAmazonECS _ecsClient;
        public IAmazonECS ECSClient
        {
            get
            {
                if (this._ecsClient == null)
                {
                    SetUserAgentString();

                    var config = new AmazonECSConfig();
                    config.RegionEndpoint = DetermineAWSRegion();

                    this._ecsClient = new AmazonECSClient(DetermineAWSCredentials(), config);
                }
                return this._ecsClient;
            }
            set { this._ecsClient = value; }
        }
    }
}

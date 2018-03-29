using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Threading;

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.S3.View;
using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit.S3.Nodes;
using Amazon.AWSToolkit.SNS.Nodes;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.PolicyEditor.Model;

namespace Amazon.AWSToolkit.S3.Controller
{
    public class BucketPropertiesController : BaseContextCommand
    {
        IAmazonS3 _s3Client;
        BucketPropertiesModel _model;
        BucketPropertiesControl _control;
        S3BucketViewModel _bucketModel;

        public BucketPropertiesController()
        {
        }

        public BucketPropertiesController(IAmazonS3 s3Client, string bucketName)
            : this(s3Client, new BucketPropertiesModel(bucketName))
        {
        }

        public BucketPropertiesController(IAmazonS3 s3Client, BucketPropertiesModel model)
        {
            this._s3Client = s3Client;
            this._model = model;
        }

        public IAmazonS3 S3Client
        {
            get { return this._s3Client; }
        }

        public BucketPropertiesModel Model
        {
            get { return this._model; }
        }

        public override ActionResults Execute(IViewModel model)
        {
            this._bucketModel = model as S3BucketViewModel;
            if (this._bucketModel == null)
                return new ActionResults().WithSuccess(false);
     
            this._s3Client = this._bucketModel.S3Client;            

            this._model = new BucketPropertiesModel(this._bucketModel.Name);            
            this._control = new BucketPropertiesControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);

            return new ActionResults().WithSuccess(true);
        }


        public void CreateBucket()
        {
            if (this._bucketModel != null)
            {
                S3RootViewModel model = this._bucketModel.S3RootViewModel;
                S3RootViewMetaNode meta = model.MetaNode as S3RootViewMetaNode;
                ActionResults results = meta.OnCreate(model);

                if (results.Success)
                {
                    this._model.LoggingTargetBucket = results.FocalName;
                    meta.OnCreateResponse(model, results);
                }
            }
        }

        public void AddEventConfiguration()
        {
            var controller = new AddEventConfigurationController();
            if (controller.Execute(this.S3Client, this.Model.BucketName, this._bucketModel.OverrideRegion, this._bucketModel.AccountViewModel))
            {
                this.RefreshNotifications();
            }
        }

        public void DeleteEventConfiguration(EventConfigurationModel configuration)
        {
            var getResponse = this._s3Client.GetBucketNotification(this._model.BucketName);

            var putRequest = new PutBucketNotificationRequest 
            { 
                BucketName = this._model.BucketName,
                TopicConfigurations = new List<TopicConfiguration>(),
                QueueConfigurations = new List<QueueConfiguration>(),
                LambdaFunctionConfigurations = new List<LambdaFunctionConfiguration>()
            };

            foreach (var item in getResponse.LambdaFunctionConfigurations)
            {
                if (configuration.TargetService == EventConfigurationModel.Service.Lambda && item.Id == configuration.Id)
                    continue;

                putRequest.LambdaFunctionConfigurations.Add(item);
            }
            foreach (var item in getResponse.QueueConfigurations)
            {
                if (configuration.TargetService == EventConfigurationModel.Service.SQS && item.Id == configuration.Id)
                    continue;

                putRequest.QueueConfigurations.Add(item);
            }
            foreach (var item in getResponse.TopicConfigurations)
            {
                if (configuration.TargetService == EventConfigurationModel.Service.SNS && item.Id == configuration.Id)
                    continue;

                putRequest.TopicConfigurations.Add(item);
            }

            this._s3Client.PutBucketNotification(putRequest);
            this.RefreshNotifications();
        }

        public void Refresh()
        {
            LoadPolicyDocument();
            loadModelPermissions();
            loadModelLogging();
            RefreshNotifications();
            loadWebSiteConfiguration();
            loadLifecycleConfiguration();

            this.Model.IsDirty = false;
        }

        public Permission AddPermission()
        {
            var permission = new Permission();
            addPropertyChangeHandler(permission);
            this.Model.PermissionEntries.Add(permission);
            return permission;
        }

        public LifecycleRuleModel AddLifecycleRule()
        {
            var rule = new LifecycleRuleModel();
            addPropertyChangeHandler(rule);
            this.Model.LifecycleRules.Add(rule);
            return rule;
        }

        void addPropertyChangeHandler(INotifyPropertyChanged property)
        {
            property.PropertyChanged += ((PropertyChangedEventHandler)((x, y) => this._model.IsDirty = true));
        }


        #region Persist Model

        public void Persist()
        {
            persistPermissions();
            persistLogging();
            persistNotifications();
            persistWebSiteConfiguration();
            persistPolicyDocument();
            persistLifecycleConfiguration();

            this._model.CommitState();
        }

        private void persistPolicyDocument()
        {
            if (!this._model.PolicyModel.IsDirty)
                return;

            var document = this._model.PolicyModel.GetPolicyDocument();
            if (this._model.PolicyModel.Statements.Count == 0)
            {
                var request = new DeleteBucketPolicyRequest() { BucketName = this._model.BucketName };
                this._s3Client.DeleteBucketPolicy(request);
            }
            else
            {
                var request = new PutBucketPolicyRequest() { BucketName = this._model.BucketName, Policy = document };
                this._s3Client.PutBucketPolicy(request);
            }
        }

        private void persistPermissions()
        {
            if (!this._model.HasPermissionsChanged)
                return;

            S3AccessControlList list = Permission.ConvertToAccessControlList(this._model.PermissionEntries, Permission.PermissionMode.Bucket);
            list.Owner = this._model.BucketOwner;

            this._s3Client.PutACL(new PutACLRequest() { BucketName = this._model.BucketName, AccessControlList = list });
        }

        private void checkLoggingTargetBucketACL()
        {
            try
            {
                S3AccessControlList targetACL = this._s3Client.GetACL(new GetACLRequest() { BucketName = this._model.LoggingTargetBucket }).AccessControlList;
                bool hasWrite = targetACL.Grants.Any(item => Permission.CommonURIGrantee.LOG_DELIVER_URI.URI.Equals(item.Grantee.URI) && item.Permission == S3Permission.WRITE);
                bool hasReadACP = targetACL.Grants.Any(item => Permission.CommonURIGrantee.LOG_DELIVER_URI.URI.Equals(item.Grantee.URI) && item.Permission == S3Permission.READ_ACP);

                if (!hasWrite)
                {
                    targetACL.AddGrant(new S3Grantee() { URI = Permission.CommonURIGrantee.LOG_DELIVER_URI.URI }, S3Permission.WRITE);
                }
                if (!hasReadACP)
                {
                    targetACL.AddGrant(new S3Grantee() { URI = Permission.CommonURIGrantee.LOG_DELIVER_URI.URI }, S3Permission.READ_ACP);
                }

                if (!hasReadACP || !hasWrite)
                {
                    this._s3Client.PutACL(new PutACLRequest() { BucketName = this._model.LoggingTargetBucket, AccessControlList = targetACL });
                }
            }
            catch { }
        }
        private void persistLogging()
        {
            if (!this._model.HasLoggingChanged)
                return;

            PutBucketLoggingRequest request = new PutBucketLoggingRequest();
            request.BucketName = this._model.BucketName;

            if (this._model.IsLoggingEnabled)
            {
                checkLoggingTargetBucketACL();

                S3BucketLoggingConfig config = new S3BucketLoggingConfig();
                config.TargetBucketName = this._model.LoggingTargetBucket;
                config.TargetPrefix = this._model.LoggingTargetPrefix;
                config.Grants = new List<S3Grant>();

                request.LoggingConfig = config;
            }

            this._s3Client.PutBucketLogging(request);
        }

        private void persistLifecycleConfiguration()
        {
            if (!this._model.HasLifecycleRulesChanged)
                return;

            if (this._model.LifecycleRules.Count == 0)
            {
                this._s3Client.DeleteLifecycleConfiguration(new DeleteLifecycleConfigurationRequest() { BucketName = this._model.BucketName });
            }
            else
            {
                var config = new LifecycleConfiguration();
                foreach (var entry in this._model.LifecycleRules)
                {
                    var rule = entry.ConvertToRule();

                    if (rule.Expiration == null && rule.Transition == null)
                        continue;

                    if (string.IsNullOrEmpty(rule.Id))
                    {
                        entry.RuleId = rule.Id = generateLifecycleRuleName(rule);                        
                    }

                    config.Rules.Add(rule);
                }
                this._s3Client.PutLifecycleConfiguration(new PutLifecycleConfigurationRequest() { BucketName = this._model.BucketName, Configuration = config });
            }            
        }

        string generateLifecycleRuleName(LifecycleRule rule)
        {
            string baseName = rule.Expiration != null ? "Expiration for data" : "Transition for data";

            string currentIteration = baseName;
            for (int i = 2; true;i++ )
            {
                bool found = false;
                foreach (var entry in this._model.LifecycleRules)
                {
                    if (!string.IsNullOrEmpty(entry.RuleId) && entry.RuleId.Equals(currentIteration))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return currentIteration;
                }

                currentIteration = baseName + i;
            }
        }

        private void persistNotifications()
        {
        }

        private void persistWebSiteConfiguration()
        {
            if (!this._model.HasWebSiteChanged)
                return;

            if(!this._model.IsWebSiteEnabled)
            {
                var request = new DeleteBucketWebsiteRequest() { BucketName = this._model.BucketName };

                this._s3Client.DeleteBucketWebsite(request);
            }
            else
            {
                PutBucketWebsiteRequest request = new PutBucketWebsiteRequest()
                {
                    BucketName = this._model.BucketName,
                    WebsiteConfiguration = new WebsiteConfiguration()
                    {
                        IndexDocumentSuffix = this._model.WebSiteIndexDocument,
                        ErrorDocument = this._model.WebSiteErrorDocument
                    }
                };

                this._s3Client.PutBucketWebsite(request);
            }

        }

        #endregion

        #region Load Model


        public void LoadModel()
        {
            var listResponse = this._s3Client.ListBuckets();

            List<string> allBucketNames = new List<string>();
            S3Bucket s3Bucket = null;
            foreach (var b in listResponse.Buckets)
            {
                allBucketNames.Add(b.BucketName);
                if (b.BucketName.Equals(this._model.BucketName))
                {
                    s3Bucket = b;
                }
            }

            if (s3Bucket == null)
                return;

            this._model.AllBucketNames = allBucketNames;
            this._model.CreationDate = s3Bucket.CreationDate;
            this._model.BucketOwner = listResponse.Owner;

            var locationResponse = this._s3Client.GetBucketLocation(new GetBucketLocationRequest() { BucketName = this._model.BucketName });

            if (locationResponse.Location == S3Region.US.ToString())
                this._model.RegionSystemName = "us-east-1";
            else if (locationResponse.Location == S3Region.EU.ToString())
                this._model.RegionSystemName = "eu-west-1";
            else
                this._model.RegionSystemName = locationResponse.Location;

            var region = RegionEndPointsManager.GetInstance().GetRegion(this._model.RegionSystemName);
            if (region != null)
            {
                this._model.RegionDisplayName = region.DisplayName;
            }
            else
            {
                this._model.RegionDisplayName = this._model.RegionSystemName;
            }

            LoadPolicyDocument();
            loadModelPermissions();
            loadModelLogging();
            RefreshNotifications();
            loadWebSiteConfiguration();
            loadLifecycleConfiguration();

            this.Model.IsDirty = false;
        }

        private void LoadPolicyDocument()
        {
            var request = new GetBucketPolicyRequest() { BucketName = this._model.BucketName };
            var response = this._s3Client.GetBucketPolicy(request);

            if (this._model.PolicyModel == null)
            {
                var model = new PolicyModel(PolicyModel.PolicyModelMode.S3);
                model.OnChange += ((EventHandler)((x, y) => this._model.IsDirty = true));
                this._model.PolicyModel = model;
            }

            this._model.PolicyModel.ImportPolicy(response.Policy, true);
            
        }

        private void loadModelPermissions()
        {
            this.Model.PermissionEntries.Clear();
            var getACLResponse = this._s3Client.GetACL(new GetACLRequest() { BucketName = this._model.BucketName });

            Permission.LoadPermissions(this._model.PermissionEntries, getACLResponse.AccessControlList);
            foreach (var permisison in this.Model.PermissionEntries)
            {
                addPropertyChangeHandler(permisison);
            }
        }

        private void loadLifecycleConfiguration()
        {
            this.Model.LifecycleRules.Clear();
            var getResponse = this._s3Client.GetLifecycleConfiguration(new GetLifecycleConfigurationRequest() { BucketName = this._model.BucketName });

            if (getResponse.Configuration == null || getResponse.Configuration.Rules == null)
                return;

            foreach (var rule in getResponse.Configuration.Rules)
            {
                var ruleModel = new LifecycleRuleModel(rule);
                this.Model.LifecycleRules.Add(ruleModel);
                addPropertyChangeHandler(ruleModel);
            }
        }

        private void loadModelLogging()
        {
            var getLoggingResponse = this._s3Client.GetBucketLogging(new GetBucketLoggingRequest() { BucketName = this._model.BucketName });

            if (getLoggingResponse.BucketLoggingConfig != null && getLoggingResponse.BucketLoggingConfig.TargetBucketName != null)
            {
                this._model.IsLoggingEnabled = true;
                this._model.LoggingTargetBucket = getLoggingResponse.BucketLoggingConfig.TargetBucketName;
                this._model.LoggingTargetPrefix = getLoggingResponse.BucketLoggingConfig.TargetPrefix;
            }
        }

        public void RefreshNotifications()
        {
            var getNotificationsResponse = this._s3Client.GetBucketNotification(new GetBucketNotificationRequest() { BucketName = this._model.BucketName });

            this._model.EventConfigurations.Clear();
            foreach (var config in getNotificationsResponse.TopicConfigurations)
            {
                var editable = new EventConfigurationModel
                {
                    Id = config.Id,
                    TargetService = EventConfigurationModel.Service.SNS,
                    ResourceArn = config.Topic
                };

                foreach (var eventType in config.Events)
                {
                    editable.EventTypes.Add(eventType);
                }

                ParseFilter(config.Filter, editable);
                this._model.EventConfigurations.Add(editable);
            }
            foreach (var config in getNotificationsResponse.QueueConfigurations)
            {
                var editable = new EventConfigurationModel
                {
                    Id = config.Id,
                    TargetService = EventConfigurationModel.Service.SQS,
                    ResourceArn = config.Queue
                };

                foreach (var eventType in config.Events)
                {
                    editable.EventTypes.Add(eventType);
                }

                ParseFilter(config.Filter, editable);
                this._model.EventConfigurations.Add(editable);
            }
            foreach (var config in getNotificationsResponse.LambdaFunctionConfigurations)
            {
                var editable = new EventConfigurationModel
                {
                    Id = config.Id,
                    TargetService = EventConfigurationModel.Service.Lambda,
                    ResourceArn = config.FunctionArn
                };

                foreach (var eventType in config.Events)
                {
                    editable.EventTypes.Add(eventType);
                }

                ParseFilter(config.Filter, editable);
                this._model.EventConfigurations.Add(editable);
            }
        }

        private void ParseFilter(Filter filter, EventConfigurationModel model)
        {
            if (filter == null || filter.S3KeyFilter == null)
                return;

            foreach(var rule in filter.S3KeyFilter.FilterRules)
            {
                if (string.Equals(S3Constants.NOTIFICATION_FILTER_KEY_PREFIX, rule.Name, StringComparison.OrdinalIgnoreCase))
                    model.Prefix = rule.Value;
                if (string.Equals(S3Constants.NOTIFICATION_FILTER_KEY_SUFFIX, rule.Name, StringComparison.OrdinalIgnoreCase))
                    model.Suffix = rule.Value;
            }
        }

        private void loadWebSiteConfiguration()
        {
            this._model.WebSiteEndPoint = string.Format("http://{0}.s3-website-{1}.amazonaws.com/", 
                this._model.BucketName, this._model.RegionSystemName);

            var response = this._s3Client.GetBucketWebsite(new GetBucketWebsiteRequest() { BucketName = this._model.BucketName });

            if (response.WebsiteConfiguration == null || string.IsNullOrEmpty(response.WebsiteConfiguration.IndexDocumentSuffix))
            {
                this._model.IsWebSiteEnabled = false;
                return;
            }

            this._model.IsWebSiteEnabled = true;
            this._model.WebSiteIndexDocument = response.WebsiteConfiguration.IndexDocumentSuffix;
            this._model.WebSiteErrorDocument = response.WebsiteConfiguration.ErrorDocument;
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Lambda.View;
using Amazon.AWSToolkit.Lambda.Model;
using Amazon.AWSToolkit.Lambda.Nodes;
using Amazon.Auth.AccessControlPolicy;
using Amazon.AWSToolkit.Context;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using log4net;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.SimpleWorkers;
using Amazon.KeyManagementService.Model;
using Amazon.KeyManagementService;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.AWSToolkit.MobileAnalytics;
using Amazon.AWSToolkit.Regions;
using Amazon.ECR;

namespace Amazon.AWSToolkit.Lambda.Controller
{
    public class ViewFunctionController : BaseContextCommand
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(ViewFunctionController));

        private static readonly string CloudWatchLogsServiceName =
            new AmazonCloudWatchLogsConfig().RegionEndpointServiceName;
        private static readonly string Ec2ServiceName =
            new AmazonEC2Config().RegionEndpointServiceName;
        private static readonly string KmsServiceName =
            new AmazonKeyManagementServiceConfig().RegionEndpointServiceName;
        private static readonly string SnsServiceName =
            new AmazonSimpleNotificationServiceConfig().RegionEndpointServiceName;
        private static readonly string SqsServiceName =
            new AmazonSQSConfig().RegionEndpointServiceName;

        private readonly ToolkitContext _toolkitContext;

        ViewFunctionModel _model;
        ViewFunctionControl _control;
        IAmazonLambda _lambdaClient;
        LambdaFunctionViewModel _viewModel;
        IAmazonEC2 _ec2Client;
        IAmazonKeyManagementService _kmsClient;
        IAmazonSimpleNotificationService _snsClient;
        IAmazonSQS _sqsClient;
        IAmazonECR _ecrClient;

        AccountViewModel _account;
        ToolkitRegion _region;

        AmazonCloudWatchLogsClient _logsClient;

        public ViewFunctionController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        /// <summary>
        /// Test only constructor initializing a ViewFunctionModel
        /// </summary>
        /// <param name="functionName">function name for view function model</param>
        /// <param name="functionArn">function arn for view function model</param>
        public ViewFunctionController(string functionName, string functionArn)
        {
            this._model =new ViewFunctionModel(functionName, functionArn);
        }

        public override ActionResults Execute(IViewModel model)
        {
            this._viewModel = model as LambdaFunctionViewModel;
            if (this._viewModel == null)
                return new ActionResults().WithSuccess(false);


            this._lambdaClient = this._viewModel.LambdaClient;
            this._model = new ViewFunctionModel(this._viewModel.FunctionName, this._viewModel.FunctionArn);
            this._control = new ViewFunctionControl(this);

            this._control.PropertyChanged += _control_PropertyChanged;

            this._account = ((LambdaFunctionViewModel) this._viewModel).LambdaRootViewModel.AccountViewModel;
            this._region = ((LambdaFunctionViewModel) this._viewModel).LambdaRootViewModel.Region;

            ConstructClients();

            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);

            return new ActionResults()
                .WithSuccess(true);
        }

        void _control_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("vpcsubnets", StringComparison.OrdinalIgnoreCase))
            {
                if (this._control.SubnetsSpanVPCs)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Invalid Subnets Selection",
                        "The selected subnets must belong to the same VPC.");
                }
                else
                {
                    var subnets = this._control.SelectedSubnets;
                    if (subnets != null && subnets.Any())
                    {
                        var vpcId = subnets.First().VpcId;
                        RefreshSecurityGroupsForVpc(vpcId);

                        if (!this._model.IsDirty)
                            this._model.IsDirty = HaveVpcSettingsChanged(vpcId, subnets);
                    }
                    else
                    {
                        RefreshSecurityGroupsForVpc(null);
                        if (!this._model.IsDirty)
                            this._model.IsDirty = HaveVpcSettingsChanged(null, null);
                    }
                }
            }

            if (e.PropertyName.Equals("kmskey", StringComparison.OrdinalIgnoreCase) && !this._model.IsDirty)
            {
                var selectedKey = this._control.SelectedKMSKey;
                if (selectedKey == null || selectedKey == KeyAndAliasWrapper.LambdaDefaultKMSKey.Key)
                {
                    // user selected default key, so dirty if they previously had a custom one
                    if (!string.IsNullOrEmpty(this._model.KMSKeyArn))
                        this._model.IsDirty = true;
                }
                else
                {
                    // dirty if a user selected a different key
                    if (string.IsNullOrEmpty(this._model.KMSKeyArn))
                        this._model.IsDirty = true;
                    else if (!this._model.KMSKeyArn.Equals(selectedKey.KeyArn))
                        this._model.IsDirty = true;
                }
            }

            if (e.PropertyName.Equals("DLQTargets", StringComparison.OrdinalIgnoreCase) && !this._model.IsDirty)
            {
                var selectedArn = this._control.SelectedDLQTargetArn;
                if (!string.Equals(selectedArn, this.Model.DLQTargetArn, StringComparison.Ordinal))
                {
                    this._model.IsDirty = true;
                }
            }

            // envvars is a complex control collection, so easiest to just flag as dirty for now
            if (e.PropertyName.Equals("EnvironmentVariables"))
            {
                this._model.IsDirty = true;
            }
        }

        bool HaveVpcSettingsChanged(string selectedVpcId, IEnumerable<SubnetWrapper> selectedSubnets)
        {
            if (string.IsNullOrEmpty(selectedVpcId))
            {
                return this._model.VpcConfig != null && !string.IsNullOrEmpty(this._model.VpcConfig.VpcId);
            }
            else
            {
                if (this._model.VpcConfig == null || string.IsNullOrEmpty(this._model.VpcConfig.VpcId))
                    return true;

                if (!string.Equals(selectedVpcId, this._model.VpcConfig.VpcId, StringComparison.OrdinalIgnoreCase))
                    return true;

                var selectedGroups = this._control.SelectedSecurityGroups;
                return !(SubnetSelectionsMatch(selectedSubnets) && SecurityGroupSelectionsMatch(selectedGroups));
            }
        }

        /// <summary>
        /// Inspect the current and selected subnets to determine if any changes have been made.
        /// We know the function has an existing VpcConfig at this stage.
        /// </summary>
        /// <param name="selectedSubnets"></param>
        /// <returns></returns>
        bool SubnetSelectionsMatch(IEnumerable<SubnetWrapper> selectedSubnets)
        {
            var existingSubnets = new HashSet<string>(this.Model.VpcConfig.SubnetIds);
            foreach (var subnet in selectedSubnets)
            {
                if (existingSubnets.Contains(subnet.SubnetId))
                    existingSubnets.Remove(subnet.SubnetId);
                else
                    return false;
            }

            return existingSubnets.Count == 0;
        }

        /// <summary>
        /// Inspect the current and selected groups to determine if any changes have been made.
        /// We know the function has an existing VpcConfig at this stage.
        /// </summary>
        /// <param name="selectedGroups"></param>
        /// <returns></returns>
        bool SecurityGroupSelectionsMatch(IEnumerable<SecurityGroupWrapper> selectedGroups)
        {
            var existingGroups = new HashSet<string>(this.Model.VpcConfig.SecurityGroupIds);
            foreach (var group in selectedGroups)
            {
                if (existingGroups.Contains(group.GroupId))
                    existingGroups.Remove(group.GroupId);
                else
                    return false;
            }

            return existingGroups.Count == 0;
        }

        void ConstructClients()
        {
            if (_toolkitContext.RegionProvider.IsServiceAvailable(CloudWatchLogsServiceName, _region.Id))
            {
                _logsClient = _account.CreateServiceClient<AmazonCloudWatchLogsClient>(_region);
            }

            if (_toolkitContext.RegionProvider.IsServiceAvailable(Ec2ServiceName, _region.Id))
            {
                _ec2Client = _account.CreateServiceClient<AmazonEC2Client>(_region);
            }

            if (_toolkitContext.RegionProvider.IsServiceAvailable(KmsServiceName, _region.Id))
            {
                _kmsClient = _account.CreateServiceClient<AmazonKeyManagementServiceClient>(_region);
            }

            if (_toolkitContext.RegionProvider.IsServiceAvailable(SnsServiceName, _region.Id))
            {
                _snsClient = _account.CreateServiceClient<AmazonSimpleNotificationServiceClient>(_region);
            }

            if (_toolkitContext.RegionProvider.IsServiceAvailable(SqsServiceName, _region.Id))
            {
                _sqsClient = _account.CreateServiceClient<AmazonSQSClient>(_region);
            }

            _ecrClient = _account.CreateServiceClient<AmazonECRClient>(_region);
        }

        public ViewFunctionModel Model => this._model;

        public void LoadModel()
        {
            Refresh();
        }

        public Task<GetFunctionConfigurationResponse> GetFunctionConfigurationAsync(CancellationToken cancellationToken)
        {
            return this._lambdaClient.GetFunctionConfigurationAsync(this._model.FunctionName, cancellationToken);
        }

        public void Refresh()
        {
            RefreshFunctionConfiguration();
            RefreshAdvancedSettings();
            RefreshEventSources();
            RefreshLogs();
        }

        public void RefreshFunctionConfiguration()
        {
            RefreshFunctionConfiguration(this._lambdaClient);
        }

        public void RefreshFunctionConfiguration(IAmazonLambda lambdaClient){

            var response = lambdaClient.GetFunctionConfiguration(this._model.FunctionName);

            this._model.PackageType = response.PackageType;
            if (response.PackageType == PackageType.Image)
            {
                this._model.ImageCommand = JoinByComma(response.ImageConfigResponse?.ImageConfig?.Command);
                this._model.ImageEntrypoint = JoinByComma(response.ImageConfigResponse?.ImageConfig?.EntryPoint);
                this._model.ImageWorkingDirectory = response.ImageConfigResponse?.ImageConfig?.WorkingDirectory ?? string.Empty;

                var getFunctionResponse = lambdaClient.GetFunction(this._model.FunctionName);
                this._model.ImageUri = getFunctionResponse?.Code?.ImageUri ?? string.Empty;
            }
            this._model.CodeSize = response.CodeSize;
            this._model.Description = response.Description;
            this._model.FunctionArn = response.FunctionArn;
            this._model.Handler = response.Handler;
            this._model.Runtime = response.Runtime;
            this._model.LastModified = DateTime.Parse(response.LastModified);
            this._model.MemorySize = response.MemorySize;
            this._model.Role = response.Role;
            this._model.Timeout = response.Timeout;
            this._model.State = response.State;
            this._model.StateReasonCode = response.StateReasonCode;
            this._model.StateReason = response.StateReason;
            this._model.LastUpdateStatus = response.LastUpdateStatus;
            this._model.LastUpdateStatusReasonCode = response.LastUpdateStatusReasonCode;
            this._model.LastUpdateStatusReason = response.LastUpdateStatusReason;
            this._model.VpcConfig = response.VpcConfig;
            this._model.KMSKeyArn = response.KMSKeyArn;

            this._model.IsEnabledActiveTracing = response.TracingConfig?.Mode == TracingMode.Active;
            this._model.DLQTargetArn = response.DeadLetterConfig?.TargetArn;

            this._model.EnvironmentVariables.Clear();
            if (response.Environment != null && response.Environment.Variables != null)
            {
                var vars = response.Environment.Variables;
                foreach (var k in vars.Keys)
                {
                    this._model.EnvironmentVariables.Add(new WizardPages.PageUI.EnvironmentVariable
                    {
                        Variable = k,
                        Value = vars[k]
                    });
                }
            }

            this._model.IsDirty = false;
        }

        public void RefreshAdvancedSettings()
        {
            RefreshVpcSubnets();
            RefreshSecurityGroupsForVpc(this._model.VpcConfig?.VpcId);
            RefreshDLQTargetArns();
            RefreshKMSKeys();
        }

        void RefreshVpcSubnets()
        {
            var vpcs = new List<Vpc>();
            var subnets = new List<Subnet>();

            this._ec2Client.DescribeVpcsAsync().ContinueWith(task =>
            {
                if (task.Exception == null)
                {
                    vpcs.AddRange(task.Result.Vpcs);
                }
                else
                {
                    LOGGER.Info("DescribeVpcsAsync: error getting available vpcs", task.Exception);
                }
            }).ContinueWith(task2 =>
            {
                this._ec2Client.DescribeSubnetsAsync().ContinueWith(task3 =>
                {
                    if (task3.Exception == null)
                    {
                        subnets.AddRange(task3.Result.Subnets);
                    }
                    else
                    {
                        LOGGER.Info("DescribeSubnetsAsync: error getting available subnets", task3.Exception);
                    }
                }).ContinueWith(task4 =>
                {
                    ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action) (() =>
                    {
                        this._control.SetAvailableVpcSubnets(vpcs, subnets, this._model.VpcConfig?.SubnetIds);
                    }));
                });
            });
        }

        void RefreshSecurityGroupsForVpc(string vpcId)
        {
            var groups = new List<SecurityGroup>();
            if (string.IsNullOrEmpty(vpcId))
            {
                ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread(() =>
                {
                    this._control.SetAvailableSecurityGroups(null, null, null);
                });
            }
            else
            {
                this._ec2Client.DescribeSecurityGroupsAsync().ContinueWith(task =>
                {
                    if (task.Exception == null)
                    {
                        foreach (var group in task.Result.SecurityGroups)
                        {
                            if (!string.IsNullOrEmpty(group.VpcId) && group.VpcId.Equals(vpcId))
                            {
                                groups.Add(group);
                            }
                        }
                    }
                    else
                    {
                        LOGGER.Info(
                            "DescribeSecurityGroupsAsync: error getting available security groups for vpc " + vpcId,
                            task.Exception);
                    }
                }).ContinueWith(task2 =>
                {
                    ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action) (() =>
                    {
                        this._control.SetAvailableSecurityGroups(groups, null,
                            this._model.VpcConfig?.SecurityGroupIds);
                    }));
                });
            }
        }

        void RefreshDLQTargetArns()
        {
            if (_snsClient != null && this._sqsClient != null)
            {
                new QueryDLQTargetsWorker(
                    this._snsClient,
                    this._sqsClient,
                    LOGGER,
                    OnDLQTargetsAvailable);
            }
        }

        void OnDLQTargetsAvailable(QueryDLQTargetsWorker.QueryResults results)
        {
            ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action) (() =>
            {
                this._control.SetAvailableDLQTargets(results.TopicArns, results.QueueArns, this.Model.DLQTargetArn);
            }));
        }


        void RefreshKMSKeys()
        {
            new QueryKMSKeysWorker(_kmsClient,
                LOGGER,
                OnKMSKeysAvailable);
        }

        void OnKMSKeysAvailable(IEnumerable<KeyListEntry> keys, IEnumerable<AliasListEntry> aliases)
        {
            ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action) (() =>
            {
                this._control.SetAvailableKMSKeys(keys, aliases);
            }));
        }

        void RefreshEnvironmentVariables()
        {
        }

        public void RefreshEventSources()
        {
            var request = new ListEventSourceMappingsRequest {FunctionName = this._model.FunctionName};
            ListEventSourceMappingsResponse response = null;

            this._model.EventSources.Clear();
            do
            {
                if (response != null)
                    request.Marker = response.NextMarker;

                response = this._lambdaClient.ListEventSourceMappings(request);

                foreach (var eventSourceConfiguration in response.EventSourceMappings)
                {
                    if (!eventSourceConfiguration.State.Equals("enabled", StringComparison.OrdinalIgnoreCase) &&
                        !eventSourceConfiguration.State.Equals("creating", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var wrapper = new EventSourceWrapper(eventSourceConfiguration);
                    this._model.EventSources.Add(wrapper);
                }
            } while (!string.IsNullOrEmpty(response.NextMarker));


            var policyStr = GetPolicy();
            if (!string.IsNullOrEmpty(policyStr))
            {
                Policy policy = Policy.FromJson(policyStr);
                foreach (var statement in policy.Statements)
                {
                    // Unexpected statement that we don't know how to handle. The Lambda API should only allow one of each type.
                    if (statement.Principals.Count != 1 || statement.Resources.Count != 1 ||
                        statement.Actions.Count != 1 || statement.Effect != Statement.StatementEffect.Allow ||
                        statement.Conditions.Count != 1 || statement.Conditions[0].Values.Count() != 1)
                        continue;

                    var wrapper = new EventSourceWrapper(statement);
                    this._model.EventSources.Add(wrapper);
                }
            }
        }

        // Lambda throws exception if the policy does not exist.
        private string GetPolicy()
        {
            try
            {
                return this._lambdaClient.GetPolicy(new GetPolicyRequest {FunctionName = this._model.FunctionName})
                    .Policy;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string CloudWatchLogGroup => string.Format("/aws/lambda/{0}", this._model.FunctionName);

        public void RefreshLogs()
        {
            this._model.Logs.Clear();
            if (this._logsClient == null)
                return;

            var request = new DescribeLogStreamsRequest
            {
                LogGroupName = this.CloudWatchLogGroup,
                OrderBy = OrderBy.LastEventTime,
                Descending = true
            };

            try
            {
                this._logsClient.DescribeLogStreamsAsync(request).ContinueWith(task =>
                {
                    if (task.Exception == null)
                    {
                        var response = task.Result;
                        ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread(() =>
                        {
                            foreach (var stream in response.LogStreams)
                            {
                                this._model.Logs.Add(new LogStreamWrapper(stream));
                            }
                        });
                    }
                    else
                    {
                        // Most likely lambda doesn't have permission to write logs.
                        LOGGER.Info("DescribeLogStreamsAsync: error getting cloudwatch logs", task.Exception);
                    }
                });
            }
            catch (Exception e)
            {
                // Most likely lambda doesn't have permission to write logs.
                LOGGER.Info("Error getting cloudwatch logs for " + request.LogGroupName, e);
            }
        }

        public void DownloadLog(string logStream)
        {
            var fileName = Path.GetTempFileName() + ".txt";

            using (var writer = new StreamWriter(fileName))
            {
                var request = new GetLogEventsRequest
                {
                    LogGroupName = this.CloudWatchLogGroup,
                    LogStreamName = logStream
                };

                var response = this._logsClient.GetLogEvents(request);
                foreach (var evnt in response.Events)
                {
                    string message = NormalizeLineEnding(evnt.Message);
                    writer.Write("{0:yyyy-MM-dd HH:mm:ss}: {1}", evnt.Timestamp.ToLocalTime(), message);
                }
            }

            var process = new Process();
            process.StartInfo.FileName = fileName;
            process.StartInfo.Verb = "Open";
            process.Start();
        }

        private static string NormalizeLineEnding(string text)
        {
            if (text.EndsWith("\r\n"))
            {
                return text.Remove(text.Length - 2) + System.Environment.NewLine;
            }

            if (text.EndsWith("\n"))
            {
                return text.Remove(text.Length - 1) + System.Environment.NewLine;
            }

            return text;
        }

        public void DeleteLog(LogStreamWrapper logStream)
        {
            var request = new DeleteLogStreamRequest
            {
                LogGroupName = this.CloudWatchLogGroup,
                LogStreamName = logStream.LogStreamName
            };

            this._logsClient.DeleteLogStream(request);
            this._model.Logs.Remove(logStream);
        }

        public void UpdateConfiguration()
        {
            var request = new UpdateFunctionConfigurationRequest
            {
                Description = this._model.Description,
                FunctionName = this._model.FunctionName,
                MemorySize = this._model.MemorySize,
                Timeout = this._model.Timeout,
                TracingConfig = new TracingConfig
                    {Mode = this._model.IsEnabledActiveTracing ? TracingMode.Active : TracingMode.PassThrough},
                Environment = new Amazon.Lambda.Model.Environment
                {
                    Variables = new Dictionary<string, string>()
                }
            };

            if (_model.PackageType == PackageType.Zip)
            {
                request.Handler = this._model.Handler;
            }

            if (_model.PackageType == PackageType.Image)
            {
                var command = SplitByComma(_model.ImageCommand);
                var entrypoint = SplitByComma(_model.ImageEntrypoint);
                request.ImageConfig = new ImageConfig()
                {
                    Command = command,
                    IsCommandSet = command != null,
                    EntryPoint = entrypoint,
                    IsEntryPointSet = entrypoint != null,
                    WorkingDirectory = _model.ImageWorkingDirectory,
                };
            }

            request.DeadLetterConfig = new DeadLetterConfig
                {TargetArn = this._control.SelectedDLQTargetArn ?? string.Empty};

            if (this._model.EnvironmentVariables.Any())
            {
                foreach (var envvar in this._model.EnvironmentVariables)
                {
                    if (!string.IsNullOrWhiteSpace(envvar.Variable))
                        request.Environment.Variables.Add(envvar.Variable, envvar.Value);
                }
            }

            var kmsKey = this._control.SelectedKMSKey;
            if (kmsKey == null || kmsKey == KeyAndAliasWrapper.LambdaDefaultKMSKey.Key)
                request.KMSKeyArn = null;
            else
                request.KMSKeyArn = kmsKey.KeyArn;

            var subnets = this._control.SelectedSubnets;
            var groups = this._control.SelectedSecurityGroups;
            if (subnets != null && subnets.Any() && groups != null && groups.Any())
            {
                var subnetIds = new List<string>();
                foreach (var subnet in subnets)
                {
                    subnetIds.Add(subnet.SubnetId);
                }

                var groupIds = new List<string>();
                foreach (var group in groups)
                {
                    groupIds.Add(group.GroupId);
                }

                request.VpcConfig = new VpcConfig
                {
                    SubnetIds = subnetIds,
                    SecurityGroupIds = groupIds
                };
            }

            UpdateConfiguration(this._lambdaClient, request);

            if (this._model.IsEnabledActiveTracing)
            {
                ToolkitEvent evnt = new ToolkitEvent();
                evnt.AddProperty(AttributeKeys.XRayEnabled, "Lambda");
                SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);
            }
        }

        public void UpdateConfiguration(IAmazonLambda lambdaClient, UpdateFunctionConfigurationRequest request)
        {

            var response = lambdaClient.UpdateFunctionConfiguration(request);

            if (response.PackageType == PackageType.Image)
            {
                // Update the Image fields that may have been reformatted/trimmed
                this._model.ImageCommand = JoinByComma(response.ImageConfigResponse?.ImageConfig?.Command);
                this._model.ImageEntrypoint = JoinByComma(response.ImageConfigResponse?.ImageConfig?.EntryPoint);
            }

            this._model.LastModified = DateTime.Parse(response.LastModified);
            this._model.State = response.State;
            this._model.StateReasonCode = response.StateReasonCode;
            this._model.StateReason = response.StateReason;
            this._model.LastUpdateStatus = response.LastUpdateStatus;
            this._model.LastUpdateStatusReasonCode = response.LastUpdateStatusReasonCode;
            this._model.LastUpdateStatusReason = response.LastUpdateStatusReason;
            this._model.IsDirty = false;
        }


        public bool UploadNewFunctionSource()
        {
            var controller = new UploadFunctionController(_toolkitContext);
            var results = controller.Execute(this._lambdaClient, this._ecrClient, this._model.FunctionName);

            return results.Success;
        }

        public bool AddEventSource()
        {
            var controller = new AddEventSourceController(_toolkitContext);
            return controller.Execute(this._lambdaClient, this._account, this._region, this._model.FunctionArn,
                this._model.Role);
        }

        public void DeleteEventSource(EventSourceWrapper wrapper)
        {
            if (wrapper.Type == EventSourceWrapper.EventSourceType.Pull)
            {
                var request = new DeleteEventSourceMappingRequest
                {
                    UUID = wrapper.UUID
                };
                this._lambdaClient.DeleteEventSourceMapping(request);
            }
            else
            {
                var request = new RemovePermissionRequest
                {
                    FunctionName = this._model.FunctionName,
                    StatementId = wrapper.UUID
                };
                this._lambdaClient.RemovePermission(request);
            }
        }

        public async Task<InvokeResponse> InvokeFunctionAsync(string input)
        {
            var request = new InvokeRequest
            {
                FunctionName = this._model.FunctionName,
                Payload = input,
                InvocationType = InvocationType.RequestResponse,
                LogType = LogType.Tail
            };

            if (!string.IsNullOrEmpty(input))
            {
                request.Payload = request.Payload.Trim();
                if (request.Payload[0] != '\"' && request.Payload[0] != '{' && request.Payload[0] != '[')
                {
                    double d;
                    long l;
                    bool b;
                    if (!double.TryParse(request.Payload, out d) && !long.TryParse(request.Payload, out l) &&
                        !bool.TryParse(request.Payload, out b))
                    {
                        request.Payload = "\"" + request.Payload + "\"";
                    }
                }
            }

            var response = await this._lambdaClient.InvokeAsync(request);
            return response;
        }

        public static string JoinByComma(List<string> strings)
        {
            if (strings == null)
            {
                return string.Empty;
            }

            return string.Join(",", strings);
        }

        public static List<string> SplitByComma(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            return text.Split(new char[] {','}, StringSplitOptions.None)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();
        }
    }
}

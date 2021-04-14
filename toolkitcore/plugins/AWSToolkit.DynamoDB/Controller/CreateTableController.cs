using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.DynamoDB.Nodes;
using Amazon.AWSToolkit.DynamoDB.Model;
using Amazon.AWSToolkit.DynamoDB.View.CreateTableWizard.PageControllers;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace Amazon.AWSToolkit.DynamoDB.Controller
{
    public class CreateTableController : BaseContextCommand
    {
        private static readonly string CloudWatchServiceName =
            new AmazonCloudWatchConfig().RegionEndpointServiceName;
        private static readonly string SnsServiceName =
            new AmazonSimpleNotificationServiceConfig().RegionEndpointServiceName;

        public const string WIZARD_SEED_DATACONTEXT = "DataContext";
        public const string LAST_CONTROLLER = "LastController";

        const string TOPIC_NAME = "dynamodb";

        private readonly ToolkitContext _toolkitContext;

        CreateTableModel _model;
        DynamoDBRootViewModel _rootModel;
        ActionResults _results;

        IAmazonSimpleNotificationService _snsClient;
        IAmazonCloudWatch _cwClient;

        public CreateTableController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public override ActionResults Execute(IViewModel model)
        {
            this._rootModel = model as DynamoDBRootViewModel;
            if (this._rootModel == null)
                return new ActionResults().WithSuccess(false);

            var region = this._rootModel.Region;
            
            if (_toolkitContext.RegionProvider.IsServiceAvailable(SnsServiceName, region.Id))
            {
                _snsClient =
                    _rootModel.AccountViewModel.CreateServiceClient<AmazonSimpleNotificationServiceClient>(region);
            }

            if (_toolkitContext.RegionProvider.IsServiceAvailable(CloudWatchServiceName, region.Id))
            {
                this._cwClient = _rootModel.AccountViewModel.CreateServiceClient<AmazonCloudWatchClient>(region);
            }

            var seedProperties = new Dictionary<string, object>();

            this._model = new CreateTableModel();

            seedProperties[WIZARD_SEED_DATACONTEXT] = this._model;

            IAWSWizardPageController[] defaultPages;
            if (this._snsClient == null || this._cwClient == null)
            {
                defaultPages = new IAWSWizardPageController[]
                {
                    new BasicSettingsPageController(),
                    new IndexesPageController()
                };
            }
            else
            {
                defaultPages = new IAWSWizardPageController[]
                {
                    new BasicSettingsPageController(),
                    new IndexesPageController(),
                    new NotificationPageController()
                };
            }

            seedProperties[LAST_CONTROLLER] = defaultPages[defaultPages.Length - 1];

            IAWSWizard wizard = AWSWizardFactory.CreateStandardWizard("Amazon.AWSToolkit.DynamoDB.View.CreateDDBTable", seedProperties);

            wizard.Title = "Create new DynamoDB Table";
            wizard.RegisterPageControllers(defaultPages, 0);
            wizard.CommitAction = this.Persist;

            wizard.SetNavigationButtonText(AWSWizardConstants.NavigationButtons.Finish, "Create");
            wizard.Run();

            if (this._results == null)
                return new ActionResults().WithSuccess(false);
            return this._results;
        }

        public CreateTableModel Model => this._model;

        public bool Persist()
        {
            try
            {
                var request = new CreateTableRequest
                {
                    TableName = this._model.TableName
                };

                // Hash Key
                request.KeySchema = new List<KeySchemaElement>()
                {
                    new KeySchemaElement
                    {
                        AttributeName = this._model.HashKeyName,
                        KeyType = "HASH"
                    }
                };
                request.AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition 
                    {
                        AttributeName = this.Model.HashKeyName, 
                        AttributeType = DynamoDBConstants.ToConstant(this.Model.HashKeyType, false) 
                    }
                };

                // Range Key
                if (this.Model.UseRangeKey)
                {
                    request.KeySchema.Add(new KeySchemaElement
                    {
                        AttributeName = this.Model.RangeKeyName,
                        KeyType = "RANGE"
                    });

                    request.AttributeDefinitions.Add(new AttributeDefinition
                    {
                        AttributeName = this.Model.RangeKeyName,
                        AttributeType = DynamoDBConstants.ToConstant(this.Model.RangeKeyType, false)
                    });
                }

                var isDefinedAttribute = new Func<string, string, bool>((name, type) =>
                {
                    var definition = request.AttributeDefinitions.FirstOrDefault(
                            a => a.AttributeName.Equals(name, StringComparison.InvariantCulture) && a.AttributeType == type);

                    return definition != null;
                });


                // LSI
                if (this.Model.UseLocalSecondaryIndexes)
                {
                    request.LocalSecondaryIndexes = new List<LocalSecondaryIndex>();
                    foreach (var index in this.Model.LocalSecondaryIndexes)
                    {
                        request.LocalSecondaryIndexes.Add(new LocalSecondaryIndex
                        {
                            IndexName = index.Name,
                            KeySchema = new List<KeySchemaElement>
                            {
                                new KeySchemaElement { AttributeName = this.Model.HashKeyName, KeyType = KeyType.HASH },
                                new KeySchemaElement { AttributeName = index.RangeKey.Name, KeyType = KeyType.RANGE }
                            },
                            Projection = new Projection
                            {
                                ProjectionType = index.ProjectAttributeDefinition.ProjectionType,
                                NonKeyAttributes = index.ProjectAttributeDefinition.IsCustomProjectionAllowed ? index.ProjectAttributeDefinition.ProjectionColumnList.Select(c => c.Value).ToList() : null
                            }
                        });


                        if (!isDefinedAttribute(index.RangeKey.Name, index.RangeKey.Type))
                        {
                            request.AttributeDefinitions.Add(new AttributeDefinition
                            {
                                AttributeName = index.RangeKey.Name,
                                AttributeType = index.RangeKey.Type
                            });
                        }
                    }
                }

                if (this._model.UseGlobalSecondaryIndexes)
                {
                    request.GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>();
                    foreach (var index in this.Model.GlobalSecondaryIndexes)
                    {
                        var keySchema = new List<KeySchemaElement>();
                        keySchema.Add(new KeySchemaElement { AttributeName = index.HashKey.Name, KeyType = KeyType.HASH});
                        if (index.HasRangeKey)
                            keySchema.Add(new KeySchemaElement { AttributeName = index.RangeKey.Name, KeyType = KeyType.RANGE});

                        request.GlobalSecondaryIndexes.Add(new GlobalSecondaryIndex
                        {
                            IndexName = index.Name,
                            KeySchema = keySchema,
                            Projection = new Projection
                            {
                                ProjectionType = index.ProjectAttributeDefinition.ProjectionType,
                                NonKeyAttributes = index.ProjectAttributeDefinition.IsCustomProjectionAllowed ? index.ProjectAttributeDefinition.ProjectionColumnList.Select(c => c.Value).ToList() : null
                            },
                            ProvisionedThroughput = new ProvisionedThroughput
                            {
                                ReadCapacityUnits = index.ReadCapacity,
                                WriteCapacityUnits = index.WriteCapacity
                            }
                        });

                        if (!isDefinedAttribute(index.HashKey.Name, index.HashKey.Type))
                        {
                            request.AttributeDefinitions.Add(new AttributeDefinition
                            {
                                AttributeName = index.HashKey.Name,
                                AttributeType = index.HashKey.Type
                            });
                        }
                        if (index.HasRangeKey && !isDefinedAttribute(index.RangeKey.Name, index.RangeKey.Type))
                        {
                            request.AttributeDefinitions.Add(new AttributeDefinition
                            {
                                AttributeName = index.RangeKey.Name,
                                AttributeType = index.RangeKey.Type
                            });
                        }
                    }
                }

                request.ProvisionedThroughput = new ProvisionedThroughput()
                {
                    ReadCapacityUnits = int.Parse(this._model.ReadCapacityUnits),
                    WriteCapacityUnits = int.Parse(this._model.WriteCapacityUnits)
                };

                this._rootModel.DynamoDBClient.CreateTable(request);

                if (this._model.UseBasicAlarms && this._snsClient != null && this._cwClient != null)
                {
                    var topicArn = FetchSNSTopicARN();

                    CreateAlarm(this._model.TableName, "ConsumedReadCapacityUnits", request.ProvisionedThroughput.ReadCapacityUnits, this._model.SelectedPercentage.Value, topicArn);
                    CreateAlarm(this._model.TableName, "ConsumedWriteCapacityUnits", request.ProvisionedThroughput.WriteCapacityUnits, this._model.SelectedPercentage.Value, topicArn);

                    SubscribeToTopic(topicArn, this._model.AlarmEmail);
                }

                this._results = new ActionResults()
                    .WithSuccess(true)
                    .WithFocalname(this._model.TableName)
                    .WithShouldRefresh(true);

                return true;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating table: " + e.Message);
                this._results = new ActionResults().WithSuccess(false);
                return false;
            }
        }
       
        private string FetchSNSTopicARN()
        {
            ListTopicsResponse response = null;
            do
            {
                var request = new ListTopicsRequest();
                if (response != null)
                    request.NextToken = response.NextToken;
                response = this._snsClient.ListTopics(request);

                var topic = response.Topics.FirstOrDefault(x => x.TopicArn.EndsWith(":" + TOPIC_NAME));
                if (topic != null)
                    return topic.TopicArn;

            } while (!string.IsNullOrEmpty(response.NextToken));

            var createTopicRequest = new CreateTopicRequest()
            {
                Name = TOPIC_NAME
            };
            var createResponse = this._snsClient.CreateTopic(createTopicRequest);
            return createResponse.TopicArn;
        }

        private void SubscribeToTopic(string topicArn, string email)
        {
            foreach (var request in email.Split(',').Select(token => new SubscribeRequest()
                                                                         {
                                                                             TopicArn = topicArn,
                                                                             Endpoint = token.Trim(),
                                                                             Protocol = "email"
                                                                         }))
            {
                this._snsClient.Subscribe(request);
            }
        }

        private void CreateAlarm(string tableName, string metricName, long units, double percentage, string topicArn)
        {
            var putAlarm = new PutMetricAlarmRequest()
            {
                AlarmName = string.Format("{0}-{1}-{2}", tableName, metricName, DateTime.Now.Ticks),
                AlarmActions = new List<string>(){ topicArn},
                ComparisonOperator = "GreaterThanOrEqualToThreshold",
                Namespace = "AWS/DynamoDB",
                MetricName = "ConsumedWriteCapacityUnits",
                Dimensions = new List<Dimension>()
                {
                    new Dimension(){Name = "TableName", Value = tableName}
                },
                Statistic = "Average",
                Threshold = units * percentage,
                EvaluationPeriods = 12,
                Period = 300
            };


            this._cwClient.PutMetricAlarm(putAlarm);
        }
    }
}

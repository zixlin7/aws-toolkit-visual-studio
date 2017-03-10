using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.DynamoDB.Nodes;
using Amazon.AWSToolkit.DynamoDB.View;
using Amazon.AWSToolkit.DynamoDB.Model;
using Amazon.AWSToolkit;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Amazon.AWSToolkit.DynamoDB.Controller
{
    public class TablePropertiesController : BaseContextCommand
    {
        TablePropertiesControl _control;
        TablePropertiesModel _model;
        DynamoDBTableViewModel _rootModel;
        ActionResults _results;
        Dictionary<string, ProvisionedThroughputDescription> _originalGlobalProvisions = new Dictionary<string, ProvisionedThroughputDescription>();

        public override ActionResults Execute(IViewModel model)
        {
            this._rootModel = model as DynamoDBTableViewModel;
            if (this._rootModel == null)
                return new ActionResults().WithSuccess(false);

            this._model = new TablePropertiesModel(this._rootModel.Table);
            this._control = new TablePropertiesControl(this);
            this._control.PreloadModel();
            ToolkitFactory.Instance.ShellProvider.ShowModal(this._control);

            if (this._results == null)
                return new ActionResults().WithSuccess(false);
            return this._results;
        }

        public TablePropertiesModel Model
        {
            get { return this._model; }
        }

        public void LoadModel()
        {
            DescribeTableResponse response = null;
            try
            {
                response = this._rootModel.DynamoDBClient.DescribeTable(new DescribeTableRequest { TableName = this.Model.TableName });
            }
            catch (Exception)
            {
                throw new ApplicationException("Failed to find table " + this.Model.TableName + ".");
            }

            var table = response.Table;
            this.Model.Status = table.TableStatus;
            
            var hash=table.KeySchema.SingleOrDefault(k=>k.KeyType == KeyType.HASH);
            if(hash!=null)
            {
                this.Model.HashKeyName=hash.AttributeName;
                var hashAttribute= table.AttributeDefinitions.Single(k=>k.AttributeName.Equals(hash.AttributeName,StringComparison.InvariantCulture)).AttributeType;
                this.Model.HashKeyType= DynamoDBConstants.FromConstant(hashAttribute);
            }

            var range = table.KeySchema.SingleOrDefault(k => k.KeyType == KeyType.RANGE);
            if (range!=null)
            {
                this.Model.UseRangeKey = true;
                this.Model.RangeKeyName = range.AttributeName;
                var rangeAttribute = table.AttributeDefinitions.Single(k => k.AttributeName.Equals(range.AttributeName, StringComparison.InvariantCulture)).AttributeType;
                this.Model.RangeKeyType = DynamoDBConstants.FromConstant(rangeAttribute);
            }
            else
            {
                this.Model.UseRangeKey = false;
            }

            if (table.LocalSecondaryIndexes!=null && table.LocalSecondaryIndexes.Count>0)
            {
                this.Model.UseLocalSecondaryIndexes = true;
                foreach (var index in table.LocalSecondaryIndexes)
                {
                    SecondaryIndex secondaryIndex = new SecondaryIndex()
                    {
                        Name = index.IndexName,
                        ProjectAttributeDefinition = new ProjectAttributeDefinition { ProjectionType = index.Projection.ProjectionType },
                        IsExisting = true
                    };

                    var secondaryIndexKeySchema = index.KeySchema.Single(k => k.KeyType == KeyType.RANGE);
                    secondaryIndex.RangeKey.Name = secondaryIndexKeySchema.AttributeName;
                    var rangeType= table.AttributeDefinitions.Single(
                        k => k.AttributeName.Equals(secondaryIndexKeySchema.AttributeName, StringComparison.InvariantCulture)).AttributeType;
                    secondaryIndex.RangeKey.Type = rangeType;

                    var nonKeyAttributes = index.Projection.NonKeyAttributes;
                    if (nonKeyAttributes != null)
                    {
                        foreach (var attr in nonKeyAttributes)
                        {
                            secondaryIndex.ProjectAttributeDefinition.ProjectionColumnList.Add(new StringWrapper { Value = attr });
                        }                                 
                    }     
                    this.Model.LocalSecondaryIndexes.Add(secondaryIndex);                    
                }
            }
            else
            {
                this.Model.UseLocalSecondaryIndexes = false;
            }

            if (table.GlobalSecondaryIndexes != null && table.GlobalSecondaryIndexes.Count > 0)
            {
                this.Model.UseGlobalSecondaryIndexes = true;
                foreach (var index in table.GlobalSecondaryIndexes)
                {
                    this._originalGlobalProvisions[index.IndexName] = index.ProvisionedThroughput;

                    SecondaryIndex secondaryIndex = new SecondaryIndex()
                    {
                        Name = index.IndexName,
                        ReadCapacity = index.ProvisionedThroughput.ReadCapacityUnits,
                        WriteCapacity = index.ProvisionedThroughput.WriteCapacityUnits,
                        IndexStatus = index.IndexStatus.Value,
                        IsExisting = true
                    };

                    if (index.IndexStatus != IndexStatus.DELETING)
                    {
                        secondaryIndex.ProjectAttributeDefinition = new ProjectAttributeDefinition { ProjectionType = index.Projection.ProjectionType };

                        var hashKeySchema = index.KeySchema.Single(k => k.KeyType == KeyType.HASH);
                        secondaryIndex.HashKey.Name = hashKeySchema.AttributeName;
                        var hashType = table.AttributeDefinitions.Single(
                            k => k.AttributeName.Equals(hashKeySchema.AttributeName, StringComparison.InvariantCulture)).AttributeType;
                        secondaryIndex.HashKey.Type = hashType;

                        var rangeKeySchema = index.KeySchema.FirstOrDefault(k => k.KeyType == KeyType.RANGE);
                        if (rangeKeySchema != null)
                        {
                            secondaryIndex.RangeKey.Name = rangeKeySchema.AttributeName;
                            var rangeType = table.AttributeDefinitions.Single(
                                k => k.AttributeName.Equals(rangeKeySchema.AttributeName, StringComparison.InvariantCulture)).AttributeType;
                            secondaryIndex.RangeKey.Type = rangeType;
                        }

                        var nonKeyAttributes = index.Projection.NonKeyAttributes;
                        if (nonKeyAttributes != null)
                        {
                            foreach (var attr in nonKeyAttributes)
                            {
                                secondaryIndex.ProjectAttributeDefinition.ProjectionColumnList.Add(new StringWrapper { Value = attr });
                            }
                        }
                    }

                    this.Model.ExistingGlobalSecondaryIndexes[secondaryIndex.Name] = secondaryIndex.Clone() as SecondaryIndex;
                    this.Model.GlobalSecondaryIndexes.Add(secondaryIndex);
                }
            }
            else
            {
                this.Model.UseLocalSecondaryIndexes = false;
            }

            if (table.ProvisionedThroughput != null)
            {
                this.Model.ReadCapacityUnits = table.ProvisionedThroughput.ReadCapacityUnits.ToString();
                this.Model.OriginalReadCapacityUnits = table.ProvisionedThroughput.ReadCapacityUnits.ToString();

                this.Model.WriteCapacityUnits = table.ProvisionedThroughput.WriteCapacityUnits.ToString();
                this.Model.OriginalWriteCapacityUnits = table.ProvisionedThroughput.WriteCapacityUnits.ToString();
            }

            DescribeTimeToLiveResponse ttlResponse = null;
            try
            {
                ttlResponse = this._rootModel.DynamoDBClient.DescribeTimeToLive(new DescribeTimeToLiveRequest { TableName = this.Model.TableName });
            }
            catch
            {
                this.Model.TTLInfoAvailable = false;
            }

            this.Model.TTLAttributeName = "Expiration";
            if (ttlResponse != null)
            {
                this.Model.TTLInfoAvailable = true;
                this.Model.TTLEnabled =
                    ttlResponse.TimeToLiveDescription.TimeToLiveStatus == TimeToLiveStatus.ENABLED ||
                    ttlResponse.TimeToLiveDescription.TimeToLiveStatus == TimeToLiveStatus.ENABLING;
                if (!string.IsNullOrEmpty(ttlResponse.TimeToLiveDescription.AttributeName))
                {
                    this.Model.TTLAttributeName = ttlResponse.TimeToLiveDescription.AttributeName;
                }
            }
            this.Model.OriginalTTLAttributeName = this.Model.TTLAttributeName;
            this.Model.OriginalTTLEnabled = this.Model.TTLEnabled;
        }

        public bool Persist()
        {
            try
            {
                this.PerformThroughputUpdates();
                this.PerformGlobalIndexCreate();
                this.PerformGlobalSecondaryDeletes();
                this.PerformTTLUpdate();

                this._results = new ActionResults()
                    .WithSuccess(true)
                    .WithFocalname(this._model.TableName)
                    .WithShouldRefresh(true);

                return true;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error updating table: " + e.Message);
                this._results = new ActionResults().WithSuccess(false);
                return false;
            }
        }

        private void PerformGlobalIndexCreate()
        {
            foreach (var index in this._model.GlobalSecondaryIndexes)
            {
                if (!this.Model.ExistingGlobalSecondaryIndexes.ContainsKey(index.Name))
                {
                    var request = new UpdateTableRequest
                    {
                        TableName = this._model.TableName
                    };

                    var action = new CreateGlobalSecondaryIndexAction
                    {
                        IndexName = index.Name,
                        ProvisionedThroughput = new ProvisionedThroughput
                        {
                            ReadCapacityUnits = index.ReadCapacity,
                            WriteCapacityUnits = index.WriteCapacity
                        },
                        Projection = new Projection
                        {
                            ProjectionType = index.ProjectAttributeDefinition.ProjectionType,
                            NonKeyAttributes = index.ProjectAttributeDefinition.IsCustomProjectionAllowed ? index.ProjectAttributeDefinition.ProjectionColumnList.Select(c => c.Value).ToList() : null
                        }
                    };

                    action.KeySchema.Add(new KeySchemaElement
                    {
                        AttributeName = index.HashKey.Name,
                        KeyType = KeyType.HASH
                    });

                    request.AttributeDefinitions.Add(new AttributeDefinition
                    {
                        AttributeName = index.HashKey.Name,
                        AttributeType = index.HashKey.Type
                    });


                    if (index.HasRangeKey)
                    {
                        action.KeySchema.Add(new KeySchemaElement
                        {
                            AttributeName = index.RangeKey.Name,
                            KeyType = KeyType.RANGE
                        });
                        request.AttributeDefinitions.Add(new AttributeDefinition
                        {
                            AttributeName = index.RangeKey.Name,
                            AttributeType = index.RangeKey.Type
                        });
                    }

                    action.Projection = new Projection
                    {
                        ProjectionType = index.ProjectAttributeDefinition.ProjectionType
                    };

                    request.GlobalSecondaryIndexUpdates.Add(new GlobalSecondaryIndexUpdate { Create = action });

                    this._rootModel.DynamoDBClient.UpdateTable(request);
                }
            }
        }

        private void PerformGlobalSecondaryDeletes()
        {
            foreach (var indexName in this.Model.ExistingGlobalSecondaryIndexes.Keys)
            {
                if (!this.Model.GlobalSecondaryIndexes.Any(x => x.Name == indexName))
                {
                    var request = new UpdateTableRequest
                    {
                        TableName = this._model.TableName
                    };

                    var delete = new DeleteGlobalSecondaryIndexAction
                    {
                        IndexName = indexName
                    };

                    request.GlobalSecondaryIndexUpdates.Add(new GlobalSecondaryIndexUpdate { Delete = delete });
                    this._rootModel.DynamoDBClient.UpdateTable(request);
                }
            }

        }

        private void PerformThroughputUpdates()
        {
            var request = new UpdateTableRequest
            {
                TableName = this._model.TableName
            };

            bool change = false;

            foreach (var index in this._model.GlobalSecondaryIndexes)
            {
                if (this.Model.ExistingGlobalSecondaryIndexes.ContainsKey(index.Name))
                {
                    var originalIndex = this.Model.ExistingGlobalSecondaryIndexes[index.Name];

                    if (originalIndex.ReadCapacity != index.ReadCapacity || originalIndex.WriteCapacity != index.WriteCapacity)
                    {
                        change = true;
                        var update = new UpdateGlobalSecondaryIndexAction
                        {
                            IndexName = index.Name,
                            ProvisionedThroughput = new ProvisionedThroughput
                            {
                                ReadCapacityUnits = index.ReadCapacity,
                                WriteCapacityUnits = index.WriteCapacity
                            }
                        };

                        request.GlobalSecondaryIndexUpdates.Add(new GlobalSecondaryIndexUpdate { Update = update });
                    }
                }
            }

            // if one value has changed, both must be sent otherwise service returns error
            if (!string.Equals(this._model.OriginalReadCapacityUnits, this._model.ReadCapacityUnits)
                || !string.Equals(this._model.OriginalWriteCapacityUnits, this._model.WriteCapacityUnits))
            {
                change = true;
                request.ProvisionedThroughput = new ProvisionedThroughput();
                request.ProvisionedThroughput.ReadCapacityUnits = int.Parse(this._model.ReadCapacityUnits);
                request.ProvisionedThroughput.WriteCapacityUnits = int.Parse(this._model.WriteCapacityUnits);
            }

            if(change)
                this._rootModel.DynamoDBClient.UpdateTable(request);
        }

        private void PerformTTLUpdate()
        {
            if (HasTtlChanged())
            {
                var request = new UpdateTimeToLiveRequest
                {
                    TableName = this._model.TableName,
                    TimeToLiveSpecification = new TimeToLiveSpecification
                    {
                        AttributeName = this._model.TTLAttributeName,
                        Enabled = this._model.TTLEnabled
                    }
                };
                this._rootModel.DynamoDBClient.UpdateTimeToLive(request);
            }
        }

        private bool HasChanged()
        {
            if (!string.Equals(this._model.OriginalReadCapacityUnits, this._model.ReadCapacityUnits) ||
                !string.Equals(this._model.OriginalWriteCapacityUnits, this._model.WriteCapacityUnits))
                return true;

            foreach (var index in this._model.GlobalSecondaryIndexes)
            {
                var original = this._originalGlobalProvisions[index.Name];

                if (index.ReadCapacity != original.ReadCapacityUnits)
                    return true;
                if (index.WriteCapacity != original.WriteCapacityUnits)
                    return true;
            }

            if (HasTtlChanged())
            {
                return true;
            }

            return false;
        }

        private bool HasTtlChanged()
        {
            return this._model.OriginalTTLEnabled != this._model.TTLEnabled ||
                            !string.Equals(this._model.OriginalTTLAttributeName, this._model.TTLAttributeName);
        }
    }
}

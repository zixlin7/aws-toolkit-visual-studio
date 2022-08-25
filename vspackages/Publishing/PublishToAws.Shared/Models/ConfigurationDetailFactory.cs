﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Publish.Commands;
using Amazon.AWSToolkit.Publish.Models.Configuration;
using Amazon.AWSToolkit.Publish.Util;

using AWS.Deploy.ServerMode.Client;

namespace Amazon.AWSToolkit.Publish.Models
{
    public class ConfigurationDetailFactory
    {
        public static class ItemSummaryIds
        {
            public const string AdditionalEcsServiceSecurityGroups = "AdditionalECSServiceSecurityGroups";
            public const string VpcId = "VpcId";
            public const string VpcConnector = "VPCConnector";
        }

        public static class ItemSummaryTypes
        {
            public const string String = "String";
            public const string Int = "Int";
            public const string Double = "Double";
            public const string Bool = "Bool";
            public const string KeyValue = "KeyValue";
            public const string Object = "Object";
            public const string List = "List";
            public const string FilePath = "FilePath";
        }

        private readonly IPublishToAwsProperties _publishToAwsProperties;
        private readonly IDialogFactory _dialogFactory;

        public ConfigurationDetailFactory(IPublishToAwsProperties publishToAwsProperties, IDialogFactory dialogFactory)
        {
            _publishToAwsProperties = publishToAwsProperties;
            _dialogFactory = dialogFactory;
        }

        public ConfigurationDetail CreateFrom(OptionSettingItemSummary itemSummary) => CreateFrom(itemSummary, null);

        public ConfigurationDetail CreateFrom(OptionSettingItemSummary itemSummary, OptionSettingItemSummary parent)
        {
            var configurationDetail = InstantiateFor(itemSummary, parent);

            configurationDetail.Id = itemSummary.Id;
            configurationDetail.Name = itemSummary.Name;
            configurationDetail.OriginalType = itemSummary.Type;
            configurationDetail.Description = itemSummary.Description;
            configurationDetail.Type = GetConfigurationDetailType(itemSummary.Type);
            configurationDetail.TypeHint = itemSummary.TypeHint;
            configurationDetail.DefaultValue = itemSummary.Value;
            configurationDetail.Category = string.IsNullOrWhiteSpace(itemSummary.Category)
                ? Category.FallbackCategoryId
                : itemSummary.Category;
            configurationDetail.Advanced = itemSummary.Advanced;
            configurationDetail.ReadOnly = itemSummary.ReadOnly;
            configurationDetail.Visible = itemSummary.Visible;
            configurationDetail.SummaryDisplayable = itemSummary.SummaryDisplayable;

            configurationDetail.ValueMappings = GetValueMappings(itemSummary);
            configurationDetail.Value = GetValue(itemSummary, configurationDetail);

            configurationDetail.ValidationMessage = GetValidationMessage(itemSummary, configurationDetail);

            // Recurse all child data
            if (itemSummary.Type.Equals("Object"))
            {
                itemSummary.ChildOptionSettings?
                    .Select(childOption =>
                    {
                        var child = CreateFrom(childOption, itemSummary);
                        child.Parent = configurationDetail;

                        return child;
                    })
                    .ToList()
                    .ForEach(configurationDetail.AddChild);
            }

            return configurationDetail;
        }

        protected static T GetTypeHintData<T>(OptionSettingItemSummary itemSummary, string key, T defaultValue = default)
        {
            return itemSummary.TypeHintData.TryGetValue(key, out object value) && value is T castValue ? castValue : defaultValue;
        }

        private ConfigurationDetail InstantiateFor(OptionSettingItemSummary itemSummary, OptionSettingItemSummary parent)
        {
            if (itemSummary.TypeHint == ConfigurationDetail.TypeHints.IamRole)
            {
                var detail = new IamRoleConfigurationDetail();
                detail.SelectRoleArn = SelectRoleArnCommandFactory.Create(detail, _publishToAwsProperties, _dialogFactory);
                detail.ServicePrincipal = GetTypeHintData<string>(itemSummary, IamRoleConfigurationDetail.TypeHintDataKeys.ServicePrincipal);

                return detail;
            }

            if (itemSummary.TypeHint == ConfigurationDetail.TypeHints.Vpc)
            {
                var detail = new VpcConfigurationDetail();
                detail.SelectVpc = SelectVpcCommandFactory.Create(detail, _publishToAwsProperties, _dialogFactory);

                return detail;
            }

            if (itemSummary.TypeHint == ConfigurationDetail.TypeHints.InstanceType)
            {
                var detail = new Ec2InstanceConfigurationDetail();
                detail.SelectInstanceType =
                    SelectEc2InstanceTypeCommandFactory.Create(detail, _publishToAwsProperties, _dialogFactory);

                return detail;
            }

            if (itemSummary.TypeHint == ConfigurationDetail.TypeHints.EcrRepository)
            {
                var detail = new EcrRepositoryConfigurationDetail();
                detail.SelectRepo = SelectEcrRepoCommandFactory.Create(detail, _publishToAwsProperties, _dialogFactory);
                
                return detail;
            }

            if (itemSummary.TypeHint == ConfigurationDetail.TypeHints.FilePath)
            {
                var detail = new FilePathConfigurationDetail()
                {
                    CheckFileExists = GetTypeHintData<bool>(itemSummary, FilePathConfigurationDetail.TypeHintDataKeys.CheckFileExists),
                    Filter = GetTypeHintData<string>(itemSummary, FilePathConfigurationDetail.TypeHintDataKeys.Filter),
                    Title = GetTypeHintData<string>(itemSummary, FilePathConfigurationDetail.TypeHintDataKeys.Title)
                };
                detail.BrowseCommand = OpenFileSelectionCommandFactory.Create(detail, _dialogFactory);

                return detail;
            }

            if (itemSummary.Type == ItemSummaryTypes.KeyValue)
            {
                var detail = new KeyValueConfigurationDetail();
                detail.EditCommand = EditKeyValuesCommandFactory.Create(
                    detail,
                    $"Edit {itemSummary.Name}",
                    _dialogFactory);

                return detail;
            }

            if (itemSummary.Type == ItemSummaryTypes.List)
            {
                var detail = new ListConfigurationDetail();
                detail.EditCommand = EditListCommandFactory.Create(
                    detail,
                    $"Edit {itemSummary.Name}",
                    _dialogFactory);

                return detail;
            }

            // TODO This is a temporary fix until deploy tool validates that "existing VPC" is not empty when using an existing VPC Connector 
            if (itemSummary.Id == ItemSummaryIds.VpcId && parent?.Id == ItemSummaryIds.VpcConnector)
            {
                return new VpcConnectorVpcConfigurationDetail();
            }

            return new ConfigurationDetail();
        }

        private static DetailType GetConfigurationDetailType(string itemSummaryType)
        {
            switch (itemSummaryType)
            {
                case ItemSummaryTypes.String:
                    return DetailType.String;
                case ItemSummaryTypes.Int:
                    return DetailType.Integer;
                case ItemSummaryTypes.Double:
                    return DetailType.Double;
                case ItemSummaryTypes.Bool:
                    return DetailType.Boolean;
                case ItemSummaryTypes.KeyValue:
                    return DetailType.KeyValue;
                case ItemSummaryTypes.Object:
                    return DetailType.Blob;
                case ItemSummaryTypes.List:
                    return DetailType.List;
                default:
                    if (Debugger.IsAttached)
                    {
                        Debug.Assert(false,
                            "Unsupported CLI item summary type",
                            $"The Type '{itemSummaryType}' is not supported. The toolkit will not be able to edit this, and will show a UI indicating the field is not supported.");
                    }

                    return DetailType.Unsupported;
            }
        }

        private static object GetValue(OptionSettingItemSummary optionSettingItem, ConfigurationDetail detail)
        {
            object value = optionSettingItem.Validation?.ValidationStatus == ValidationStatus.Invalid ? 
                optionSettingItem.Validation.InvalidValue : optionSettingItem.Value;

            if (detail.HasValueMappings())
            {
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            }
            return value;
        }

        private static string GetValidationMessage(OptionSettingItemSummary optionSettingItem,
            ConfigurationDetail configurationDetail)
        {
            if (optionSettingItem.Validation?.ValidationStatus != ValidationStatus.Invalid)
            {
                // Fall back to any validation message that the Toolkit may have applied through derived ConfigurationDetail types.
                return configurationDetail.ValidationMessage;
            }

            return optionSettingItem.Validation.ValidationMessage;
        }

        private static IDictionary<string, string> GetValueMappings(OptionSettingItemSummary optionSettingItem)
        {

            if (optionSettingItem.HasValueMapping())
            {
                return optionSettingItem.ValueMapping;
            }

            if (optionSettingItem.HasAllowedValues())
            {
                return optionSettingItem.AllowedValues.ToDictionary(x => x, x => x);
            }

            return new Dictionary<string, string>();
        }
    }
}

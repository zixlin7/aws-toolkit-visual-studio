using System;
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
        private static class ItemSummaryTypes
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

        public ConfigurationDetail CreateFrom(OptionSettingItemSummary itemSummary)
        {
            var configurationDetail = InstantiateFor(itemSummary);

            configurationDetail.Id = itemSummary.Id;
            configurationDetail.Name = itemSummary.Name;
            configurationDetail.OriginalType = itemSummary.Type;
            configurationDetail.Description = itemSummary.Description;
            configurationDetail.Type = GetConfigurationDetailType(itemSummary.Type);
            configurationDetail.TypeHint = itemSummary.TypeHint;
            configurationDetail.DefaultValue = itemSummary.Value;
            // TODO : use category once API provides it. (View is already set to render it)
            configurationDetail.Category = string.Empty;
            configurationDetail.Advanced = itemSummary.Advanced;
            configurationDetail.ReadOnly = itemSummary.ReadOnly;
            configurationDetail.Visible = itemSummary.Visible;
            configurationDetail.SummaryDisplayable = itemSummary.SummaryDisplayable;

            configurationDetail.ValueMappings = GetValueMappings(itemSummary);
            configurationDetail.Value = GetValue(itemSummary, configurationDetail);

            // Recurse all child data
            if (itemSummary.Type.Equals("Object"))
            {
                itemSummary.ChildOptionSettings?
                    .Select(childOption =>
                    {
                        var child = CreateFrom(childOption);
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

        private ConfigurationDetail InstantiateFor(OptionSettingItemSummary itemSummary)
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

            // TODO When https://github.com/aws/aws-dotnet-deploy/pull/509 is merged and that version of the Deploy CLI is merged into the
            // VSTK then uncomment this code to support FilePath TypeHints.
            //if (itemSummary.TypeHint == ConfigurationDetail.TypeHints.FilePath)
            //{
            //    var detail = new FilePathConfigurationDetail()
            //    {
            //        CheckFileExists = GetTypeHintData<bool>(itemSummary, FilePathConfigurationDetail.TypeHintDataKeys.CheckFileExists),
            //        Filter = GetTypeHintData<string>(itemSummary, FilePathConfigurationDetail.TypeHintDataKeys.Filter),
            //        Title = GetTypeHintData<string>(itemSummary, FilePathConfigurationDetail.TypeHintDataKeys.Title)
            //    };
            //    detail.BrowseCommand = OpenFileSelectionCommandFactory.Create(detail, _dialogFactory);
            //
            //    return detail;
            //}

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
            if (detail.HasValueMappings())
            {
                return Convert.ToString(optionSettingItem.Value, CultureInfo.InvariantCulture);
            }
            return optionSettingItem.Value;
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

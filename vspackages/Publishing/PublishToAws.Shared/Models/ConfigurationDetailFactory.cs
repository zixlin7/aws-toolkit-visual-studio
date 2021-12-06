using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Publish.Util;

using AWS.Deploy.ServerMode.Client;

namespace Amazon.AWSToolkit.Publish.Models
{
    public class ConfigurationDetailFactory
    {
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

        private ConfigurationDetail InstantiateFor(OptionSettingItemSummary itemSummary)
        {
            if (itemSummary.TypeHint == ConfigurationDetail.TypeHints.IamRole)
            {
                var detail = new IamRoleConfigurationDetail();
                detail.SelectRoleArn = SelectRoleArnCommandFactory.Create(detail, _publishToAwsProperties, _dialogFactory);

                return detail;
            }

            return new ConfigurationDetail();
        }

        private static Type GetConfigurationDetailType(string itemSummaryType)
        {
            switch (itemSummaryType)
            {
                case "String":
                    return typeof(string);
                case "Int":
                    return typeof(int);
                case "Double":
                    return typeof(double);
                case "Bool":
                    return typeof(bool);
                case "Object":
                    return typeof(object);
                default:
                    throw new UnsupportedOptionSettingItemTypeException($"The Type '{itemSummaryType}' is not supported.");
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

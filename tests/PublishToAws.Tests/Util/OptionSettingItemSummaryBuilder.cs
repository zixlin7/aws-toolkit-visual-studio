using System;
using System.Collections.Generic;
using System.Linq;

using AWS.Deploy.ServerMode.Client;

namespace Amazon.AWSToolkit.Tests.Publishing.Util
{
    public class OptionSettingItemSummaryBuilder
    {
        private readonly OptionSettingItemSummary _itemSummary = new OptionSettingItemSummary();
        private readonly IList<OptionSettingItemSummaryBuilder> _childBuilders = new List<OptionSettingItemSummaryBuilder>();

        private OptionSettingItemSummaryBuilder()
        {
            _itemSummary.ChildOptionSettings = new List<OptionSettingItemSummary>();
        }

        public static OptionSettingItemSummaryBuilder Create()
        {
            return new OptionSettingItemSummaryBuilder();
        }

        public OptionSettingItemSummaryBuilder WithId(string id)
        {
            _itemSummary.Id = id;
            return this;
        }

        public OptionSettingItemSummaryBuilder WithName(string name)
        {
            _itemSummary.Name = name;
            return this;
        }

        public OptionSettingItemSummaryBuilder WithDescription(string description)
        {
            _itemSummary.Description = description;
            return this;
        }

        public OptionSettingItemSummaryBuilder WithType(string type)
        {
            _itemSummary.Type = type;
            return this;
        }

        public OptionSettingItemSummaryBuilder WithTypeHint(string typeHint)
        {
            _itemSummary.TypeHint = typeHint;
            return this;
        }

        public OptionSettingItemSummaryBuilder WithValueMapping(string key, string value)
        {
            if (_itemSummary.ValueMapping == null)
            {
                _itemSummary.ValueMapping = new Dictionary<string, string>();
            }

            _itemSummary.ValueMapping[key] = value;

            return this;
        }

        public OptionSettingItemSummaryBuilder WithAllowedValue(string value)
        {
            if (_itemSummary.AllowedValues == null)
            {
                _itemSummary.AllowedValues = new List<string>();
            }

            _itemSummary.AllowedValues.Add(value);

            return this;
        }

        public OptionSettingItemSummaryBuilder WithValue(object value)
        {
            _itemSummary.Value = value;
            return this;
        }

        public OptionSettingItemSummaryBuilder WithAdvanced()
        {
            _itemSummary.Advanced = true;
            return this;
        }

        public OptionSettingItemSummaryBuilder WithReadOnly()
        {
            _itemSummary.ReadOnly = true;
            return this;
        }

        public OptionSettingItemSummaryBuilder WithVisible()
        {
            _itemSummary.Visible = true;
            return this;
        }

        public OptionSettingItemSummaryBuilder WithSummaryDisplayable()
        {
            _itemSummary.SummaryDisplayable = true;
            return this;
        }

        public OptionSettingItemSummaryBuilder UseSampleData()
        {
            var guid = Guid.NewGuid().ToString();

            return WithId(guid)
                .WithName($"Name of {guid}")
                .WithDescription($"Description for {guid}")
                .WithType("String")
                .WithTypeHint("some-type-hint")
                .WithValue(guid)
                .WithVisible()
                .WithSummaryDisplayable();
        }

        public OptionSettingItemSummaryBuilder WithChild(OptionSettingItemSummaryBuilder childBuilder)
        {
            _childBuilders.Add(childBuilder);
            return this;
        }

        public OptionSettingItemSummary Build()
        {
            _childBuilders
                .Select(childBuilder => childBuilder.Build())
                .ToList()
                .ForEach(_itemSummary.ChildOptionSettings.Add);

            return _itemSummary;
        }
    }
}

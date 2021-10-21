using System;
using System.Collections.Generic;

using Amazon.AWSToolkit.Publish.Models;

namespace Amazon.AWSToolkit.Tests.Publishing.Util
{
    public class ConfigurationDetailBuilder
    {
        private readonly ConfigurationDetail _detail = new ConfigurationDetail();
        private readonly IList<ConfigurationDetailBuilder> _childBuilders = new List<ConfigurationDetailBuilder>();

        private ConfigurationDetailBuilder()
        {
            
        }

        public static ConfigurationDetailBuilder Create()
        {
            return new ConfigurationDetailBuilder();
        }

        public ConfigurationDetailBuilder WithId(string id)
        {
            _detail.Id = id;
            return this;
        }

        public ConfigurationDetailBuilder WithName(string name)
        {
            _detail.Name = name;
            return this;
        }

        public ConfigurationDetailBuilder WithValue(object value)
        {
            _detail.Value = value;
            return this;
        }

        public ConfigurationDetailBuilder WithType(Type type)
        {
            _detail.Type = type;
            return this;
        }

        public ConfigurationDetailBuilder WithSampleError()
        {
            _detail.ValidationMessage = $"Error: {Guid.NewGuid()}";
            return this;
        }

        public ConfigurationDetailBuilder WithChild(ConfigurationDetailBuilder childBuilder)
        {
            _childBuilders.Add(childBuilder);
            return this;
        }

        public ConfigurationDetailBuilder IsVisible()
        {
            _detail.Visible = true;
            return this;
        }

        public ConfigurationDetailBuilder IsSummaryDisplayable()
        {
            _detail.SummaryDisplayable = true;
            return this;
        }

        public ConfigurationDetailBuilder IsAdvanced()
        {
            _detail.Advanced = true;
            return this;
        }

        public ConfigurationDetailBuilder WithSampleData()
        {
            _detail.Name = Guid.NewGuid().ToString();
            _detail.Description = Guid.NewGuid().ToString();
            _detail.Value = Guid.NewGuid().ToString();
            return this;
        }

        public ConfigurationDetail Build()
        {
            foreach (var childBuilder in _childBuilders)
            {
                var child = childBuilder.Build();
                child.Parent = _detail;
                _detail.Children.Add(child);
            }

            return _detail;
        }
    }
}

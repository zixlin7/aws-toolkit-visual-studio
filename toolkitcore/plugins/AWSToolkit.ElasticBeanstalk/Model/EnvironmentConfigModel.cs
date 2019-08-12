using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.CommonUI;

using Amazon.ElasticBeanstalk.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Model
{
    public class EnvironmentConfigModel : BaseModel
    {
        #region Private members

        private string _propertyPrefix;
        private Dictionary<string, string> _changedFields = new Dictionary<string, string>();
        private Dictionary<string, string> _currentValues = new Dictionary<string, string>();
        private Dictionary<string, ConfigurationOptionDescription> _optionDescriptions = new Dictionary<string, ConfigurationOptionDescription>();


        private Dictionary<string, List<ConfigurationOptionDescription>> _propertiesByNamespace = new Dictionary<string,List<ConfigurationOptionDescription>>();

        #endregion

        #region Constructors


        public EnvironmentConfigModel(string propertyPrefix)
        {
            this._propertyPrefix = propertyPrefix;
        }


        #endregion

        #region Private methods

        private string makeSystemName(string nameSpace, string name)
        {
            return nameSpace + "/" + name;
        }

        #endregion

        #region Public properties

        private bool _isConfigDirty;
        public bool IsConfigDirty
        {
            get => this._isConfigDirty;
            set
            {
                this._isConfigDirty = value;
                if (!this._isConfigDirty)
                {
                    this._changedFields.Clear();
                }

                base.NotifyPropertyChanged("IsConfigDirty");
            }
        }

        #endregion

        #region Public methods


        public ConfigurationOptionDescription GetDescription(string ns, string propertyName)
        {
            string systemKey = makeSystemName(ns, propertyName);

            ConfigurationOptionDescription description;
            if (_optionDescriptions.TryGetValue(systemKey, out description))
            {
                return description;
            }
            return null;
        }

        public void LoadConfigDescriptions(List<ConfigurationOptionDescription> descriptions)
        {
            this._propertiesByNamespace = new Dictionary<string, List<ConfigurationOptionDescription>>();

            foreach (var description in descriptions)
            {
                List<ConfigurationOptionDescription> properties = null;
                if (!this._propertiesByNamespace.TryGetValue(description.Namespace, out properties))
                {
                    properties = new List<ConfigurationOptionDescription>();
                    this._propertiesByNamespace.Add(description.Namespace, properties);
                }
                properties.Add(description);

                this._optionDescriptions[makeSystemName(description.Namespace, description.Name)] = description;
            }
        }

        public void LoadConfigOptions(List<ConfigurationOptionSetting> settings, bool changingEnvironmentType)
        {
            if (changingEnvironmentType)
            {
                this._currentValues.Clear();
                this._changedFields.Clear();
            }

            foreach (var setting in settings)
            {
                string systemName = makeSystemName(setting.Namespace, setting.OptionName);

                // Field is dirty so don't overwrite it
                if (this._changedFields.ContainsKey(systemName))
                    continue;

                this._currentValues[systemName] = setting.Value;
                base.NotifyPropertyChanged(setting.OptionName);
            }
        }

        public List<ConfigurationOptionSetting> GetSettings()
        {
            var list = new List<ConfigurationOptionSetting>();

            foreach (var kvp in this._currentValues)
            {
                if (kvp.Value == null)
                    continue;

                var tokens = kvp.Key.Split('/');

                if (tokens[0] == BeanstalkConstants.INTERNAL_PROPERTIES_NAMESPACE)
                    continue;

                var setting = new ConfigurationOptionSetting() { Namespace = tokens[0], OptionName = tokens[1], Value = kvp.Value };
                list.Add(setting);                
            }

            return list;
        }

        public void SetValue(string propertyNamespace, string propertySystemName, object value)
        {
            string systemKey = makeSystemName(propertyNamespace, propertySystemName);

            string currentValue = null;
            this._currentValues.TryGetValue(systemKey, out currentValue);

            // Keep track of the changed field
            if (!this._changedFields.ContainsKey(systemKey))
            {
                this._changedFields[systemKey] = currentValue;
            }
            // value is being reverted to its original value.                
            else if (string.Equals(value, this._changedFields[systemKey]))
            {
                this._changedFields.Remove(systemKey);
            }

            this._currentValues[systemKey] = Convert.ToString(value);

            base.NotifyPropertyChanged(propertySystemName);
            this.IsConfigDirty = true;
        }

        public string GetValue(string propertyNamespace, string propertySystemName)
        {
            string systemKey = makeSystemName(propertyNamespace, propertySystemName);

            string value = null;
            this._currentValues.TryGetValue(systemKey, out value);
            return value;
        }

        public IEnumerable<string> Namespaces => this._propertiesByNamespace.Keys;

        public IEnumerable<ConfigurationOptionDescription> GetProperties(string ns)
        {
            List<ConfigurationOptionDescription> properties = null;
            if (!this._propertiesByNamespace.TryGetValue(ns, out properties))
                return new List<ConfigurationOptionDescription>();

            return properties;
        }

        #endregion
    }
}

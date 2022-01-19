using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Publish.Models.Configuration;

namespace Amazon.AWSToolkit.Publish.Models
{
    /// <summary>
    /// A primitive that describes a singular configuration value/field of a publish target.
    /// This is the model that translates from the CLI server's API to what is shown in the UI
    /// (see <see cref="ConfigurationDetailPropertyDescriptor"/> for the UI related property descriptors).
    /// </summary>
    [DebuggerDisplay("{Id} | {Value}")]
    public class ConfigurationDetail : BaseModel, INotifyDataErrorInfo
    {
        public static class TypeHints
        {
            public const string IamRole = "IAMRole";
            public const string Vpc = "Vpc";
            public const string InstanceType = "InstanceType";
        }

        private bool _suspendDetailChangeEvents = false;
        private string _id;
        private string _name;
        private string _description;
        private Type _type;
        private string _originalType;
        private string _typeHint;
        private object _value;
        private object _defaultValue;
        private string _category;
        private bool _advanced;
        private bool _readOnly;
        private bool _visible;
        private bool _summaryDisplayable;
        private string _validationMessage;
        private ConfigurationDetail _parent;
        private readonly List<ConfigurationDetail> _children = new List<ConfigurationDetail>();
        private IDictionary<string, string> _valueMappings = new Dictionary<string, string>();

        public event EventHandler<DetailChangedEventArgs> DetailChanged;

        public ConfigurationDetail()
        {
            PropertyChanged += ConfigurationDetail_PropertyChanged;
        }

        private void ConfigurationDetail_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Value))
            {
                OnDetailPropertyChanged(sender, e);
            }
        }

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public bool HasErrors => !string.IsNullOrWhiteSpace(ValidationMessage);

        /// <summary>
        /// The Id of the configuration field
        /// </summary>
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// The display-friendly name of this configuration field
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// The configuration detail's <see cref="OptionSettingItemSummary"/> original type
        /// (the unaltered version, different from <see cref="Type"/>)
        /// </summary>
        public string OriginalType
        {
            get => _originalType;
            set => SetProperty(ref _originalType, value);
        }

        /// <summary>
        /// The display-friendly name of this configuration field, including the 
        /// full chain of parent details.
        /// Example: "Foo : Bar : Baz"
        /// </summary>
        public string FullDisplayName
        {
            get => GetFullDisplayName();
        }

        /// <summary>
        /// User-friendly explanation of the configuration field
        /// </summary>
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        /// <summary>
        /// The configuration field's type, as interpreted by the Toolkit (and how the UI displays it).
        /// </summary>
        public Type Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        /// <summary>
        /// The configuration field's type hint, used to help the toolkit with the UI display.
        /// </summary>
        public string TypeHint
        {
            get => _typeHint;
            set => SetProperty(ref _typeHint, value);
        }

        /// <summary>
        /// The configuration field's value
        /// </summary>
        public object Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        /// <summary>
        /// The configuration field's default (or recommended) value
        /// </summary>
        public object DefaultValue
        {
            get => _defaultValue;
            set => SetProperty(ref _defaultValue, value);
        }

        /// <summary>
        /// Display-friendly name of the group that the configuration field belongs to.
        /// </summary>
        public string Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        /// <summary>
        /// Indicates whether the configuration field is an advanced configuration.
        /// </summary>
        public bool Advanced
        {
            get => _advanced;
            set => SetProperty(ref _advanced, value);
        }

        /// <summary>
        /// Indicates whether the configuration field can be updated.
        /// </summary>
        public bool ReadOnly
        {
            get => _readOnly;
            set => SetProperty(ref _readOnly, value);
        }

        /// <summary>
        /// Indicates whether the configuration field should be displayed in the UI
        /// </summary>
        public bool Visible
        {
            get => _visible;
            set => SetProperty(ref _visible, value);
        }

        /// <summary>
        /// Indicates whether the configuration field should be displayed in the summary for a republish target
        /// </summary>
        public bool SummaryDisplayable
        {
            get => _summaryDisplayable;
            set => SetProperty(ref _summaryDisplayable, value);
        }

        /// <summary>
        /// Error validation results against <see cref="Value"/> (if any)
        /// </summary>
        public string ValidationMessage
        {
            get => _validationMessage;
            set
            {
                if (Equals(_validationMessage, value))
                {
                    return;
                }

                _validationMessage = value;

                NotifyPropertyChanged(nameof(ValidationMessage));
                RaiseErrorsChanged(nameof(Value));
            }
        }

        /// <summary>
        /// The parent configuration field.
        /// </summary>
        public ConfigurationDetail Parent
        {
            get => _parent;
            set => SetProperty(ref _parent, value);
        }

        /// <summary>
        /// The set of allowed values (key) and their corresponding user-facing display values (value)
        /// </summary>
        public IDictionary<string, string> ValueMappings
        {
            get => _valueMappings;
            set => SetProperty(ref _valueMappings, value);
        }

        /// <summary>
        /// Configuration details that belong to this instance.
        /// Child details may also contain children, with no depth limit.
        /// </summary>
        public IReadOnlyList<ConfigurationDetail> Children => _children;

        public void ClearChildren()
        {
            _children.ToList().ForEach(RemoveChild);
            _children.Clear();
        }

        protected virtual void RemoveChild(ConfigurationDetail child)
        {
            UnListenToChild(child);
            _children.Remove(child);
        }

        private void UnListenToChild(ConfigurationDetail child)
        {
            child.PropertyChanged -= OnDetailPropertyChanged;
        }

        public virtual void AddChild(ConfigurationDetail child)
        {
            _children.Add(child);
            ListenToChild(child);
        }

        private void ListenToChild(ConfigurationDetail child)
        {
            child.PropertyChanged += OnDetailPropertyChanged;
        }

        private void OnDetailPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_suspendDetailChangeEvents)
            {
                return;
            }

            if (e.PropertyName != nameof(Value))
            {
                return;
            }

            if (!(sender is ConfigurationDetail detail))
            {
                return;
            }

            RaiseConfigurationDetailChanged(detail);
        }

        protected void RaiseConfigurationDetailChanged(ConfigurationDetail detail)
        {
            DetailChanged?.Invoke(this, new DetailChangedEventArgs(detail));
        }

        protected void SuspendDetailChangeEvents() => _suspendDetailChangeEvents = true;
        protected void ResumeDetailChangeEvents() => _suspendDetailChangeEvents = false;

        protected void SuspendDetailChangeEvents(Action action)
        {
            try
            {
                SuspendDetailChangeEvents();
                action();
            }
            finally
            {
                ResumeDetailChangeEvents();
            }
        }

        public IEnumerable GetErrors(string propertyName)
        {
            if (propertyName == nameof(Value) && !string.IsNullOrWhiteSpace(ValidationMessage))
            {
                return Enumerable.Repeat(ValidationMessage, 1);
            }

            return Enumerable.Empty<string>();
        }

        private void RaiseErrorsChanged(string propertyName)
        {
            NotifyPropertyChanged(nameof(HasErrors));
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public string GetLeafId()
        {
            var configurationDetailParent = Parent;

            var ids = new List<string>() { Id };

            while (configurationDetailParent != null)
            {
                ids.Insert(0, configurationDetailParent.Id);
                configurationDetailParent = configurationDetailParent.Parent;
            }

            return string.Join(".", ids);
        }

        public string GetFullDisplayName()
        {
            if (Parent == null)
            {
                return Name;
            }

            return string.Join(" : ", new string[] { Parent.FullDisplayName, Name });
        }

        /// <summary>
        /// Retrieves this node and all of its children, recursively.
        /// </summary>
        /// <param name="filter">Optional filter to specify which nodes are included. Children are not traversed for excluded nodes.</param>
        public IEnumerable<ConfigurationDetail> GetSelfAndDescendants(Predicate<ConfigurationDetail> filter = null)
        {
            if (filter?.Invoke(this) ?? true)
            {
                return Enumerable.Repeat(this, 1).Concat(Children.GetDetailAndDescendants(filter));
            }

            return Enumerable.Empty<ConfigurationDetail>();
        }

        public bool IsLeaf()
        {
            return !Children.Any();
        }

        public bool HasValueMappings()
        {
            return ValueMappings?.Any() ?? false;
        }
    }
}

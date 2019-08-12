using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;

namespace Amazon.AWSToolkit.CommonUI.Components
{
    /// <summary>
    /// Control used to display a list of Beanstalk/CloudFormation templates with parsed
    /// header/description text from a backing json data source
    /// </summary>
    public partial class TemplatePickerControl : INotifyPropertyChanged
    {
        public static readonly string uiProperty_Template = "template";

        ObservableCollection<DeploymentTemplateWrapperBase> _wrappedTemplates = new ObservableCollection<DeploymentTemplateWrapperBase>();

        public TemplatePickerControl()
        {
            InitializeComponent();
        }

        public IEnumerable<DeploymentTemplateWrapperBase> Templates
        {
            get
            {
                List<DeploymentTemplateWrapperBase> templates = new List<DeploymentTemplateWrapperBase>();
                foreach (DeploymentTemplateWrapperBase template in _wrappedTemplates)
                {
                    templates.Add(template);
                }

                return templates;
            }

            set
            {
                _wrappedTemplates.Clear();
                foreach (var template in value)
                {
                    _wrappedTemplates.Add(template);
                }

                this._templateList.ItemsSource = _wrappedTemplates;
                this._templateList.Cursor = Cursors.Arrow;
            }
        }

        public DeploymentTemplateWrapperBase SelectedTemplate => this._templateList.SelectedItem as DeploymentTemplateWrapperBase;

        ObservableCollection<DeploymentTemplateWrapperBase> BoundTemplates => _wrappedTemplates;

        private void _templateList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_Template);
        }
    }
}

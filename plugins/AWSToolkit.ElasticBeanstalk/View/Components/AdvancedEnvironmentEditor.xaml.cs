using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.AWSToolkit.ElasticBeanstalk.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.View.Components
{
    /// <summary>
    /// Interaction logic for AdvancedEnvironmentEditor.xaml
    /// </summary>
    public partial class AdvancedEnvironmentEditor
    {
        public AdvancedEnvironmentEditor()
        {
            InitializeComponent();
            this.DataContextChanged += onDataContextChanged;
        }

        public void Rebuild()
        {
            this._ctlMainPanel.Children.Clear();
            buildEditor();
        }

        void onDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            buildEditor();
        }

        void buildEditor()
        {
            var model = this.DataContext as EnvironmentStatusModel;
            if (model == null)
                return;

            var config = model.ConfigModel;

            foreach (var ns in config.Namespaces.OrderBy(x => x))
            {
                if (ns == BeanstalkConstants.INTERNAL_PROPERTIES_NAMESPACE)
                    continue;

                addNamespaceHeader(this._ctlMainPanel, ns);
                addProperties(this._ctlMainPanel, config, ns);
            }
        }

        FrameworkElement addProperties(Grid grid, EnvironmentConfigModel configModel, string ns)
        {
            Thickness defaultMargin = new Thickness(30,5,5,5);
            

            var properties = configModel.GetProperties(ns);
            int rowIndex = grid.RowDefinitions.Count;
            foreach (var property in properties.OrderBy(x => x.Name))
            {
                grid.RowDefinitions.Add(new RowDefinition(){Height=GridLength.Auto});

                TextBlock label = new TextBlock(); 
                label.Text = property.Name;
                label.Margin = defaultMargin;
                Grid.SetRow(label, rowIndex);
                Grid.SetColumn(label, 0);
                grid.Children.Add(label);

                // EnvironmentType hack; this is a readonly label, user must change
                // environment type via drop menu on page
                if (property.Namespace.Equals(BeanstalkConstants.ENVIRONMENT_NAMESPACE) 
                    && property.Name.Equals(BeanstalkConstants.ENVIRONMENTTYPE_OPTION))
                {
                    TextBlock typeLabel = new TextBlock();
                    typeLabel.Text = configModel.GetValue(property.Namespace, property.Name);
                    typeLabel.Margin = defaultMargin;
                    Grid.SetRow(typeLabel, rowIndex);
                    Grid.SetColumn(typeLabel, 1);
                    grid.Children.Add(typeLabel);
                }
                else
                {
                    ConfigurationOptionControl edit = new ConfigurationOptionControl();
                    edit.Margin = defaultMargin;
                    edit.PropertySystemName = property.Name;
                    edit.PropertyNamespaceName = property.Namespace;
                    edit.DataContext = configModel;
                    Grid.SetRow(edit, rowIndex);
                    Grid.SetColumn(edit, 1);
                    grid.Children.Add(edit);

                    ConfigurationOptionRangeLabelControl info = new ConfigurationOptionRangeLabelControl();
                    info.Margin = defaultMargin;
                    info.PropertySystemName = property.Name;
                    info.PropertyNamespaceName = property.Namespace;
                    info.DataContext = configModel;
                    Grid.SetRow(info, rowIndex);
                    Grid.SetColumn(info, 2);
                    grid.Children.Add(info);
                }

                rowIndex++;
            }

            return grid;
        }

        void addNamespaceHeader(Grid grid, string ns)
        {
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            DockPanel panel = new DockPanel();
            
            TextBlock header = new TextBlock();
            header.Text = ns;
            header.Margin = new Thickness(5, 5, 10, 5);
            DockPanel.SetDock(header, Dock.Left);
            panel.Children.Add(header);

            Separator seperator = new Separator();
            seperator.Height = 0.5;
            panel.Children.Add(seperator);

            Grid.SetRow(panel, grid.RowDefinitions.Count - 1);
            Grid.SetColumn(panel, 0);
            Grid.SetColumnSpan(panel, grid.ColumnDefinitions.Count);
            grid.Children.Add(panel);
        }
    }
}

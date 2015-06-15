using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Configuration;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Amazon.AWSToolkit.VisualStudio.Shared;
using log4net;

using Amazon.AWSToolkit;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

using Amazon.AWSToolkit.MobileAnalytics;

//using Amazon.AWSToolkit.VisualStudio.Shared;

namespace Amazon.AWSToolkit.Studio
{
    /// <summary>
    /// Interaction logic for AWSStudioWindow.xaml
    /// </summary>
    public partial class AWSStudioWindow : Window, IShellProvider
    {
        Dictionary<string, CloseableTabItem> _openTabs = new Dictionary<string, CloseableTabItem>();

        public AWSStudioWindow()
        {
            log4net.Config.XmlConfigurator.Configure();

            InitializeComponent();
            this.WindowState = WindowState.Maximized;

            ToolkitFactory.InitializeToolkit(this._navigation, this, null);

            this.AddHandler(CloseableTabItem.CloseTabEvent, new RoutedEventHandler(this.CloseTab));
//            OpenInEditor(new Browser());
        }

        public string ShellName
        {
            get { return AWSToolkit.Constants.AWSStudioHostShell.ShellName; }
        }

        private void CloseTab(object source, RoutedEventArgs args)
        {
            TabItem tabItem = args.Source as TabItem;
            if (tabItem != null)
            {
                TabControl tabControl = tabItem.Parent as TabControl;
                if (tabControl != null)
                {
                    tabControl.Items.Remove(tabItem);

                    foreach (KeyValuePair<string, CloseableTabItem> kvp in this._openTabs)
                    {
                        if (tabItem.Equals(kvp.Value))
                        {
                            this._openTabs.Remove(kvp.Key);
                            break;
                        }
                    }
                }
            }

            if (this._ctlEditorTab.Items.Count == 0)
            {
                this._ctlEditorTab.Background = new SolidColorBrush(Colors.Gray);
            }
        }

        public void OpenShellWindow(ShellWindows window)
        {
        }

        public void OpenInEditor(IAWSControl editorControl)
        {
            string uniqueId = editorControl.UniqueId;
            if (ToolkitFactory.Instance.Navigator.SelectedAccount != null)
            {
                uniqueId += this._navigation.SelectedAccount.SettingsUniqueKey;
            }
            if (ToolkitFactory.Instance.Navigator.SelectedRegionEndPoints != null)
            {
                uniqueId += ToolkitFactory.Instance.Navigator.SelectedRegionEndPoints.SystemName;
            }

            if (this._openTabs.ContainsKey(uniqueId))
            {
                var existingTab = this._openTabs[uniqueId];

                existingTab.HostedEditor.RefreshInitialData(editorControl.GetInitialData());
                this._ctlEditorTab.SelectedItem = existingTab;
            }
            else
            {
                // Start the data to be load in the background
                editorControl.ExecuteBackGroundLoadDataLoad();


                CloseableTabItem tab = new CloseableTabItem();
                tab.Add(editorControl);
                this._openTabs[uniqueId] = tab;

                if (this._ctlEditorTab.Items.Count == 0)
                {
                    var backColor = Color.FromArgb(0xFF, 0xF9, 0xF9, 0xF9);
                    this._ctlEditorTab.Background = new SolidColorBrush(backColor);
                }

                // Update Tab's header when control's title is updated.
                editorControl.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
                {
                    if (e.PropertyName.Equals("Title"))
                    {
                        tab.Header = editorControl.Title;
                    }
                };

                this._ctlEditorTab.Items.Add(tab);
                this._ctlEditorTab.SelectedItem = tab;

                if (editorControl is IPropertySupport)
                {
                    var property = editorControl as IPropertySupport;
                    property.OnPropertyChange += AWSStudioWindow.OnViewPropertyChange;
                }
            }
        }

        public void OpenInEditor(string fileName)
        {
        }

        static void OnViewPropertyChange(object sender, bool forceShow, System.Collections.IList propertyObjects)
        {
            if (forceShow)
            {
                var propertiesControl = new PropertiesControl(propertyObjects);
                ToolkitFactory.Instance.ShellProvider.ShowModal(propertiesControl, MessageBoxButton.OK);
            }
        }


        public bool ShowModal(IAWSControl hostedControl)
        {
            return ShowModal(hostedControl, MessageBoxButton.OKCancel);
        }

        public bool ShowModal(IAWSControl hostedControl, MessageBoxButton buttons)
        {
            Window host = DialogHostUtil.CreateDialogHost(buttons, hostedControl);

            host.Owner = this;
            host.HorizontalAlignment = HorizontalAlignment.Center;
            host.VerticalAlignment = VerticalAlignment.Center;
            bool dialogResult = host.ShowDialog().GetValueOrDefault();
            return dialogResult;
        }

        public bool ShowModalFrameless(IAWSControl hostedControl)
        {
            Window host = DialogHostUtil.CreateFramelessDialogHost(hostedControl);
            return ShowModal(host, hostedControl.MetricId);
        }

        public bool ShowModal(Window window, string metricId)
        {
            SimpleMobileAnalytics recorder = SimpleMobileAnalytics.Instance;
            recorder.AddProperty(Attributes.OpenViewFullIdentifier, metricId);
            recorder.RecordEventWithProperties();
            window.Owner = this;
            return window.ShowDialog().GetValueOrDefault();
        }

        public void ShowError(string message)
        {
            this.ShowError("Error!", message);
        }

        public void ShowError(string title, string message)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            }));
        }

        public void ShowErrorWithLinks(string title, string message)
        {
            this.ShellDispatcher.Invoke((Action)(() =>
            {
                Messaging.ShowErrorWithLinks(this, title, message);
            }));
        }

        public void ShowMessage(string title, string message)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            }));
        }

        public void UpdateStatus(string status)
        {
            this.ShellDispatcher.BeginInvoke((Action)(() => 
                {
                    this._ctlStatusField.Text = status;
                }));
        }

        public bool Confirm(string title, string msg)
        {
			return Confirm(title, msg, MessageBoxButton.YesNo);
        }

        public bool Confirm(string title, string msg, MessageBoxButton buttons)
        {
            MessageBoxResult result = MessageBox.Show(this, msg, title, buttons, MessageBoxImage.Exclamation);
            return result == MessageBoxResult.Yes || result == MessageBoxResult.OK;
        }

        public Dispatcher ShellDispatcher
        {
            get { return this.Dispatcher; }
        }

        public void OutputToHostConsole(string message)
        {
            OutputToHostConsole(message, false);
        }

        public void OutputToHostConsole(string message, bool forceVisible)
        {
        }

        public T QueryShellProverService<T>() where T : class
        {
            return this as T;
        }

    }
}

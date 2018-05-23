using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.MobileAnalytics;
using Amazon.AWSToolkit.Shared;

namespace Amazon.AWSToolkit.Studio
{
    /// <summary>
    /// Interaction logic for AWSStudioWindow.xaml
    /// </summary>
    public partial class AWSStudioWindow : Window, IAWSToolkitShellProvider
    {
        readonly Dictionary<string, CloseableTabItem> _openTabs = new Dictionary<string, CloseableTabItem>();

        public AWSStudioWindow()
        {
            log4net.Config.XmlConfigurator.Configure();

            InitializeComponent();
            this.WindowState = WindowState.Maximized;

            ToolkitFactory.InitializeToolkit(this._navigation, this, null, () =>
            {
                this.AddHandler(CloseableTabItem.CloseTabEvent, new RoutedEventHandler(this.CloseTab));
            });
        }

        public string ShellName
        {
            get { return AWSToolkit.Constants.AWSStudioHostShell.ShellName; }
        }

        public string ShellVersion
        {
            get { return "2017"; }
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

        public void OpenInEditor(IAWSToolkitControl editorControl)
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


        public bool ShowModal(IAWSToolkitControl hostedControl)
        {
            return ShowModal(hostedControl, MessageBoxButton.OKCancel);
        }

        public bool ShowModal(IAWSToolkitControl hostedControl, MessageBoxButton buttons)
        {
            Window host = DialogHostUtil.CreateDialogHost(buttons, hostedControl);

            host.Owner = this;
            host.HorizontalAlignment = HorizontalAlignment.Center;
            host.VerticalAlignment = VerticalAlignment.Center;
            bool dialogResult = host.ShowDialog().GetValueOrDefault();
            return dialogResult;
        }

        public bool ShowModalFrameless(IAWSToolkitControl hostedControl)
        {
            Window host = DialogHostUtil.CreateFramelessDialogHost(hostedControl);
            return ShowModal(host, hostedControl.MetricId);
        }

        public bool ShowModal(Window window, string metricId)
        {
            ToolkitEvent toolkitEvent = new ToolkitEvent();
            toolkitEvent.AddProperty(AttributeKeys.OpenViewFullIdentifier, metricId);
            SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(toolkitEvent);

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

        public void AddToLog(string category, string message)
        {
        }

        public T QueryShellProviderService<T>() where T : class
        {
            return null;
        }

        public object QueryAWSToolkitPluginService(Type pluginServiceType)
        {
            return null;
        }

        public object QueryAWSToolkitPluginService(string pluginServiceType)
        {
            return null;
        }

        public void OpenInBrowser(string url, bool preferInternalBrowser)
        {
        }

        public void CloseEditor(IAWSToolkitControl editorControl)
        {
        }

        public void CloseEditor(string fileName)
        {
        }

        public T QueryShellProverService<T>() where T : class
        {
            return this as T;
        }

        private void _beanstalkAspNet_OnClick(object sender, RoutedEventArgs e)
        {
        }

        private void _beanstalkAspNetCore_OnClick(object sender, RoutedEventArgs e)
        {
        }

        private void _lambdaNetCore10Function_OnClick(object sender, RoutedEventArgs e)
        {
        }

        private void _lambdaNetCore10Serverless_OnClick(object sender, RoutedEventArgs e)
        {
        }

        private void _lambdaNetCore20Function_OnClick(object sender, RoutedEventArgs e)
        {
        }

        private void _lambdaNetCore20Serverless_OnClick(object sender, RoutedEventArgs e)
        {
        }
    }
}

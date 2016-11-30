using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.MobileAnalytics;
using Amazon.AWSToolkit.Shared;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Amazon.AWSToolkit.VisualStudio.Services
{
    /// <summary>
    /// https://blogs.msdn.microsoft.com/mshneer/2009/12/07/vs-2010-compiler-error-interop-type-xxx-cannot-be-embedded-use-the-applicable-interface-instead/
    /// </summary>
    internal class EnvDTEConstants
    {
        public const string vsViewKindTextView = "{7651A703-06E5-11D1-8EBD-00A0C90F26EA}";
    }

    internal class AWSToolkitShellProviderService : SAWSToolkitShellProvider, IAWSToolkitShellProvider
    {
        private AWSToolkitPackage _hostPackage;

        public AWSToolkitShellProviderService(AWSToolkitPackage hostPackage)
        {
            _hostPackage = hostPackage;
        }

        #region IAWSToolkitShellProvider

        string _knownShell = null;
        public string ShellName
        {
            get
            {
                if (string.IsNullOrEmpty(_knownShell))
                {
                    var dte = (EnvDTE.DTE)_hostPackage.GetVSShellService(typeof(EnvDTE.DTE));
                    if (dte != null) // null can happen during initialization
                    {
                        if (dte.Version.StartsWith("12"))
                            _knownShell = Constants.VS2013HostShell.ShellName;
                        else
                            _knownShell = Constants.VS2015HostShell.ShellName;
                    }
                }

                return _knownShell ?? Constants.VS2013HostShell.ShellName;
            }
        }

        string _shellVersion = null;
        public string ShellVersion
        {
            get
            {
                if (string.IsNullOrEmpty(_shellVersion))
                {
                    var dte = (EnvDTE.DTE)_hostPackage.GetVSShellService(typeof(EnvDTE.DTE));
                    if (dte != null) // null can happen during initialization
                    {
                        if (dte.Version.StartsWith("12"))
                            _shellVersion = "2013";
                        else
                            _shellVersion = "2015";
                    }
                }

                return _shellVersion ?? "2013";
            }
        }

        public void OpenShellWindow(ShellWindows window)
        {
            switch (window)
            {
                case ShellWindows.Explorer:
                    ShellDispatcher.Invoke((Action)(_hostPackage.ShowExplorerWindow));
                    break;

                case ShellWindows.Output:
                    break;
            }
        }

        public void OpenInEditor(IAWSToolkitControl editorControl)
        {
            var openShell = _hostPackage.GetVSShellService(typeof(IVsUIShellOpenDocument)) as IVsUIShellOpenDocument;

            ToolkitEvent toolkitEvent = new ToolkitEvent();
            toolkitEvent.AddProperty(AttributeKeys.OpenViewFullIdentifier, editorControl.GetType().FullName);
            _hostPackage.AnalyticsRecorder.QueueEventToBeRecorded(toolkitEvent);

            var logicalView = VSConstants.LOGVIEWID_Primary;
            var editorFactoryGuid = new Guid(GuidList.HostedEditorFactoryGuidString);

            var controlId = (uint)(Interlocked.Increment(ref _hostPackage.ControlCounter));

            _hostPackage.ControlCache[controlId] = new WeakReference(editorControl);
            try
            {
                var uniqueId = editorControl.UniqueId;
                if (ToolkitFactory.Instance.Navigator.SelectedAccount != null)
                {
                    uniqueId += ToolkitFactory.Instance.Navigator.SelectedAccount.SettingsUniqueKey;
                }
                if (ToolkitFactory.Instance.Navigator.SelectedRegionEndPoints != null)
                {
                    uniqueId += ToolkitFactory.Instance.Navigator.SelectedRegionEndPoints.SystemName;
                }

                string filename;
                if (!_hostPackage.ControlUniqueNameToFileName.TryGetValue(uniqueId, out filename))
                {
                    filename = Guid.NewGuid() + ".hostedControl";
                    _hostPackage.ControlUniqueNameToFileName[uniqueId] = filename;
                    _hostPackage.OpenedEditors[filename] = new WeakReference(editorControl);
                }
                else if (_hostPackage.OpenedEditors.ContainsKey(filename))
                {
                    var wr = _hostPackage.OpenedEditors[filename];
                    if (wr.IsAlive)
                    {
                        var existingOpenEditor = wr.Target as IAWSToolkitControl;
                        existingOpenEditor.RefreshInitialData(editorControl.GetInitialData());
                    }
                }

                IVsWindowFrame frame;
                var result = openShell.OpenSpecificEditor(0,  // grfOpenSpecific 
                                                          _hostPackage.GetTempFileLocation() + "/" + filename, // pszMkDocument 
                                                          ref editorFactoryGuid,  // rGuidEditorType 
                                                          null, // pszPhysicalView 
                                                          ref logicalView, // rguidLogicalView +++
                                                          editorControl.Title, // pszOwnerCaption 
                                                          _hostPackage._navigatorVsUIHierarchy, // pHier 
                                                          controlId, // itemid 
                                                          new IntPtr(0), // punkDocDataExisting 
                                                          null, // pSPHierContext 
                                                          out frame);

                if (result != VSConstants.S_OK)
                {
                    _hostPackage.ControlCache.Remove(controlId);
                    Trace.WriteLine(result);
                }
                else
                {
                    frame.Show();
                }
            }
            catch
            {
                _hostPackage.ControlCache.Remove(controlId);
            }

            ThreadPool.QueueUserWorkItem(_hostPackage.ClearDeadWeakReferences, null);
        }

        public void OpenInEditor(string fileName)
        {
            try
            {
                var dte = (EnvDTE.DTE)_hostPackage.GetVSShellService(typeof(EnvDTE.DTE));
                dte.ItemOperations.OpenFile(fileName, EnvDTEConstants.vsViewKindTextView);
            }
            catch (Exception e)
            {
                ShowError(string.Format("Failed to open file {0}, exception message {1}", fileName, e.Message));
            }
        }

        public bool ShowModal(IAWSToolkitControl hostedControl)
        {
            return ShowModal(hostedControl, MessageBoxButton.OKCancel);
        }

        public bool ShowModal(IAWSToolkitControl hostedControl, MessageBoxButton buttons)
        {
            var host = DialogHostUtil.CreateDialogHost(buttons, hostedControl);
            return ShowModal(host, hostedControl.MetricId);
        }

        public bool ShowModalFrameless(IAWSToolkitControl hostedControl)
        {
            var host = DialogHostUtil.CreateFramelessDialogHost(hostedControl);
            return ShowModal(host, hostedControl.MetricId);
        }

        public bool ShowModal(Window window, string metricId)
        {
            ToolkitEvent toolkitEvent = new ToolkitEvent();
            toolkitEvent.AddProperty(AttributeKeys.OpenViewFullIdentifier, metricId);
            _hostPackage.AnalyticsRecorder.QueueEventToBeRecorded(toolkitEvent);

            var uiShell = (IVsUIShell)_hostPackage.GetVSShellService(typeof(SVsUIShell));
            IntPtr parent;
            if (uiShell.GetDialogOwnerHwnd(out parent) != VSConstants.S_OK)
            {
                Trace.Fail("Failed to get hwnd for ShowModal: " + window.Title);
                return false;
            }

            try
            {
                window.HorizontalAlignment = HorizontalAlignment.Center;
                window.VerticalAlignment = VerticalAlignment.Center;

                var wih = new WindowInteropHelper(window);
                wih.Owner = parent;

                uiShell.EnableModeless(0);
                var dialogResult = window.ShowDialog().GetValueOrDefault();
                return dialogResult;
            }
            catch (Exception e)
            {
                Trace.Fail("Error displaying modal dialog: " + e.Message);
                return false;
            }
            finally
            {
                uiShell.EnableModeless(1);
            }
        }

        public void ShowError(string message)
        {
            ShowError("Error", message);
        }

        public void ShowError(string title, string message)
        {
            ShellDispatcher.Invoke((Action)(() => MessageBox.Show(_hostPackage.GetParentWindow(), message, title, MessageBoxButton.OK, MessageBoxImage.Error)));
        }

        public void ShowErrorWithLinks(string title, string message)
        {
            ShellDispatcher.Invoke((Action)(() => Messaging.ShowErrorWithLinks(_hostPackage.GetParentWindow(), title, message)));
        }

        public void ShowMessage(string title, string message)
        {
            ShellDispatcher.Invoke((Action)(() => MessageBox.Show(_hostPackage.GetParentWindow(), message, title, MessageBoxButton.OK, MessageBoxImage.Information)));
        }

        public bool Confirm(string title, string message)
        {
            return Confirm(title, message, MessageBoxButton.YesNo);
        }

        public bool Confirm(string title, string message, MessageBoxButton buttons)
        {
            var uiShell = (IVsUIShell)_hostPackage.GetVSShellService(typeof(SVsUIShell));
            IntPtr parent;
            if (uiShell.GetDialogOwnerHwnd(out parent) == VSConstants.S_OK)
            {
                var host = new Window();
                var wih = new WindowInteropHelper(host);
                wih.Owner = parent;

                var result = MessageBox.Show(host, message, title, buttons, MessageBoxImage.Exclamation);
                return result == MessageBoxResult.Yes || result == MessageBoxResult.OK;
            }

            Trace.Fail("Failed to get hwnd for error message: " + message);
            return false;
        }

        public void UpdateStatus(string status)
        {
            try
            {
                var statusBar = (IVsStatusbar)_hostPackage.GetVSShellService(typeof(SVsStatusbar));
                int frozen;

                statusBar.IsFrozen(out frozen);

                if (frozen == 0)
                {
                    if (string.IsNullOrEmpty(status))
                        statusBar.Clear();
                    else
                        statusBar.SetText(status);
                }
            }
            catch (Exception) { }
        }

        public Dispatcher ShellDispatcher
        {
            get { return _hostPackage.ShellDispatcher; }
        }

        public void OutputToHostConsole(string message)
        {
            _hostPackage.OutputToConsole(message, false);
        }

        public void OutputToHostConsole(string message, bool forceVisible)
        {
            _hostPackage.OutputToConsole(message, forceVisible);
        }

        public void AddToLog(string category, string message)
        {
            if (string.Compare(category, "info", true) == 0)
            {
                _hostPackage.Logger.Info(message);
                return;
            }

            if (string.Compare(category, "warn", true) == 0)
            {
                _hostPackage.Logger.Warn(message);
                return;
            }

            if (string.Compare(category, "error", true) == 0)
            {
                _hostPackage.Logger.Error(message);
                return;
            }

            if (string.Compare(category, "debug", true) == 0)
            {
                _hostPackage.Logger.Debug(message);
                return;
            }

            // don't throw it away if caller got category wrong
            _hostPackage.Logger.InfoFormat("Request to AddToLog with unknown category '{0}', message is '{1}'", category, message);
        }

        public T QueryShellProverService<T>() where T : class
        {
            var svc = this as T;
            if (svc != null)
                return svc;
            else
                return _hostPackage as T;
        }

        public object QueryAWSToolkitPluginService(Type pluginServiceType)
        {
            return ToolkitFactory.Instance.QueryPluginService(pluginServiceType);
        }

        public object QueryAWSToolkitPluginService(string pluginServiceType)
        {
            throw new NotImplementedException();
        }

        #endregion

        private AWSToolkitShellProviderService() { }
    }
}

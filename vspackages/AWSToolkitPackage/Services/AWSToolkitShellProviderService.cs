﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.MessageBox;
using Amazon.AWSToolkit.MobileAnalytics;
using Amazon.AWSToolkit.Shared;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Amazon.AWSToolkit.VisualStudio.Services
{
    /// <summary>
    /// https://blogs.msdn.microsoft.com/mshneer/2009/12/07/vs-2010-compiler-error-interop-type-xxx-cannot-be-embedded-use-the-applicable-interface-instead/
    /// </summary>
    internal class EnvDTEConstants
    {
        // Guid Source: https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.logicalview?view=visualstudiosdk-2017
        // Represents a Logical Text View of a VS Editor
        public const string vsViewKindTextView = "{7651A703-06E5-11D1-8EBD-00A0C90F26EA}";
    }

    internal class AWSToolkitShellProviderService : SAWSToolkitShellProvider, IAWSToolkitShellProvider
    {
        private AWSToolkitPackage _hostPackage;
        private string _shellVersion;

        public AWSToolkitShellProviderService(AWSToolkitPackage hostPackage, string shellVersion)
        {
            _hostPackage = hostPackage;
            _shellVersion = shellVersion;
        }

        #region IAWSToolkitShellProvider

        string _knownShell = null;
        public string ShellName
        {
            get
            {
                if (string.IsNullOrEmpty(_knownShell))
                {
                    if (_shellVersion != null) // null can happen during initialization
                    {
                        if (_shellVersion.StartsWith("12"))
                            _knownShell = Constants.VS2013HostShell.ShellName;
                        else if (_shellVersion.StartsWith("14"))
                            _knownShell = Constants.VS2015HostShell.ShellName;
                        else if (_shellVersion.StartsWith("14"))
                            _knownShell = Constants.VS2015HostShell.ShellName;
                        else if (_shellVersion.StartsWith("15"))
                            _knownShell = Constants.VS2017HostShell.ShellName;
                        else
                            _knownShell = Constants.VS2019HostShell.ShellName;
                    }
                }

                return _knownShell ?? Constants.VS2013HostShell.ShellName;
            }
        }

        string _shellDisplayVersion = null;
        public string ShellVersion
        {
            get
            {
                if (string.IsNullOrEmpty(_shellDisplayVersion))
                {
                    if (_shellVersion != null) // null can happen during initialization
                    {
                        if (_shellVersion.StartsWith("12"))
                            _shellDisplayVersion = "2013";
                        else if (_shellVersion.StartsWith("14"))
                            _shellDisplayVersion = "2015";
                        else if (_shellVersion.StartsWith("15"))
                            _shellDisplayVersion = "2017";
                        else
                            _shellDisplayVersion = "2019";
                    }
                }

                return _shellDisplayVersion ?? "2013";
            }
        }

        public void OpenShellWindow(ShellWindows window)
        {
            this._hostPackage.JoinableTaskFactory.Run(async () =>
            {
                await this._hostPackage.JoinableTaskFactory.SwitchToMainThreadAsync();
                switch (window)
                {
                    case ShellWindows.Explorer:
                        _hostPackage.ShowExplorerWindow();
                        break;

                    case ShellWindows.Output:
                        break;
                }
            });
        }

        public void OpenInEditor(IAWSToolkitControl editorControl)
        {
            this._hostPackage.JoinableTaskFactory.Run(async () =>
            {
                await this._hostPackage.JoinableTaskFactory.SwitchToMainThreadAsync();
                var openShell = _hostPackage.GetVSShellService(typeof(IVsUIShellOpenDocument)) as IVsUIShellOpenDocument;

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

                    var documentMoniker = Path.Combine(_hostPackage.GetTempFileLocation(), filename);
                    IVsWindowFrame frame;

                    await _hostPackage.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var result = openShell.OpenSpecificEditor(0,  // grfOpenSpecific 
                                                                documentMoniker, // pszMkDocument 
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
                        editorControl.OnEditorOpened(false);
                    }
                    else
                    {
                        frame.Show();
                        editorControl.OnEditorOpened(true);
                    }
                }
                catch
                {
                    _hostPackage.ControlCache.Remove(controlId);
                    editorControl.OnEditorOpened(false);
                }
                
                ThreadPool.QueueUserWorkItem(_hostPackage.ClearDeadWeakReferences, null);
            });
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
            return this._hostPackage.JoinableTaskFactory.Run<bool>(async () =>
            {
                await this._hostPackage.JoinableTaskFactory.SwitchToMainThreadAsync();
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
            });
        }

        public void ShowError(string message)
        {
            ShowError("Error", message);
        }

        public void ShowError(string title, string message)
        {
            this.ExecuteOnUIThread(() =>
            {
                var ownerHandle = _hostPackage.GetParentWindowHandle();

                WindowsMessageBox.Show(ownerHandle, title, message,
                    MessageBoxButton.OK, MessageBoxImage.Error,
                    MessageBoxResult.OK);
            });
        }

        public void ShowErrorWithLinks(string title, string message)
        {
            this.ExecuteOnUIThread(() => Messaging.ShowErrorWithLinks(_hostPackage.GetParentWindow(), title, message));
        }

        public void ShowMessage(string title, string message)
        {
            this.ExecuteOnUIThread(() =>
            {
                var ownerHandle = _hostPackage.GetParentWindowHandle();

                WindowsMessageBox.Show(ownerHandle, title, message,
                    MessageBoxButton.OK, MessageBoxImage.Information,
                    MessageBoxResult.OK);
            });
        }

        public bool Confirm(string title, string message)
        {
            return Confirm(title, message, MessageBoxButton.YesNo);
        }

        public bool Confirm(string title, string message, MessageBoxButton buttons)
        {
            return this.ExecuteOnUIThread<bool>(() =>
            {
                var ownerHandle = _hostPackage.GetParentWindowHandle();

                var result = WindowsMessageBox.Show(ownerHandle, title, message,
                    buttons, MessageBoxImage.Exclamation,
                    MessageBoxResult.None);

                return result == MessageBoxResult.Yes || result == MessageBoxResult.OK;
            });
        }

        public void UpdateStatus(string status)
        {
            this._hostPackage.JoinableTaskFactory.Run(async () =>
            {
                await this._hostPackage.JoinableTaskFactory.SwitchToMainThreadAsync();
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
            });
        }

        public void ExecuteOnUIThread(Action action)
        {
            this._hostPackage.JoinableTaskFactory.Run(async delegate
            {
                await this._hostPackage.JoinableTaskFactory.SwitchToMainThreadAsync();
                action();
            });
        }

        public void BeginExecuteOnUIThread(Action action)
        {
            this._hostPackage.JoinableTaskFactory.RunAsync(async delegate
            {
                await this._hostPackage.JoinableTaskFactory.SwitchToMainThreadAsync();
                action();
            });
        }

        public T ExecuteOnUIThread<T>(Func<T> func)
        {
            Func<System.Threading.Tasks.Task<T>> taskFunc = async () =>
            {
                await this._hostPackage.JoinableTaskFactory.SwitchToMainThreadAsync();
                return func();
            };
            T t = this._hostPackage.JoinableTaskFactory.Run<T>(taskFunc);

            return t;
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

        public T QueryShellProviderService<T>() where T : class
        {
            var svc = this as T;
            if (svc != null)
                return svc;

            var hostSvc = _hostPackage as T;
            if (hostSvc != null)
                return hostSvc;

            // last gasp try and get from the VS shell
            return _hostPackage.GetVSShellService(typeof(T)) as T;
        }

        public object QueryAWSToolkitPluginService(Type pluginServiceType)
        {
            return ToolkitFactory.Instance.QueryPluginService(pluginServiceType);
        }

        public object QueryAWSToolkitPluginService(string pluginServiceType)
        {
            throw new NotImplementedException();
        }

        public void OpenInBrowser(string url, bool preferInternalBrowser)
        {
            this._hostPackage.JoinableTaskFactory.Run(async () =>
            {
                await this._hostPackage.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (preferInternalBrowser)
                {
                    var service = _hostPackage.GetVSShellService(typeof(SVsWebBrowsingService)) as IVsWebBrowsingService;

                    if (service != null)
                    {
                        __VSCREATEWEBBROWSER createFlags = __VSCREATEWEBBROWSER.VSCWB_AutoShow;
                        VSPREVIEWRESOLUTION resolution = VSPREVIEWRESOLUTION.PR_Default;
                        int result = ErrorHandler.CallWithCOMConvention(() =>
                        {
                            ThreadHelper.ThrowIfNotOnUIThread();
                            return service.CreateExternalWebBrowser((uint)createFlags, resolution, new Uri(url).AbsoluteUri);
                        });
                        if (ErrorHandler.Succeeded(result))
                            return;
                    }
                }

                // prefer not set, or internal service not available - launch the system
                // default browser in a separate process
                var u = new UriBuilder(url)
                {
                    Scheme = "https"
                };

                Process.Start(new ProcessStartInfo(u.Uri.ToString()));
            });
        }

        public void CloseEditor(IAWSToolkitControl editorControl)
        {
            var uniqueId = editorControl.UniqueId;

            // OpenInEditor can append account and region data to the control
            // key, so try and find a filename entry that starts with the key
            string filename = null;
            foreach (var k in _hostPackage.ControlUniqueNameToFileName.Keys)
            {
                if (k.StartsWith(uniqueId, StringComparison.OrdinalIgnoreCase))
                {
                    filename = _hostPackage.ControlUniqueNameToFileName[k];
                    break;
                }
            }

            if (!string.IsNullOrEmpty(filename))
            {
                CloseEditor(filename);
            }
        }

        public void CloseEditor(string fileName)
        {
            WeakReference editorControlReference = null;
            foreach (var k in _hostPackage.OpenedEditors.Keys)
            {
                if (k.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                {
                    editorControlReference = _hostPackage.OpenedEditors[k];
                    break;
                }
            }

            if (editorControlReference == null)
            {
                return;
            }

            if (editorControlReference.IsAlive)
            {
                var documentMoniker = Path.IsPathRooted(fileName) 
                    ? fileName 
                    : Path.Combine(_hostPackage.GetTempFileLocation(), fileName);

                IVsHierarchy ownerHier;
                uint itemid, cookie;
                var rdt = new RunningDocumentTable(_hostPackage);
                var docObj = rdt.FindDocument(documentMoniker, out ownerHier, out itemid, out cookie);
                if (docObj != null)
                {
                    rdt.CloseDocument(__FRAMECLOSE.FRAMECLOSE_NoSave, cookie);
                }
            }
        }

        #endregion

        private AWSToolkitShellProviderService() { }
    }
}

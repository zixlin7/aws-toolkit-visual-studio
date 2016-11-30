using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell;
using EnvDTE;

using ISysServiceProvider = System.IServiceProvider;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using VSStd97CmdID = Microsoft.VisualStudio.VSConstants.VSStd97CmdID;

using Amazon.AWSToolkit.CommonUI;

using log4net;
using Amazon.AWSToolkit.Shared;

namespace Amazon.AWSToolkit.VisualStudio.HostedEditor
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    [ComVisible(true)]
    public class HostedEditorPane : Microsoft.VisualStudio.Shell.WindowPane,
                                IVsPersistDocData2,     //to Enable persistence functionality for document data
                                IPersistFileFormat,     //to enable the programmatic loading or saving of an object in a format specified by the user.
                                IVsStatusbarUser,   //support updating the status bar
                                IExtensibleObject,  //so we can get the atuomation object
                                IEditor  //the automation interface for Editor
    {

        static readonly ILog LOGGER = LogManager.GetLogger(typeof(HostedEditorPane));
        public HostedEditorPane()
        {

        }

        public HostedEditorPane(IAWSToolkitControl hostedControl)
        {
            // Start the data to be load in the background
            hostedControl.ExecuteBackGroundLoadDataLoad();
            this._hostedControl = new HostedUserControl();
            this._hostedControl.AddHostedControl(hostedControl);

            var propertySupport = hostedControl as IPropertySupport;
            if (propertySupport != null)
            {
                propertySupport.OnPropertyChange += this.onPropertyChange;
            }

        }

        private const uint MyFormat = 0;
        private const string MyExtension = ".hostedControl";

        #region Fields
 
        private string fileName = string.Empty;
        private Timer FNFStatusbarTrigger = new Timer();
        private IExtensibleObjectSite extensibleObjectSite;

        #endregion

        #region "Window.Pane Overrides"


        private HostedUserControl _hostedControl;


        protected override void OnClose()
        {
            base.OnClose();
        }

        /// <summary>
        /// This is a required override from the Microsoft.VisualStudio.Shell.WindowPane class.
        /// It returns the extended rich text box that we host.
        /// </summary>
        public override IWin32Window Window
        {
            get
            {
                return this._hostedControl;
            }
        }
        #endregion



        /// <summary>
        /// returns the name of the file currently loaded
        /// </summary>
        public string FileName
        {
            get { return fileName; }
        }

        /// <summary>
        /// returns whether the contents of file have changed since the last save
        /// </summary>
        public bool DataChanged
        {
            get { return false; }
        }


        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1816:CallGCSuppressFinalizeCorrectly")]
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    // Dispose the timers
                    if (null != FNFStatusbarTrigger)
                    {
                        FNFStatusbarTrigger.Dispose();
                        FNFStatusbarTrigger = null;
                    }

                    if (_hostedControl != null)
                    {
                        _hostedControl.Dispose();
                        _hostedControl = null;
                    }
                    if (extensibleObjectSite != null)
                    {
                        extensibleObjectSite.NotifyDelete(this);
                        extensibleObjectSite = null;
                    }
                    GC.SuppressFinalize(this);
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }


        #region IExtensibleObject Implementation

        /// <summary>
        /// This function is used for Macro playback.  Whenever a macro gets played this funtion will be
        /// called and then the IEditor functions will be called on the object that ppDisp is set to.
        /// Since EditorPane implements IEditor we will just set it to "this".
        /// </summary>
        /// <param name="Name"> Passing in either null, empty string or "Document" will work.  Anything
        /// else will result in ppDisp being set to null.</param>
        /// <param name="pParent"> An object of type IExtensibleObjectSite.  We will keep a reference to this
        /// so that in the Dispose method we can call the NotifyDelete function.</param>
        /// <param name="ppDisp"> The object that this is set to will act as the automation object for macro
        /// playback.  In our case since IEditor is the automation interface and EditorPane
        /// implements it we will just be setting this parameter to "this".</param>
        public void GetAutomationObject(string Name, IExtensibleObjectSite pParent, out Object ppDisp)
        {
            // null or empty string just means the default object, but if a specific string
            // is specified, then make sure it's the correct one, but don't enforce case
            if (!string.IsNullOrEmpty(Name) && !Name.Equals("Document", StringComparison.CurrentCultureIgnoreCase))
            {
                ppDisp = null;
                return;
            }

            // Set the out value to this
            ppDisp = (IEditor)this;

            // Store the IExtensibleObjectSite object, it will be used in the Dispose method
            extensibleObjectSite = pParent;
        }

        #endregion

        public int GetClassID(out Guid pClassID)
        {
            ErrorHandler.ThrowOnFailure(((Microsoft.VisualStudio.OLE.Interop.IPersist)this).GetClassID(out pClassID));
            return VSConstants.S_OK;
        }

        #region IPersistFileFormat Members

        /// <summary>
        /// Notifies the object that it has concluded the Save transaction
        /// </summary>
        /// <param name="pszFilename">Pointer to the file name</param>
        /// <returns>S_OK if the funtion succeeds</returns>
        public int SaveCompleted(string pszFilename)
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Returns the path to the object's current working file 
        /// </summary>
        /// <param name="ppszFilename">Pointer to the file name</param>
        /// <param name="pnFormatIndex">Value that indicates the current format of the file as a zero based index
        /// into the list of formats. Since we support only a single format, we need to return zero. 
        /// Subsequently, we will return a single element in the format list through a call to GetFormatList.</param>
        /// <returns></returns>
        public int GetCurFile(out string ppszFilename, out uint pnFormatIndex)
        {
            // We only support 1 format so return its index
            pnFormatIndex = MyFormat;
            ppszFilename = fileName;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Initialization for the object 
        /// </summary>
        /// <param name="nFormatIndex">Zero based index into the list of formats that indicates the current format 
        /// of the file</param>
        /// <returns>S_OK if the method succeeds</returns>
        public int InitNew(uint nFormatIndex)
        {
            if (nFormatIndex != MyFormat)
            {
                return VSConstants.E_INVALIDARG;
            }
            // until someone change the file, we can consider it not dirty as
            // the user would be annoyed if we prompt him to save an empty file
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Provides the caller with the information necessary to open the standard common "Save As" dialog box. 
        /// This returns an enumeration of supported formats, from which the caller selects the appropriate format. 
        /// Each string for the format is terminated with a newline (\n) character. 
        /// The last string in the buffer must be terminated with the newline character as well. 
        /// The first string in each pair is a display string that describes the filter, such as "Text Only 
        /// (*.txt)". The second string specifies the filter pattern, such as "*.txt". To specify multiple filter 
        /// patterns for a single display string, use a semicolon to separate the patterns: "*.htm;*.html;*.asp". 
        /// A pattern string can be a combination of valid file name characters and the asterisk (*) wildcard character. 
        /// Do not include spaces in the pattern string. The following string is an example of a file pattern string: 
        /// "HTML File (*.htm; *.html; *.asp)\n*.htm;*.html;*.asp\nText File (*.txt)\n*.txt\n."
        /// </summary>
        /// <param name="ppszFormatList">Pointer to a string that contains pairs of format filter strings</param>
        /// <returns>S_OK if the method succeeds</returns>
        public int GetFormatList(out string ppszFormatList)
        {
            char Endline = (char)'\n';
            string FormatList = string.Format(CultureInfo.InvariantCulture, "My Editor (*{0}){1}*{0}{1}{1}", MyExtension, Endline);
            ppszFormatList = FormatList;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Loads the file content into the textbox
        /// </summary>
        /// <param name="pszFilename">Pointer to the full path name of the file to load</param>
        /// <param name="grfMode">file format mode</param>
        /// <param name="fReadOnly">determines if teh file should be opened as read only</param>
        /// <returns>S_OK if the method succeeds</returns>
        public int Load(string pszFilename, uint grfMode, int fReadOnly)
        {
            if (pszFilename == null)
            {
                return VSConstants.E_INVALIDARG;
            }

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Determines whether an object has changed since being saved to its current file
        /// </summary>
        /// <param name="pfIsDirty">true if the document has changed</param>
        /// <returns>S_OK if the method succeeds</returns>
        public int IsDirty(out int pfIsDirty)
        {
            pfIsDirty = VSConstants.S_FALSE;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Save the contents of the textbox into the specified file. If doing the save on the same file, we need to
        /// suspend notifications for file changes during the save operation.
        /// </summary>
        /// <param name="pszFilename">Pointer to the file name. If the pszFilename parameter is a null reference 
        /// we need to save using the current file
        /// </param>
        /// <param name="remember">Boolean value that indicates whether the pszFileName parameter is to be used 
        /// as the current working file.
        /// If remember != 0, pszFileName needs to be made the current file and the dirty flag needs to be cleared after the save.
        ///                   Also, file notifications need to be enabled for the new file and disabled for the old file 
        /// If remember == 0, this save operation is a Save a Copy As operation. In this case, 
        ///                   the current file is unchanged and dirty flag is not cleared
        /// </param>
        /// <param name="nFormatIndex">Zero based index into the list of formats that indicates the format in which 
        /// the file will be saved</param>
        /// <returns>S_OK if the method succeeds</returns>
        public int Save(string pszFilename, int fRemember, uint nFormatIndex)
        {
            return VSConstants.S_OK;
        }

        #endregion


        #region IVsPersistDocData Members

        public int IsDocDataReadOnly(out int pfReadOnly)
        {
            pfReadOnly = 0;
            return VSConstants.S_OK;
        }

        public int SetDocDataReadOnly([InAttribute] int fReadOnly)
        {
            return VSConstants.S_OK;
        }

        public int SetDocDataDirty([InAttribute] int fReadOnly)
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Used to determine if the document data has changed since the last time it was saved
        /// </summary>
        /// <param name="pfDirty">Will be set to 1 if the data has changed</param>
        /// <returns>S_OK if the function succeeds</returns>
        public int IsDocDataDirty(out int pfDirty)
        {
            return ((IPersistFileFormat)this).IsDirty(out pfDirty);
        }

        /// <summary>
        /// Saves the document data. Before actually saving the file, we first need to indicate to the environment
        /// that a file is about to be saved. This is done through the "SVsQueryEditQuerySave" service. We call the
        /// "QuerySaveFile" function on the service instance and then proceed depending on the result returned as follows:
        /// If result is QSR_SaveOK - We go ahead and save the file and the file is not read only at this point.
        /// If result is QSR_ForceSaveAs - We invoke the "Save As" functionality which will bring up the Save file name 
        ///                                dialog 
        /// If result is QSR_NoSave_Cancel - We cancel the save operation and indicate that the document could not be saved
        ///                                by setting the "pfSaveCanceled" flag
        /// If result is QSR_NoSave_Continue - Nothing to do here as the file need not be saved
        /// </summary>
        /// <param name="dwSave">Flags which specify the file save options:
        /// VSSAVE_Save        - Saves the current file to itself.
        /// VSSAVE_SaveAs      - Prompts the User for a filename and saves the file to the file specified.
        /// VSSAVE_SaveCopyAs  - Prompts the user for a filename and saves a copy of the file with a name specified.
        /// VSSAVE_SilentSave  - Saves the file without prompting for a name or confirmation.  
        /// </param>
        /// <param name="pbstrMkDocumentNew">Pointer to the path to the new document</param>
        /// <param name="pfSaveCanceled">value 1 if the document could not be saved</param>
        /// <returns></returns>
        public int SaveDocData(Microsoft.VisualStudio.Shell.Interop.VSSAVEFLAGS dwSave, out string pbstrMkDocumentNew, out int pfSaveCanceled)
        {
            pbstrMkDocumentNew = null;
            pfSaveCanceled = 0;
            int hr = VSConstants.S_OK;

            switch (dwSave)
            {
                case VSSAVEFLAGS.VSSAVE_Save:
                case VSSAVEFLAGS.VSSAVE_SilentSave:
                    {
                        IVsQueryEditQuerySave2 queryEditQuerySave = (IVsQueryEditQuerySave2)GetService(typeof(SVsQueryEditQuerySave));

                        // Call QueryEditQuerySave
                        uint result = 0;
                        hr = queryEditQuerySave.QuerySaveFile(
                                fileName,        // filename
                                0,    // flags
                                null,            // file attributes
                                out result);    // result
                        if (ErrorHandler.Failed(hr))
                            return hr;

                        // Process according to result from QuerySave
                        switch ((tagVSQuerySaveResult)result)
                        {
                            case tagVSQuerySaveResult.QSR_NoSave_Cancel:
                                // Note that this is also case tagVSQuerySaveResult.QSR_NoSave_UserCanceled because these
                                // two tags have the same value.
                                pfSaveCanceled = ~0;
                                break;

                            case tagVSQuerySaveResult.QSR_SaveOK:
                                {
                                    // Call the shell to do the save for us
                                    IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
                                    hr = uiShell.SaveDocDataToFile(dwSave, (IPersistFileFormat)this, fileName, out pbstrMkDocumentNew, out pfSaveCanceled);
                                    if (ErrorHandler.Failed(hr))
                                        return hr;
                                }
                                break;

                            case tagVSQuerySaveResult.QSR_ForceSaveAs:
                                {
                                    // Call the shell to do the SaveAS for us
                                    IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
                                    hr = uiShell.SaveDocDataToFile(VSSAVEFLAGS.VSSAVE_SaveAs, (IPersistFileFormat)this, fileName, out pbstrMkDocumentNew, out pfSaveCanceled);
                                    if (ErrorHandler.Failed(hr))
                                        return hr;
                                }
                                break;

                            case tagVSQuerySaveResult.QSR_NoSave_Continue:
                                // In this case there is nothing to do.
                                break;

                            default:
                                throw new NotSupportedException("Unsupported result from QEQS");
                        }
                        break;
                    }
                case VSSAVEFLAGS.VSSAVE_SaveAs:
                case VSSAVEFLAGS.VSSAVE_SaveCopyAs:
                    {
                        // Make sure the file name as the right extension
                        if (String.Compare(MyExtension, System.IO.Path.GetExtension(fileName), true, CultureInfo.CurrentCulture) != 0)
                        {
                            fileName += MyExtension;
                        }
                        // Call the shell to do the save for us
                        IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
                        hr = uiShell.SaveDocDataToFile(dwSave, (IPersistFileFormat)this, fileName, out pbstrMkDocumentNew, out pfSaveCanceled);
                        if (ErrorHandler.Failed(hr))
                            return hr;
                        break;
                    }
                default:
                    throw new ArgumentException("Unsupported Save flag");
            };

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Loads the document data from the file specified
        /// </summary>
        /// <param name="pszMkDocument">Path to the document file which needs to be loaded</param>
        /// <returns>S_Ok if the method succeeds</returns>
        public int LoadDocData(string pszMkDocument)
        {
            return ((IPersistFileFormat)this).Load(pszMkDocument, 0, 0);
        }

        /// <summary>
        /// Used to set the initial name for unsaved, newly created document data
        /// </summary>
        /// <param name="pszDocDataPath">String containing the path to the document. We need to ignore this parameter
        /// </param>
        /// <returns>S_OK if the mthod succeeds</returns>
        public int SetUntitledDocPath(string pszDocDataPath)
        {
            return ((IPersistFileFormat)this).InitNew(MyFormat);
        }

        /// <summary>
        /// Returns the Guid of the editor factory that created the IVsPersistDocData object
        /// </summary>
        /// <param name="pClassID">Pointer to the class identifier of the editor type</param>
        /// <returns>S_OK if the method succeeds</returns>
        public int GetGuidEditorType(out Guid pClassID)
        {
            pClassID = new Guid(GuidList.HostedEditorFactoryGuidString);
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Close the IVsPersistDocData object
        /// </summary>
        /// <returns>S_OK if the function succeeds</returns>
        public int Close()
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Determines if it is possible to reload the document data
        /// </summary>
        /// <param name="pfReloadable">set to 1 if the document can be reloaded</param>
        /// <returns>S_OK if the method succeeds</returns>
        public int IsDocDataReloadable(out int pfReloadable)
        {
            // Allow file to be reloaded
            pfReloadable = 1;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Renames the document data
        /// </summary>
        /// <param name="grfAttribs"></param>
        /// <param name="pHierNew"></param>
        /// <param name="itemidNew"></param>
        /// <param name="pszMkDocumentNew"></param>
        /// <returns></returns>
        public int RenameDocData(uint grfAttribs, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
        {
            // TODO:  Add EditorPane.RenameDocData implementation
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Reloads the document data
        /// </summary>
        /// <param name="grfFlags">Flag indicating whether to ignore the next file change when reloading the document data.
        /// This flag should not be set for us since we implement the "IVsDocDataFileChangeControl" interface in order to 
        /// indicate ignoring of file changes
        /// </param>
        /// <returns>S_OK if the mthod succeeds</returns>
        public int ReloadDocData(uint grfFlags)
        {
            return ((IPersistFileFormat)this).Load(fileName, grfFlags, 0);
        }

        /// <summary>
        /// Called by the Running Document Table when it registers the document data. 
        /// </summary>
        /// <param name="docCookie">Handle for the document to be registered</param>
        /// <param name="pHierNew">Pointer to the IVsHierarchy interface</param>
        /// <param name="itemidNew">Item identifier of the document to be registered from VSITEM</param>
        /// <returns></returns>
        public int OnRegisterDocData(uint docCookie, IVsHierarchy pHierNew, uint itemidNew)
        {
            //Nothing to do here
            return VSConstants.S_OK;
        }

        #endregion



        #region IVsStatusbarUser Members

        /// <summary>
        /// This is the IVsStatusBarUser function that will update our status bar.
        /// Note that the IDE calls this function only when our document window is
        /// initially activated.
        /// </summary>
        /// <returns> Hresult that represents success or failure.</returns>
        int IVsStatusbarUser.SetInfo()
        {
            // Call the helper function that updates the status bar insert mode
            int hrSetInsertMode = SetStatusBarInsertMode();

            // Call the helper function that updates the status bar selection mode
            int hrSetSelectionMode = SetStatusBarSelectionMode();

            // Call the helper function that updates the status bar position
            int hrSetPosition = SetStatusBarPosition();

            return (hrSetInsertMode == VSConstants.S_OK &&
                    hrSetSelectionMode == VSConstants.S_OK &&
                    hrSetPosition == VSConstants.S_OK) ? VSConstants.S_OK : VSConstants.E_FAIL;
        }

        /// <summary>
        /// Helper function that updates the insert mode displayed on the status bar.
        /// This is the text that is displayed in the right side of the status bar that
        /// will either say INS or OVR.
        /// </summary>
        /// <returns> Hresult that represents success or failure.</returns>
        int SetStatusBarInsertMode()
        {
            // Get the IVsStatusBar interface
            IVsStatusbar statusBar = GetService(typeof(SVsStatusbar)) as IVsStatusbar;
            if (statusBar == null)
                return VSConstants.E_FAIL;

            // Set the insert mode based on our editorControl.richTextBoxCtrl.Overstrike value.  If 1 is passed
            // in then it will display OVR and if 0 is passed in it will display INS.
            object insertMode = 0;
            return statusBar.SetInsMode(ref insertMode);
        }

        /// <summary>
        /// This is an extra command handler that we will use to check when the insert
        /// key is pressed.  Note that even if we detect that the insert key is pressed
        /// we are not setting the handled property to true, so other event handlers will
        /// also see it.
        /// </summary>
        /// <param name="sender"> Not used.</param>
        /// <param name="e"> KeyEventArgs instance that we will use to get the key that was pressed.</param>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
        }

        /// <summary>
        /// Helper function that updates the selection mode displayed on the status
        /// bar.  Right now we only support stream selection.
        /// </summary>
        /// <returns> Hresult that represents success or failure.</returns>
        int SetStatusBarSelectionMode()
        {
            // Get the IVsStatusBar interface.
            IVsStatusbar statusBar = GetService(typeof(SVsStatusbar)) as IVsStatusbar;
            if (statusBar == null)
                return VSConstants.E_FAIL;

            // Set the selection mode.  Since we only support stream selection we will
            // always pass in zero here.  Passing in one would make "COL" show up
            // just to the left of the insert mode on the status bar.
            object selectionMode = 0;
            return statusBar.SetSelMode(ref selectionMode);
        }

        /// <summary>
        /// Helper function that updates the cursor position displayed on the status bar.
        /// </summary>
        /// <returns> Hresult that represents success or failure.</returns>
        int SetStatusBarPosition()
        {
            return 0;
        }

        #endregion

        private ITrackSelection _trackSel;
        private ITrackSelection TrackSelection
        {
            get
            {
                if (this._trackSel == null)
                {
                    this._trackSel = GetService(typeof(STrackSelection)) as ITrackSelection;
                }

                return _trackSel;
            }
        }

        void onPropertyChange(object sender, bool forceShow, IList propertyObjects)
        {
            try
            {
                var tracker = this.TrackSelection;
                if (tracker != null)
                {
                    var selContainer = new Microsoft.VisualStudio.Shell.SelectionContainer(true, false);
                    selContainer.SelectableObjects = propertyObjects;
                    selContainer.SelectedObjects = propertyObjects;
                    tracker.OnSelectChange((ISelectionContainer)selContainer);

                    if (forceShow)
                    {
                        var shell = GetService(typeof(SVsUIShell)) as IVsUIShell;
                        if (shell != null)
                        {
                            var guidPropertyBrowser = new
                                Guid(ToolWindowGuids.PropertyBrowser);

                            IVsWindowFrame frame = null;
                            shell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate,
                                ref guidPropertyBrowser, out frame);

                            if (frame != null)
                            {
                                frame.Show();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error displaying properties window", e);
            }
        }
    }
}

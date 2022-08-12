﻿using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Amazon.AWSToolkit.VisualStudio.ToolWindow
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    ///
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane, 
    /// usually implemented by the package implementer.
    ///
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its 
    /// implementation of the IVsUIElementPane interface.
    /// </summary>
    [Guid(GuidList.AWSExplorerToolWindowGuidString)]
    public class AWSNavigatorToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Constructor
        /// Undocumented -- by passing at least one object through the constructor, Visual Studio
        /// will be able to asynchronously load the ToolWindow.
        /// Parameter is instantiated in AWSToolkitPackage.InitializeToolWindowAsync
        /// </summary>
        /// <example>https://github.com/microsoft/VSSDK-Extensibility-Samples/tree/master/AsyncToolWindow</example>
        public AWSNavigatorToolWindow(AWSNavigatorToolWindowState state) : base()
        {
            // Set the window title reading it from the resources.
            this.Caption = Resources.AWSExplorerToolWindowTitle;
            // Set the image that will appear on the tab of the window frame
            // when docked with an other window
            // The resource ID correspond to the one defined in the resx file
            // while the Index is the offset in the bitmap strip. Each image in
            // the strip being 16x16.
            this.BitmapResourceID = 301;
            this.BitmapIndex = 0;

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on 
            // the object returned by the Content property.
            base.Content = new AWSNavigatorToolControl();
        }
    }

    /// <summary>
    /// Placeholder object used to enable async instantiation of AWSNavigatorToolWindow.
    /// See AWSNavigatorToolWindow constructor comments for details.
    /// </summary>
    public class AWSNavigatorToolWindowState
    {
    }
}

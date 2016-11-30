using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Shell;

using log4net;

namespace Amazon.AWSToolkit.CloudFormation.EditorExtensions
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType(TemplateContentType.ContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class TemplateViewCreationListener : IWpfTextViewCreationListener
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(TemplateViewCreationListener));

        [Import(typeof(SVsServiceProvider))]
        private IServiceProvider ServiceProvider { get; set; }

        public void TextViewCreated(IWpfTextView textView)
        {
            string filePath;
            IErrorListReporter errorListProvider = CreateErrorListReporter(textView, out filePath);

            // Attempt to change the encoding to remove the BOM. If the BOM exist the file can't 
            // not be used directly in the CloudFormation web console.
            try
            {
                ITextDocument property;
                textView.TextDataModel.DocumentBuffer.Properties.TryGetProperty<ITextDocument>((object)typeof(ITextDocument), out property);
                property.Encoding = new UTF8Encoding(false);
            }
            catch { }

            textView.TextBuffer.Properties[typeof(IErrorListReporter)] = errorListProvider;
            textView.TextBuffer.Properties[EditorContants.FILE_PATH_PROPERTY_NAME] = filePath;
            textView.Closed += new EventHandler(textView_Closed);

            ThemeUtil.ThemeChange += (EventHandler)((s, e) =>
            {
                try
                {
                    Microsoft.VisualStudio.Shell.Interop.IVsUIHierarchy uiHierarchy;
                    uint itemID;
                    Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame windowFrame;
                    if (Microsoft.VisualStudio.Shell.VsShellUtilities.IsDocumentOpen(ServiceProvider, filePath, Guid.Empty,
                                                    out uiHierarchy, out itemID, out windowFrame))
                    {
                        // Get the IVsTextView from the windowFrame.
                        var tv = Microsoft.VisualStudio.Shell.VsShellUtilities.GetTextView(windowFrame);

                        Microsoft.VisualStudio.TextManager.Interop.IVsTextLines buffer;
                        if (tv.GetBuffer(out buffer) == VSConstants.S_OK && buffer != null)
                        {
                            buffer.Reload(1);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LOGGER.Warn("Failed to respond to theme change", ex);
                }
            });
        }

        void textView_Closed(object sender, EventArgs e)
        {
            IWpfTextView textView = sender as IWpfTextView;
            if (textView.TextBuffer.Properties.ContainsProperty(typeof(IErrorListReporter)))
            {
                var errorListProvider = textView.TextBuffer.Properties.GetProperty<IErrorListReporter>(typeof(IErrorListReporter));

                // If this reporter was created as part of opening the file and not reusing the project reporter then clear
                // out all the task when the file is closed.
                if(errorListProvider is ErrorListReporter)
                    errorListProvider.Tasks.Clear();
            }
        }

        IErrorListReporter CreateErrorListReporter(IWpfTextView textView, out string filePath)
        {
            filePath = GetFilePath(textView);
            if (string.IsNullOrEmpty(filePath))
                return new ErrorListReporter(this.ServiceProvider);

            var dte = ServiceProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            if(dte == null)
                return new ErrorListReporter(this.ServiceProvider);

            var projectItem = dte.Solution.FindProjectItem(filePath);
            if(projectItem == null)
                return new ErrorListReporter(this.ServiceProvider);

            var project = projectItem.ContainingProject;
            if (project == null || !(project.Object is IErrorListReporter))
                return new ErrorListReporter(this.ServiceProvider);

            return project.Object as IErrorListReporter;
        }

        /// <summary>
        /// This error list provider is used when the template if not contained within a CloudFormation project.
        /// Otherwise we will use the one from the project node so that we don't create duplicate errors with MSBuild
        /// </summary>
        static string GetFilePath(Microsoft.VisualStudio.Text.Editor.IWpfTextView wpfTextView)
        {
            Microsoft.VisualStudio.Text.ITextDocument document;
            if ((wpfTextView == null) ||
                    (!wpfTextView.TextDataModel.DocumentBuffer.Properties.TryGetProperty(typeof(Microsoft.VisualStudio.Text.ITextDocument), out document)))
                return String.Empty;

            // If we have no document, just ignore it.
            if ((document == null) || (document.TextBuffer == null))
                return String.Empty;

            return document.FilePath;
        }

        internal class ErrorListReporter : IErrorListReporter
        {
            ErrorListProvider errorListProvider;

            internal ErrorListReporter(IServiceProvider serviceProvider)
            {
                errorListProvider = new ErrorListProvider(serviceProvider);
                errorListProvider.ProviderGuid = Guid.Parse("{3D48F5B6-64DF-474B-943D-F07F3BC5504F}");
                errorListProvider.ProviderName = "CloudFormation Template Validation";
            }

            public TaskProvider.TaskCollection Tasks
            {
                get { return errorListProvider.Tasks; }
            }

            public bool Navigate(Microsoft.VisualStudio.Shell.Task task, Guid logicalView)
            {
                return this.errorListProvider.Navigate(task, logicalView);
            }

            public void ResumeRefresh()
            {
                this.errorListProvider.ResumeRefresh();
            }

            public void SuspendRefresh()
            {
                this.errorListProvider.SuspendRefresh();
            }
        }
    }
}

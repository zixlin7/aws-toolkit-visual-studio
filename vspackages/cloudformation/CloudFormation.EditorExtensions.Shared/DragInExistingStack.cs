using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Editor.DragDrop;

using log4net;


namespace Amazon.AWSToolkit.CloudFormation.EditorExtensions
{
    internal class DragInExistingStackHandler : IDropHandler
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(DragInExistingStackHandler));

        IWpfTextView _textView;

        internal DragInExistingStackHandler(IWpfTextView textView)
        {
            this._textView = textView;
        }
    
        public DragDropPointerEffects  HandleDataDropped(DragDropInfo dragDropInfo)
        {
            try
            {
                if (!ToolkitFactory.Instance.ShellProvider.Confirm("Replace Template", "Replace the contents of this template with the template from this existing stack?"))
                    return DragDropPointerEffects.None;

                var getTemplate = dragDropInfo.Data.GetData(ToolkitGlobalConstants.CloudFormationStackTemplateFetcherDnDFormat) as Func<string>;
                if(getTemplate == null)
                    return DragDropPointerEffects.None;

                var template = getTemplate();
                var edit = this._textView.TextBuffer.CreateEdit();
                edit.Replace(0, this._textView.TextSnapshot.Length, template);
                edit.Apply();

                return DragDropPointerEffects.Move;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error DnD cloudformation stack", e);
                return DragDropPointerEffects.None;
            }
        }

        public void  HandleDragCanceled()
        {
        }

        public DragDropPointerEffects  HandleDragStarted(DragDropInfo dragDropInfo)
        {
            return DragDropPointerEffects.Move;
        }

        public DragDropPointerEffects  HandleDraggingOver(DragDropInfo dragDropInfo)
        {
            return DragDropPointerEffects.Move;
        }

        public bool  IsDropEnabled(DragDropInfo dragDropInfo)
        {
            if (!dragDropInfo.Data.GetDataPresent(ToolkitGlobalConstants.CloudFormationStackTemplateFetcherDnDFormat))
                return false;

            var getTemplate = dragDropInfo.Data.GetData(ToolkitGlobalConstants.CloudFormationStackTemplateFetcherDnDFormat) as Func<string>;
            if (getTemplate == null)
                return false;

 	        return true;
        }
    }

    [Export(typeof(IDropHandlerProvider))]
    [DropFormat(ToolkitGlobalConstants.CloudFormationStackTemplateFetcherDnDFormat)]
    [Name("CloudFormation Stack Drop Handler")]
    [Order(Before = "DefaultFileDropHandler")]
    internal class DragInExistingStackProvider : IDropHandlerProvider
    {
        public IDropHandler GetAssociatedDropHandler(IWpfTextView wpfTextView)
        {
            return wpfTextView.Properties.GetOrCreateSingletonProperty<DragInExistingStackHandler>(() => new  DragInExistingStackHandler(wpfTextView));
        }
    }
}

using System;
using System.Runtime.InteropServices;
using EnvDTE;
using tom;

namespace Amazon.AWSToolkit.VisualStudio
{

    /// <summary>
    /// IEditor is the automation interface for EditorDocument.
    /// The implementation of the methods is just a wrapper over the rich
    /// edit control's object model.
    /// </summary>
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IEditor
    {
    }
}

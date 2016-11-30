using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;

namespace Amazon.AWSToolkit.Navigator.Node
{
    public interface IViewModel : INotifyPropertyChanged
    {
        string Name { get; }
        Stream Icon { get; }
        string ToolTip { get; }

        IViewModel Parent { get; }
        ObservableCollection<IViewModel> Children { get; }
        IMetaNode MetaNode { get; }
        T FindSingleChild<T>(bool recursive) where T : IViewModel;
        T FindSingleChild<T>(bool recursive, Predicate<T> func) where T : IViewModel;
        T FindAncestor<T>() where T : class, IViewModel;

        void LoadDnDObjects(IDataObject dndDataObjects);
        void Refresh(bool async);

        bool FailedToLoadChildren { get; }

        IList<ActionHandlerWrapper> GetVisibleActions();
    }
}

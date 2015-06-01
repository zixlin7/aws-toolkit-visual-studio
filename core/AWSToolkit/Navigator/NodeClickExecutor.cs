using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.Navigator
{
    internal class NodeClickExecutor
    {
        IViewModel _selectedNode;
        ActionHandlerWrapper _handler;

        internal NodeClickExecutor(IViewModel viewModel, ActionHandlerWrapper handler)
        {
            this._selectedNode = viewModel;
            this._handler = handler;
        }

        internal NodeClickExecutor(NavigatorControl navigator, ActionHandlerWrapper handler)
        {
            this._selectedNode = navigator.SelectedNode;
            this._handler = handler;
        }

        public void OnClick(object sender, RoutedEventArgs e)
        {
            if (this._handler.Handler == null)
                return;

            ActionResults results = this._handler.Handler(this._selectedNode);
            if (results.Success && this._handler.ResponseHandler != null)
                this._handler.ResponseHandler(this._selectedNode, results);
        }
    }
}

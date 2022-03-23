using System.Collections.Generic;
using System.Linq;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Models;

using CommonUI.Models;

using Microsoft.VisualStudio.PlatformUI;

namespace AwsToolkit.VsSdk.Common.CommonUI
{
    public partial class KeyValueEditorDialog : DialogWindow, IKeyValueEditorDialog
    {
        private readonly ToolkitContext _toolkitContext;
        private readonly KeyValueEditorViewModel _viewModel;

        public IEnumerable<KeyValue> KeyValues { get; set; }

        public KeyValueEditorDialog(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;

            _viewModel = CreateViewModel();

            InitializeComponent();
            DataContext = _viewModel;
        }

        private KeyValueEditorViewModel CreateViewModel()
        {
            var viewModel = new KeyValueEditorViewModel();
            viewModel.OkCommand = new RelayCommand(
                _ => !_viewModel.KeyValues.HasErrors,
                _ => DialogResult = true);

            return viewModel;
        }

        public new bool Show()
        {
            // Necessary to clone KeyValues coming in so if dialog is cancelled, existing settings aren't mutated
            _viewModel.Title = Title;
            _viewModel.SetKeyValues(KeyValues.Select(kv => new KeyValue(kv)));

            var result = ShowModal() ?? false;

            if (result)
            {
                KeyValues = _viewModel.GetValidKeyValues().Select(kv => new KeyValue(kv));
            }

            return result;
        }
    }
}

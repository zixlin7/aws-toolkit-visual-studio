using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Models;
using Amazon.AWSToolkit.ViewModels;

namespace CommonUI.Models
{
    public enum KeyValueEditorMode
    {
        ListEditor,
        TextEditor,
    }

    public class KeyValueEditorViewModel : BaseModel
    {
        public KeyValuesViewModel KeyValues { get; }

        private ICommand _okCommand;

        public ICommand OkCommand
        {
            get => _okCommand;
            set => SetProperty(ref _okCommand, value);
        }

        private string _title;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private KeyValueEditorMode _keyValueEditorMode = KeyValueEditorMode.ListEditor;

        public KeyValueEditorMode EditorMode
        {
            get => _keyValueEditorMode;
            set => SetProperty(ref _keyValueEditorMode, value);
        }

        public ICommand UseListMode { get; }
        public ICommand UseTextMode { get; }

        public KeyValueEditorViewModel()
        {
            KeyValues = new KeyValuesViewModel();
            UseListMode = new RelayCommand(param => EditorMode = KeyValueEditorMode.ListEditor);
            UseTextMode = new RelayCommand(param => EditorMode = KeyValueEditorMode.TextEditor);
        }

        public void SetKeyValues(ICollection<KeyValue> keyValues)
        {
            KeyValues.KeyValues = new ObservableCollection<KeyValue>(keyValues);
        }
    }
}

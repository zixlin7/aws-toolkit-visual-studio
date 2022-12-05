using System.Windows.Input;
using System.Windows.Media;

using Amazon.AWSToolkit.CommonUI;

namespace CodeCatalyst.ViewModels
{
    /// <summary>
    /// Represents the (AWS Builder ID) connection status for a CodeCatalyst dialog
    /// </summary>
    internal class CodeCatalystConnectionStatus : BaseModel
    {
        /// <summary>
        /// Image associated with status
        /// </summary>
        public ImageSource Image
        {
            get => _image;
            set => SetProperty(ref _image, value);
        }

        /// <summary>
        /// Status text
        /// </summary>
        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        /// <summary>
        /// (Optional) Command associated with this status
        /// </summary>
        public ICommand Command
        {
            get => _command;
            set => SetProperty(ref _command, value);
        }

        /// <summary>
        /// Display name shown in UI for <see cref="Command"/>
        /// </summary>
        public string CommandText
        {
            get => _commandText;
            set => SetProperty(ref _commandText, value);
        }

        private ImageSource _image;
        private string _text;
        private ICommand _command;
        private string _commandText;
    }
}

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Tasks;

namespace Amazon.AWSToolkit.Publish.Commands
{
    /// <summary>
    /// ICommand wrapper enabling WPF command binding for Publish Document Panel Footer's Commands/Buttons
    /// </summary>
    public abstract class PublishFooterCommand : ICommand, IDisposable
    {
        public event EventHandler CanExecuteChanged;
        protected readonly PublishToAwsDocumentViewModel PublishDocumentViewModel;

        protected PublishFooterCommand(PublishToAwsDocumentViewModel publishDocumentViewModel)
        {
            PublishDocumentViewModel = publishDocumentViewModel;
            PublishDocumentViewModel.PropertyChanged += PublishDocumentViewModelOnPropertyChanged;
        }

        public abstract bool CanExecute(object parameter);

        public void Execute(object parameter)
        {
            ExecuteAsync(parameter).LogExceptionAndForget();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            PublishDocumentViewModel.PropertyChanged -= PublishDocumentViewModelOnPropertyChanged;
        }

        /// <summary>
        /// Defines action to be executed for derived commands (for eg. Target View Command, Back to Target command)
        /// </summary>
        protected abstract Task ExecuteCommandAsync();

        public async Task ExecuteAsync(object parameter)
        {
            if (CanExecute(parameter))
            {
                await ExecuteCommandAsync();
            }

            await PublishDocumentViewModel.JoinableTaskFactory.SwitchToMainThreadAsync();
            RaiseCanExecuteChanged();
        }

        private void PublishDocumentViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Re-evaluate if the command can be invoked
            RaiseCanExecuteChanged();
        }
    }
}

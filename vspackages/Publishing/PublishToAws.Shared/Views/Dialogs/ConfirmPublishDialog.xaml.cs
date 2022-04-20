using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;

using Microsoft.VisualStudio.PlatformUI;

namespace Amazon.AWSToolkit.Publish.Views.Dialogs
{
    public partial class ConfirmPublishDialog : DialogWindow, INotifyPropertyChanged, IConfirmPublishDialog
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand AcceptCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        public string ProjectName { get; set; }
        public string PublishDestinationName { get; set; }
        public string RegionName { get; set; }
        public string CredentialsId { get; set; }

        public bool SilenceFutureConfirmations
        {
            get => _silenceFutureConfirmations;
            set
            {
                _silenceFutureConfirmations = value;
                RaisePropertyChanged(nameof(SilenceFutureConfirmations));
            } 
        }

        private bool _silenceFutureConfirmations = false;

        public ConfirmPublishDialog()
        {
            AcceptCommand = new RelayCommand(_ => DialogResult = true);
            CancelCommand = new RelayCommand(_ => DialogResult = false);

            InitializeComponent();
            DataContext = this;
        }

        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

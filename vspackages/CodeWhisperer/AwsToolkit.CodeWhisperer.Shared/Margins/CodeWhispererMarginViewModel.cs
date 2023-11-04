using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AwsToolkit.CodeWhisperer.Commands;
using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Documents;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
using Amazon.AwsToolkit.VsSdk.Common.Commands;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Context;

using log4net;

namespace Amazon.AwsToolkit.CodeWhisperer.Margins
{
    public class CodeWhispererMarginViewModel : BaseModel, IDisposable
    {
        private const string _generateSuggestionsCommandName = "AWSToolkit.CodeWhisperer.GetSuggestion";

        private static readonly ILog _logger = LogManager.GetLogger(typeof(CodeWhispererMarginViewModel));
        private readonly IToolkitContextProvider _toolkitContextProvider;
        private readonly ToolkitJoinableTaskFactoryProvider _taskFactoryProvider;
        private readonly ICodeWhispererManager _manager;
        private readonly IVsCommandRepository _commandRepository;
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private bool _isAutoSuggestPaused = false;
        private ConnectionStatus _connectionState = ConnectionStatus.Disconnected;

        public CodeWhispererMarginViewModel(
            ICodeWhispererTextView textView,
            ICodeWhispererManager manager,
            ISuggestionUiManager suggestionUiManager,
            IVsCommandRepository commandRepository,
            IToolkitContextProvider toolkitContextProvider,
            ToolkitJoinableTaskFactoryProvider taskFactoryProvider)
        {
            _manager = manager;
            _commandRepository = commandRepository;
            _toolkitContextProvider = toolkitContextProvider;
            _taskFactoryProvider = taskFactoryProvider;

            SignIn = new SignInCommand(_manager, _toolkitContextProvider);
            SignOut = new SignOutCommand(_manager, _toolkitContextProvider);

            ViewOptions = new ViewOptionsCommand(_toolkitContextProvider);

            var pauseCommand = new PauseCommand(_manager, _toolkitContextProvider);
            _disposables.Add(pauseCommand);
            Pause = pauseCommand;

            var resumeCommand = new ResumeCommand(_manager, _toolkitContextProvider);
            _disposables.Add(resumeCommand);
            Resume = resumeCommand;

            ViewUserGuide = new ViewUserGuideCommand(_toolkitContextProvider);
            GettingStarted = new GettingStartedCommand(_toolkitContextProvider);

            GenerateSuggestions = new GetSuggestionsCommand(textView, _manager, suggestionUiManager, _toolkitContextProvider);

            ViewCodeReferences = new ViewCodeReferencesCommand(_manager, _toolkitContextProvider);
            SecurityScan = new SecurityScanCommand(_toolkitContextProvider);

            _isAutoSuggestPaused = _manager.IsAutoSuggestPaused();
            _connectionState = _manager.ConnectionStatus;
            _manager.PauseAutoSuggestChanged += OnManagerPauseAutoSuggestChanged;
            _manager.ConnectionStatusChanged += OnManagerConnectionStatusChanged;

            UpdateKeyBindings();
            UpdateMarginStatus();
        }

        public void UpdateKeyBindings()
        {
            GenerateSuggestionsKeyBinding = _taskFactoryProvider.JoinableTaskFactory.Run(async () =>
                await GetKeyBindingAsync(_generateSuggestionsCommandName));
        }

        private async Task<string> GetKeyBindingAsync(string commandName)
        {
            try
            {
                return await _commandRepository.GetCommandBindingAsync(commandName);
            }
            catch (Exception e)
            {
                _logger.Error($"Error getting key binding for command {commandName}", e);
                return string.Empty;
            }
        }

        private ICommand _signIn;
        public ICommand SignIn
        {
            get => _signIn;
            set => SetProperty(ref _signIn, value);
        }

        private ICommand _signOut;
        public ICommand SignOut
        {
            get => _signOut;
            set => SetProperty(ref _signOut, value);
        }

        private ICommand _generateSuggestions;
        public ICommand GenerateSuggestions
        {
            get => _generateSuggestions;
            set => SetProperty(ref _generateSuggestions, value);
        }

        private ICommand _pause;
        public ICommand Pause
        {
            get => _pause;
            set => SetProperty(ref _pause, value);
        }

        private ICommand _resume;
        public ICommand Resume
        {
            get => _resume;
            set => SetProperty(ref _resume, value);
        }

        private ICommand _viewOptions;
        public ICommand ViewOptions
        {
            get => _viewOptions;
            set => SetProperty(ref _viewOptions, value);
        }

        private ICommand _viewCodeReferences;
        public ICommand ViewCodeReferences
        {
            get => _viewCodeReferences;
            set => SetProperty(ref _viewCodeReferences, value);
        }

        private ICommand _securityScan;
        public ICommand SecurityScan
        {
            get => _securityScan;
            set => SetProperty(ref _securityScan, value);
        }

        private ICommand _viewUserGuide;
        public ICommand ViewUserGuide
        {
            get => _viewUserGuide;
            set => SetProperty(ref _viewUserGuide, value);
        }

        private ICommand _gettingStarted;
        public ICommand GettingStarted
        {
            get => _gettingStarted;
            set => SetProperty(ref _gettingStarted, value);
        }

        private string _generateSuggestionsKeyBinding;
        public string GenerateSuggestionsKeyBinding
        {
            get => _generateSuggestionsKeyBinding;
            set => SetProperty(ref _generateSuggestionsKeyBinding, value);
        }

        private MarginStatus _status = MarginStatus.Disconnected;
        public MarginStatus MarginStatus
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public void Dispose()
        {
            _manager.PauseAutoSuggestChanged -= OnManagerPauseAutoSuggestChanged;
            _manager.ConnectionStatusChanged -= OnManagerConnectionStatusChanged;

            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
        }

        private void OnManagerPauseAutoSuggestChanged(object sender, PauseStateChangedEventArgs e)
        {
            _isAutoSuggestPaused = e.IsPaused;
            UpdateMarginStatus();
        }

        private void OnManagerConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs e)
        {
            _connectionState = e.ConnectionStatus;
            UpdateMarginStatus();
        }

        private void UpdateMarginStatus()
        {
            switch (_connectionState)
            {
                // todo : Error
                case ConnectionStatus.Disconnected:
                case ConnectionStatus.Expired:
                    MarginStatus = MarginStatus.Disconnected;
                    break;
                case ConnectionStatus.Connected:
                    MarginStatus = _isAutoSuggestPaused ? MarginStatus.ConnectedPaused : MarginStatus.Connected;
                    break;
                default:
                    break;
            }
        }
    }
}

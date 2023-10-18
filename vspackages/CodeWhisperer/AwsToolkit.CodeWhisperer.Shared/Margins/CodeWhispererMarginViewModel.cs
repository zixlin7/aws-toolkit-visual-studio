using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

using Amazon.AwsToolkit.CodeWhisperer.Commands;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard;
using Amazon.AWSToolkit.Context;
using Amazon.AwsToolkit.VsSdk.Common.Commands;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;

using EnvDTE;

using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Install;
using log4net;

namespace Amazon.AwsToolkit.CodeWhisperer.Margins
{
    public class CodeWhispererMarginViewModel : BaseModel, IDisposable
    {
        private const string _generateSuggestionsCommandName = "AWSToolkit.CodeWhisperer.GetSuggestion";

        private static readonly ILog _logger = LogManager.GetLogger(typeof(CodeWhispererMarginViewModel));
        private readonly IToolkitContextProvider _toolkitContextProvider;
        private readonly ToolkitJoinableTaskFactoryProvider _taskFactoryProvider;
        private readonly IWpfTextView _textView;
        private readonly ICodeWhispererManager _manager;
        private readonly SVsServiceProvider _serviceProvider;
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public CodeWhispererMarginViewModel(IWpfTextView textView, ICodeWhispererManager manager,
            ISuggestionUiManager suggestionUiManager,
            SVsServiceProvider serviceProvider,
            IToolkitContextProvider toolkitContextProvider,
            ToolkitJoinableTaskFactoryProvider taskFactoryProvider)
        {
            _textView = textView;
            _manager = manager;
            _serviceProvider = serviceProvider;
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

            GenerateSuggestions = new GetSuggestionsCommand(_textView, _manager, suggestionUiManager, _toolkitContextProvider);
            GenerateSuggestionsKeyBinding = GetKeyBindingFrom(_generateSuggestionsCommandName);

            ViewCodeReferences = new ViewCodeReferencesCommand(_manager, _toolkitContextProvider);
            SecurityScan = new SecurityScanCommand(_toolkitContextProvider);
        }

        public void UpdateKeyBindings()
        {
            GenerateSuggestionsKeyBinding = GetKeyBindingFrom(_generateSuggestionsCommandName);
        }

        private string GetKeyBindingFrom(string commandName)
        {
            return _taskFactoryProvider.JoinableTaskFactory.Run(async () =>
            {
                try
                {
                    await _taskFactoryProvider.JoinableTaskFactory.SwitchToMainThreadAsync();

                    var dte = (DTE) _serviceProvider.GetService(typeof(DTE));

                    Assumes.Present(dte);

                    var binding = ((IEnumerable) dte.Commands.Item(commandName).Bindings)
                        .Cast<object>()
                        .ToList()
                        .LastOrDefault()?
                        .ToString();

                    return KeyBindingUtilities.FormatKeyBindingDisplayText(binding);
                }
                catch (Exception e)
                {
                    _logger.Error($"Error getting key binding for command {commandName}", e);
                    return string.Empty;
                }
            });
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

        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
        }
    }
}

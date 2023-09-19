using System.Windows.Input;

using Amazon.AwsToolkit.CodeWhisperer.Commands;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Context;

namespace Amazon.AwsToolkit.CodeWhisperer.Margins
{
    public class CodeWhispererMarginViewModel : BaseModel
    {
        private readonly IToolkitContextProvider _toolkitContextProvider;
        private readonly ICodeWhispererManager _manager;

        public CodeWhispererMarginViewModel(ICodeWhispererManager manager,
            IToolkitContextProvider toolkitContextProvider)
        {
            _manager = manager;
            _toolkitContextProvider = toolkitContextProvider;

            SignIn = new SignInCommand(_manager, _toolkitContextProvider);
            SignOut = new SignOutCommand(_manager, _toolkitContextProvider);

            Pause = new PauseCommand(_toolkitContextProvider);
            Resume = new ResumeCommand(_toolkitContextProvider);

            ViewUserGuide = new ViewUserGuideCommand(_toolkitContextProvider);
            GettingStarted = new GettingStartedCommand(_toolkitContextProvider);

            GenerateSuggestions = new GetSuggestionsCommand(_toolkitContextProvider);
            ViewCodeReferences = new ViewCodeReferencesCommand(_toolkitContextProvider);
            SecurityScan = new SecurityScanCommand(_toolkitContextProvider);
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
    }
}

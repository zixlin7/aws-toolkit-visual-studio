using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;
using Amazon.AwsToolkit.VsSdk.Common.Documents;

using Microsoft;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Suggestions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

#pragma warning disable CS0618 // Type or member is obsolete
namespace Amazon.AwsToolkit.CodeWhisperer.Suggestions
{
    [Export(typeof(ISuggestionUiManager))]
    public class SuggestionUiManager : ISuggestionUiManager, IDisposable
    {
        private readonly SuggestionServiceBase _serviceBase;
        private readonly SuggestionProviderBase _suggestionProviderBase = new SuggestionProviderBase();
        private SuggestionSessionBase _session = null;
        private readonly Dictionary<IWpfTextView, SuggestionManagerBase> _textViewManagers =
            new Dictionary<IWpfTextView, SuggestionManagerBase>();
        private readonly ToolkitJoinableTaskFactoryProvider _taskFactoryProvider;

        [ImportingConstructor]
        public SuggestionUiManager(
            SVsServiceProvider serviceProvider,
            ToolkitJoinableTaskFactoryProvider taskFactoryProvider,
            SuggestionServiceBase serviceBase)
        {
            _serviceBase = serviceBase;
            
            _taskFactoryProvider = taskFactoryProvider;
        }

        public async Task ShowAsync(IEnumerable<Suggestion> suggestions, IWpfTextView textView)
        {
            await _taskFactoryProvider.JoinableTaskFactory.SwitchToMainThreadAsync();

            var suggestionContainer = new SuggestionContainer(suggestions, textView, _taskFactoryProvider.DisposalToken);

            var suggestionManager = await CreateSuggestionManagerAsync(textView);

            if (_session != null)
            {
                await _session.DismissAsync(ReasonForDismiss.DismissedBySession, _taskFactoryProvider.DisposalToken);
                _session = null; 
            }

            if (suggestionManager != null)
            {
                _session = await suggestionManager.TryDisplaySuggestionAsync(suggestionContainer,
                    _taskFactoryProvider.DisposalToken);

                if (_session != null)
                {
                    await _session.DisplayProposalAsync(suggestionContainer.CurrentProposal, _taskFactoryProvider.DisposalToken);
                }
            }
        }

        private async Task<SuggestionManagerBase> CreateSuggestionManagerAsync(IWpfTextView textView)
        {
            if (_textViewManagers.Count == 0 || !_textViewManagers.ContainsKey(textView))
            {
                var manager = await _serviceBase.TryRegisterProviderAsync(_suggestionProviderBase, textView,
                    nameof(SuggestionUiManager),
                    _taskFactoryProvider.DisposalToken);

                _textViewManagers.Add(textView, manager);
            }

            return _textViewManagers[textView];
        }

        public void Dispose()
        {
            _taskFactoryProvider.JoinableTaskFactory.Run(async () =>
            {
                foreach (var item in _textViewManagers)
                {
                    await item.Value.DisposeAsync();
                }
                _textViewManagers.Clear();
            });
        }
    }
}

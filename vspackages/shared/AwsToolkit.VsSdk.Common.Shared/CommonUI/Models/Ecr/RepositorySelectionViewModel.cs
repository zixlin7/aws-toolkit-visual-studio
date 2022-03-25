using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.ECS.Models.Ecr;
using Amazon.AWSToolkit.ECS.PluginServices.Ecr;
using Amazon.AWSToolkit.Regions;

using Microsoft.VisualStudio.Threading;

namespace CommonUI.Models
{
    public class RepositorySelectionViewModel : BaseModel
    {
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly IRepositoryFactory _repositoryFactory;

        public ICredentialIdentifier CredentialsId;
        public ToolkitRegion Region;

        private string _filter;

        public string Filter
        {
            get => _filter;
            set
            {
                SetProperty(ref _filter, value);
                GetView()?.Refresh();
            }
        }

        private Repository _repository;

        public Repository Repository
        {
            get => _repository;
            set => SetProperty(ref _repository, value);
        }

        private ObservableCollection<Repository> _repositories;

        public ObservableCollection<Repository> Repositories
        {
            get => _repositories;
            set => SetProperty(ref _repositories, value);
        }

        private ICommand _okCommand;

        public ICommand OkCommand
        {
            get => _okCommand;
            set => SetProperty(ref _okCommand, value);
        }

        public RepositorySelectionViewModel(IRepositoryFactory repositoryFactory, JoinableTaskFactory joinableTaskFactory)
        {
            _repositoryFactory = repositoryFactory;
            _joinableTaskFactory = joinableTaskFactory;
        }

        public async Task RefreshRepositoriesAsync()
        {
            await TaskScheduler.Default;
            var repoRepository = GetRepoRepository();
            var selectedName = Repository?.Name;
            var repositories = await repoRepository.GetRepositoriesAsync()
                .ConfigureAwait(false);

            await _joinableTaskFactory.SwitchToMainThreadAsync();
            Repositories = new ObservableCollection<Repository>(repositories.OrderBy(repo => repo.Name));
            Repository = Repositories.FirstOrDefault(i => i.Name == selectedName);
            GetView().Filter = FilterRepository;
        }

        private IRepoRepository GetRepoRepository()
        {
            return _repositoryFactory.CreateRepoRepository(CredentialsId, Region);
        }

        private ICollectionView GetView() => CollectionViewSource.GetDefaultView(Repositories);

        private bool FilterRepository(object candidate)
        {
            return IsObjectFiltered(candidate, Filter);
        }

        public static bool IsObjectFiltered(object candidate, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return true;
            }

            if (!(candidate is Repository repository))
            {
                return false;
            }

            var filters = filter.Split(new [] { " " }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();
            var candidates = GetFilterByText(repository).ToList();
            return filters.All(f => MatchesOneOrMore(candidates, f));
        }

        private static IEnumerable<string> GetFilterByText(Repository repository)
        {
            var texts = new List<string>();
            texts.Add(repository.Name);

            return texts;
        }

        private static bool MatchesOneOrMore(IEnumerable<string> texts, string filter)
        {
            return texts.Any(text => Contains(text, filter));
        }

        private static bool Contains(string text, string filter)
        {
            return text.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        public void Select(string repoName)
        {
            Repository = Repositories.FirstOrDefault(repository => repository.Name == repoName);
        }
    }
}

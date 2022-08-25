using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Collections;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.Models.Configuration;

namespace Amazon.AWSToolkit.Publish.ViewModels
{
    public class ConfigurationDetailGrouping : BaseModel
    {
        public Category Category { get; set; }

        public List<ConfigurationDetail> ConfigurationDetails { get; } = new List<ConfigurationDetail>();
    }

    /// <summary>
    /// Backs the "Edit Settings" view
    /// </summary>
    public class ConfigurationViewModel : BaseModel
    {
        public PublishDestinationBase PublishDestination { get; set; }

        public ConfigurationDetailGrouping CurrentGroup
        {
            get => _currentGroup;
            set => SetProperty(ref _currentGroup, value);
        }

        public ObservableCollection<ConfigurationDetailGrouping> GroupedSettings { get; } =
            new ObservableCollection<ConfigurationDetailGrouping>();

        private readonly PublishApplicationContext _publishContext;

        private readonly List<ConfigurationDetail> _configurationDetails = new List<ConfigurationDetail>();
        private ConfigurationDetailGrouping _currentGroup;

        public ConfigurationViewModel(PublishApplicationContext publishContext)
        {
            _publishContext = publishContext;
        }

        public void SetConfigurationDetails(IEnumerable<ConfigurationDetail> details)
        {
            _configurationDetails.Clear();
            _configurationDetails.AddRange(details);
        }

        public async Task RecreateGroupsAsync()
        {
            await _publishContext.PublishPackage.JoinableTaskFactory.SwitchToMainThreadAsync();
            var groupings = CreateGroupings();

            GroupedSettings.Clear();
            GroupedSettings.AddAll(groupings.OrderBy(g => g.Category));
        }

        private IEnumerable<ConfigurationDetailGrouping> CreateGroupings()
        {
            if (PublishDestination == null)
            {
                return Enumerable.Empty<ConfigurationDetailGrouping>();
            }

            return _configurationDetails
                .Where(d => d.Visible)
                .GroupBy(d => d.Category)
                .Select(grp =>
                {
                    var category = PublishDestination.ConfigurationCategories.First(c => c.Id == grp.Key);

                    var grouping = new ConfigurationDetailGrouping() { Category = category };
                    grouping.ConfigurationDetails.AddRange(grp);

                    return grouping;
                });
        }
    }
}

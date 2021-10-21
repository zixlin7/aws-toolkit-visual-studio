using System.Collections.ObjectModel;
using System.Linq;

using Amazon.AWSToolkit.Publish.Models;

namespace Amazon.AWSToolkit.Publish.Converters
{
    /// <summary>
    /// Determines visibility based on whether the UI is still loading and if there are any publish targets
    /// <see cref="PublishRecommendation"/>
    /// </summary>
    public class PublishTargetsToVisibilityConverter : TargetsToVisibilityConverter
    {

        protected override bool HasTargets(object targets)
        {
            var recommendations = (ObservableCollection<PublishRecommendation>) targets;
            return recommendations?.Any() ?? false;
        }

    }
}

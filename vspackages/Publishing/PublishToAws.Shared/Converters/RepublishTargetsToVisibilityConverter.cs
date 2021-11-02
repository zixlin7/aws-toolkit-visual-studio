using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

using Amazon.AWSToolkit.Publish.Models;

namespace Amazon.AWSToolkit.Publish.Converters
{
    /// <summary>
    /// Determines visibility based on whether the UI is still loading and if there are any republish targets
    /// <see cref="RepublishTarget"/>
    /// </summary>
    public class RepublishTargetsToVisibilityConverter : TargetsToVisibilityConverter
    {
        protected override bool HasTargets(object targets)
        {
            var republishTargets = (ObservableCollection<RepublishTarget>)targets;
          
            return republishTargets?.Any() ?? false;
        }
    }
}

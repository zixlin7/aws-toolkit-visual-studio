using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.FirstRunSetup.Model
{
    public class FirstRunSetupModel
    {
        public string MediaContent { get; set; }
        public bool OpenExplorerOnExit { get; set; }

        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string AccountNumber { get; set; }
        public string DisplayName { get; set; }

        public List<RegionEndPointsManager.RegionEndPoints> Regions { get; set; }
        public RegionEndPointsManager.RegionEndPoints SelectedRegion { get; set; }

        internal FirstRunSetupModel()
        {
            this.OpenExplorerOnExit = true;

            this.DisplayName = "default";

            // sublocation under install
            this.MediaContent = @"Plugins\Media\ToolkitFirstRun.wmv";
            
            this.Regions = new List<RegionEndPointsManager.RegionEndPoints>();
            foreach (var region in RegionEndPointsManager.Instance.Regions)
            {
                // don't think we want to have govcloud registration on first startup
                if (!region.HasRestrictions)
                    this.Regions.Add(region);
            }

            this.SelectedRegion = this.Regions.Find(r => r.SystemName.Equals("us-west-2", StringComparison.InvariantCultureIgnoreCase));
        }
    }
}

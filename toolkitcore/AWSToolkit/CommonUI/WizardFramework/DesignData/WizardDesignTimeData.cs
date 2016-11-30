using System;
using System.Collections.Generic;
using System.Linq;

namespace Amazon.AWSToolkit.CommonUI.WizardFramework.DesignData
{
    public class TableOfContentsDesignTimeData
    {
        private readonly List<TableOfContentEntry> _sampleData = new List<TableOfContentEntry>
        {
            new TableOfContentEntry { GroupName = "Group1" },
            new TableOfContentEntry { GroupName = "Group1", PageName = "Page1" },
            new TableOfContentEntry { GroupName = "Group1", PageName = "Page2", IsActive = true },
            new TableOfContentEntry { GroupName = "Group2" },
            new TableOfContentEntry { GroupName = "Group2", PageName = "Page1" },
            new TableOfContentEntry { GroupName = "Group3" }
        };

        public IEnumerable<TableOfContentEntry> TableOfContents
        {
            get { return _sampleData; }
        }

        public TableOfContentEntry ActiveTableOfContentEntry
        {
            get
            {
                return _sampleData[1];
            }
        }
    }

    public class PageGroupsDesignTimeData
    {
        readonly List<PageGroup> _sampleData = new List<PageGroup>
        {
            new PageGroup { GroupName = "Group1" },
            new PageGroup { GroupName = "Group2" },
            new PageGroup { GroupName = "Group3" }
        };
 
        public IEnumerable<string> PageGroupNames
        {
            get
            {
                return _sampleData.Select(@group => @group.GroupName).ToList();
            }
        }

        public string ActivePageGroupName
        {
            get { return _sampleData[1].GroupName; }
        }
    }

    public class HeaderDesignTimeData
    {
        public string ActivePageTitle
        {
            get { return "Page Title"; }
        }

        public string ActivePageDescription 
        {
            get { return "Some descriptive text about the page."; }
        }
    } 
}

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.EC2.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Amazon.AWSToolkit.Lambda.View.Components
{
    /// <summary>
    /// Interaction logic for SecurityGroupMultiSelectionControl.xaml
    /// </summary>
    public partial class SecurityGroupMultiSelectionControl : UserControl
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private TextBlock _ctlGroupsSelectionDisplay;

        public SecurityGroupMultiSelectionControl()
        {
            DataContext = this;

            AvailableSecurityGroups = new ObservableCollection<SelectableItem<SecurityGroupWrapper>>();

            InitializeComponent();
            this._ctlGroups.Loaded += _ctlGroups_Loaded;
        }

        public ObservableCollection<SelectableItem<SecurityGroupWrapper>> AvailableSecurityGroups { get; }

        public void SetAvailableSecurityGroups(IEnumerable<SecurityGroup> existingGroups, string autoSelectGroup, IEnumerable<string> selectedSecurityGroupIds)
        {
            AvailableSecurityGroups.Clear();
            if (existingGroups == null || existingGroups.Count() == 0)
            {
                _ctlGroups.IsEnabled = false;
                return;
            }

            foreach (var sg in existingGroups)
            {
                var item = new SelectableItem<SecurityGroupWrapper>(new SecurityGroupWrapper(sg), false);
                if (selectedSecurityGroupIds != null && selectedSecurityGroupIds.Contains(sg.GroupId))
                    item.IsSelected = true;

                AvailableSecurityGroups.Add(item);
            }

            if (string.IsNullOrEmpty(autoSelectGroup))
            {
                if (existingGroups.Count() == 1)
                    AvailableSecurityGroups[0].IsSelected = true;
                else
                {
                    // preselect 'default' group so at least something is selected
                    var defaultGroup
                        = AvailableSecurityGroups.FirstOrDefault((wrapper) => wrapper.InnerObject.DisplayName.Equals("default", StringComparison.OrdinalIgnoreCase));
                    if (defaultGroup != null)
                        defaultGroup.IsSelected = true;
                }
            }
            else
            {
                var preselectedGroup = AvailableSecurityGroups.FirstOrDefault((wrapper) => string.Compare(wrapper.InnerObject.DisplayName, autoSelectGroup, true) == 0);
                if (preselectedGroup != null)
                    preselectedGroup.IsSelected = true;
            }

            FormatDisplayValue();
            _ctlGroups.IsEnabled = true;
            _ctlGroups.Cursor = Cursors.Arrow;
        }

        public IEnumerable<SecurityGroupWrapper> SelectedSecurityGroups
        {
            get
            {
                var selections = new List<SecurityGroupWrapper>();

                foreach (var s in AvailableSecurityGroups)
                {
                    if (s.IsSelected)
                        selections.Add(s.InnerObject);
                }

                return selections;
            }
        }

        private void _ctlGroups_Loaded(object sender, RoutedEventArgs e)
        {
            var contentPresenter = this._ctlGroups.Template.FindName("ContentSite", this._ctlGroups) as ContentPresenter;
            if (contentPresenter != null)
            {
                this._ctlGroupsSelectionDisplay = contentPresenter.ContentTemplate.FindName("PART_ContentPresenter", contentPresenter) as TextBlock;
            }
        }

        private void GroupsItemCheckBox_Clicked(object sender, RoutedEventArgs e)
        {
            FormatDisplayValue();
        }

        private void _ctlGroups_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FormatDisplayValue();
        }

        private void FormatDisplayValue()
        {
            if (this._ctlGroupsSelectionDisplay == null) return;

            var sb = new StringBuilder();
            foreach (var group in AvailableSecurityGroups.OrderBy((x) => x.InnerObject.GroupId))
            {
                if (!group.IsSelected)
                    continue;

                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append(group.InnerObject.GroupId);
            }

            this._ctlGroupsSelectionDisplay.Text = sb.ToString();
        }

        private void _ctlGroups_DropDownClosed(object sender, EventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SecurityGroups"));
        }
    }
}

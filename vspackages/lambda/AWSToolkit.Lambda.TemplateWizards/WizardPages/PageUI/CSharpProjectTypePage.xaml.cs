using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Lambda.TemplateWizards.Model;
using log4net;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows;
using System;

namespace Amazon.AWSToolkit.Lambda.TemplateWizards.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for CSharpProjectTypePage.xaml
    /// </summary>
    public partial class CSharpProjectTypePage : INotifyPropertyChanged
    {
        ILog LOGGER = LogManager.GetLogger(typeof(CSharpProjectTypePage));
        
        string[] RequiredTags { get; }

        public CSharpProjectTypePage()
        {
            InitializeComponent();
            DataContext = this;
        }

        public CSharpProjectTypePage(IAWSWizardPageController controller, BlueprintsModel _model, string[] requiredTags)
            : this()
        {
            this.PageController = controller;
            this.Model = _model;
            this.SelectableBlueprints = new ObservableCollection<Blueprint>();
            this.RequiredTags = requiredTags;

            UpdateSelectableBlueprints(null);
        }

        public IAWSWizardPageController PageController { get; set; }

        public BlueprintsModel Model { get; private set; }

        /// <summary>
        /// The set of blueprints the user can choose from, taking out
        /// non-UserVisible blueprints and blueprints that do not match
        /// any filter the user has specified.
        /// </summary>
        public ObservableCollection<Blueprint> SelectableBlueprints { get; private set; }

        /// <summary>
        /// The blueprint selected by the user; this can be one selected
        /// by the radio buttons or a selection from the list
        /// </summary>
        public Blueprint SelectedBlueprint
        {
            get
            {
                if (Model == null)
                    return null;

                return BlueprintListSelection;
            }
        }


        /// <summary>
        /// This tracks the selection in the blueprint list
        /// </summary>
        private Blueprint BlueprintListSelection { get; set; }

        /// <summary>
        /// Space separated collection of tags to filter the selectable blueprints
        /// </summary>
        public string FilterTags { get; set; }

        private void OnClickApplyFilter(object sender, System.Windows.RoutedEventArgs e)
        {
            string[] filters = null;
            if (!string.IsNullOrEmpty(FilterTags))
                filters = FilterTags.Split(' ');

            UpdateSelectableBlueprints(filters);
        }

        private void UpdateSelectableBlueprints(IEnumerable<string> filterTags)
        {
            var availableBlueprints = Model.BlueprintsFromFilter(this.RequiredTags, filterTags);
            SelectableBlueprints.Clear();
            foreach (var blueprint in availableBlueprints)
            {
                SelectableBlueprints.Add(blueprint);
            }
            NotifyPropertyChanged("SelectableBlueprints");
        }

        private void _ctlBlueprintList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1)
                this.BlueprintListSelection = e.AddedItems[0] as Blueprint;
            else
                this.BlueprintListSelection = null;

            // fire this so the controller can update Finish/Next buttons etc
            NotifyPropertyChanged("BlueprintListSelection");
        }

        private void _ctlFunctionOption1_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            NotifyPropertyChanged("FunctionOption1_Checked");
        }

        private void _ctlFunctionOption2_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            NotifyPropertyChanged("FunctionOption2_Checked");
        }

        private void _ctlBlueprintProject_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            NotifyPropertyChanged("BlueprintProject_Checked");
        }
    }
}

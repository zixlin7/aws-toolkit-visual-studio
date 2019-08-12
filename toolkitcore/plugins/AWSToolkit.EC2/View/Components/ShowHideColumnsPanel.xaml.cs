using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.EC2.View.DataGrid;

namespace Amazon.AWSToolkit.EC2.View.Components
{
    /// <summary>
    /// Interaction logic for ShowHideColumnsPanel.xaml
    /// </summary>
    public partial class ShowHideColumnsPanel
    {
        /// <summary>
        /// To allow for checkbox selection through binding layer, we have to wrap definitions :-(
        /// </summary>
        class EC2ColumnDefinitionWrapper
        {
            public EC2ColumnDefinitionWrapper(EC2ColumnDefinition inner, bool isDisplayed)
                : this(inner, isDisplayed, false)
            {
            }

            public EC2ColumnDefinitionWrapper(EC2ColumnDefinition inner, bool isDisplayed, bool isPendingAddition)
            {
                InnerColumn = inner;
                IsDisplayed = isDisplayed;
                IsPendingAddition = isPendingAddition;
                UnusedTag = false;
            }

            public EC2ColumnDefinition InnerColumn { get; }
            public bool IsDisplayed { get; }

            /// <summary>
            /// Set from column header editing for custom tag types that have been added by the user
            /// as column headers but which have not yet been applied as column headers; at this stage
            /// the user can still delete the entries.
            /// </summary>
            public bool IsPendingAddition
            {
                get;
                set;
            }

            /// <summary>
            /// For custom tags, indicates whether the tag has an assigned value on one or more
            /// objects in the display (used solely for UI rendering distinction)
            /// </summary>
            public bool UnusedTag
            {
                get;
                set;
            }

            EC2ColumnDefinitionWrapper() { }
        }

        ObservableCollection<EC2ColumnDefinitionWrapper> _customTagCollection = new ObservableCollection<EC2ColumnDefinitionWrapper>();

        // the set of fixed attributes is broken into multiple lists, each no more than 10 entries
        List<ObservableCollection<EC2ColumnDefinitionWrapper>> _fixedAttributeLists = new List<ObservableCollection<EC2ColumnDefinitionWrapper>>();

        HashSet<string> _displayedColumnsHash = new HashSet<string>();

        #region Dependency Properties

        public static readonly DependencyProperty ShowCustomAttributePanelProperty
            = DependencyProperty.Register("ShowCustomAttributePanel", typeof(bool), typeof(ShowHideColumnsPanel));

        /// <summary>
        /// Gets/sets whether to show custom 'tags' panel on left side
        /// </summary>
        public bool ShowCustomAttributePanel
        {
            get => (bool)GetValue(ShowCustomAttributePanelProperty);
            set => SetValue(ShowCustomAttributePanelProperty, value);
        }

        #endregion

        public ShowHideColumnsPanel()
        {
            InitializeComponent();
            _customTagsList.ItemsSource = _customTagCollection;
        }

        public override void EndInit()
        {
            base.EndInit();
            this._customAttributesPanel.Visibility = ShowCustomAttributePanel ? Visibility.Visible : Visibility.Collapsed;
        }

        public void SetColumnData(EC2ColumnDefinition[] fixedAttributes, 
                                  string[] tagsInUse, 
                                  EC2ColumnDefinition[] displayedColumns)
        {
            BuildDisplayedColumnsHash(displayedColumns);

            if (ShowCustomAttributePanel)
                LoadCustomTagsList(displayedColumns, tagsInUse);
            LoadFixedAttributes(fixedAttributes);
        }

        public bool HasColumnDataSet => _customTagCollection.Count > 0 || _fixedAttributeLists.Count > 0;

        public EC2ColumnDefinition[] SelectedColumns
        {
            get
            {
                List<EC2ColumnDefinition> output = new List<EC2ColumnDefinition>();
                if (ShowCustomAttributePanel)
                    OutputCustomTags(output);

                OutputFixedAttributes(output);

                return output.ToArray<EC2ColumnDefinition>();
            }
        }

        /// <summary>
        /// Process displayed columns so we have easier lookup to determine
        /// if checkboxes should be ticked when we load the lists
        /// </summary>
        /// <param name="displayedColumns"></param>
        void BuildDisplayedColumnsHash(IEnumerable<EC2ColumnDefinition> displayedColumns)
        {
            _displayedColumnsHash.Clear();

            if (displayedColumns == null)
                return;

            foreach (EC2ColumnDefinition col in displayedColumns)
            {
                string key = GetColumnKey(col);
                _displayedColumnsHash.Add(key);
            }
        }

        string GetColumnKey(EC2ColumnDefinition col)
        {
            return string.Format("{0}_{1}", Enum.GetName(typeof(EC2ColumnDefinition.ColumnType), col.Type), col.FieldName);
        }

        bool IsOnDisplay(EC2ColumnDefinition col)
        {
            return _displayedColumnsHash.Contains(GetColumnKey(col));
        }

        /// <summary>
        /// Render a sorted collection of custom tags, fusing two sets - those that
        /// were persisted into headers last time around and the current set of tags
        /// for the view objects that we know have values assigned. If the user unchecks
        /// a tag that has no value, it will be removed from the persisted data on Apply,
        /// effectively deleting it.
        /// </summary>
        /// <param name="columnDefs"></param>
        void LoadCustomTagsList(IEnumerable<EC2ColumnDefinition> displayedColumns, IEnumerable<string> tagsWithValues)
        {
            _customTagCollection.Clear();

            // fuse unique tags into one separate list so we can sort for display. The extra flag denoting whether
            // the tag is in use allows us to differentiate uncheck action in UI if we want
            Dictionary<string, bool> seenTags = new Dictionary<string, bool>(); 
            List<EC2ColumnDefinition> tagCols = new List<EC2ColumnDefinition>();

            if (displayedColumns != null)
            {
                foreach (EC2ColumnDefinition colDef in displayedColumns)
                {
                    if (colDef.Type == EC2ColumnDefinition.ColumnType.Tag && !seenTags.ContainsKey(colDef.Header))
                    {
                        seenTags.Add(colDef.Header, false);
                        tagCols.Add(colDef);
                    }
                }
            }

            if (tagsWithValues != null)
            {
                foreach (string tag in tagsWithValues)
                {
                    if (!seenTags.ContainsKey(tag))
                    {
                        EC2ColumnDefinition colDef = new EC2ColumnDefinition(tag, EC2ColumnDefinition.ColumnType.Tag);
                        seenTags.Add(colDef.Header, true);
                        tagCols.Add(colDef);
                    }
                    else
                        seenTags[tag] = true;
                }
            }

            // final reprocess into display order with wrapper so we can change UI based on usage etc
            foreach (EC2ColumnDefinition tagCol in tagCols.OrderBy(x => x.Header))
            {
                EC2ColumnDefinitionWrapper wrapper = new EC2ColumnDefinitionWrapper(tagCol, IsOnDisplay(tagCol));
                wrapper.UnusedTag = !seenTags[tagCol.Header];
                _customTagCollection.Add(wrapper);
            }
        }

        void LoadFixedAttributes(IEnumerable<EC2ColumnDefinition> attributes)
        {
            // fixed attrs aren't going to change between invocations on same view, so do
            // this once only :-)
            if (_fixedAttributeLists.Count != 0)
                return;

            // organise (sorted alphabetically) into batches of 10
            const int MaxAttribsPerList = 10;
            ObservableCollection<EC2ColumnDefinitionWrapper> currentCollection = new ObservableCollection<EC2ColumnDefinitionWrapper>();
            foreach (EC2ColumnDefinition colDef in attributes.OrderBy(x => x.Header))
            {
                if (currentCollection.Count == MaxAttribsPerList)
                {
                    _fixedAttributeLists.Add(currentCollection);
                    currentCollection = new ObservableCollection<EC2ColumnDefinitionWrapper>();
                }

                currentCollection.Add(new EC2ColumnDefinitionWrapper(colDef, IsOnDisplay(colDef)));
            }

            // always end up here with a collection containing < batch count
            _fixedAttributeLists.Add(currentCollection);

            // now transfer into a series of listboxes inside the wrap panel container
            _fixedAttributesContainer.Children.Clear();
            foreach (ObservableCollection<EC2ColumnDefinitionWrapper> collection in _fixedAttributeLists)
            {
                ListBox lb = new ListBox();
                lb.ItemsSource = collection;
                lb.Style = this.Resources["ImageAttributeColumnList"] as Style;
                if (_fixedAttributesContainer.Children.Count > 0)
                    lb.Margin = new Thickness(0,8,0,2);
                else
                    lb.Margin = new Thickness(8,8,0,2);

                _fixedAttributesContainer.Children.Add(lb);

            }
        }

        void OutputCustomTags(List<EC2ColumnDefinition> output)
        {
            foreach (EC2ColumnDefinitionWrapper col in _customTagCollection)
            {
                if (col.IsDisplayed)
                    output.Add(col.InnerColumn);
            }
        }

        void OutputFixedAttributes(List<EC2ColumnDefinition> output)
        {
            foreach (ObservableCollection<EC2ColumnDefinitionWrapper> colList in _fixedAttributeLists)
            {
                foreach (EC2ColumnDefinitionWrapper col in colList)
                {
                    if (col.IsDisplayed)
                        output.Add(col.InnerColumn);
                }
            }
        }

        private void btnAddTag_Click(object sender, RoutedEventArgs e)
        {
            string newTagText = _newTagTextBox.Text;
            int insertAt = -1;
            bool isValid = true;
            // do a sweep to guarantee uniqueness and find insertion point in one go
            for (int i = 0; i < _customTagCollection.Count && isValid; i++)
            {
                EC2ColumnDefinitionWrapper wrapper = _customTagCollection[i];
                int comparison = string.Compare(newTagText, wrapper.InnerColumn.Header, true);
                if (comparison == 0)
                {
                    // cannot show a message box, kills popup
                    _alreadyExistsMsg.Visibility = Visibility.Visible;
                    isValid = false;
                }
                else
                    if (comparison < 0 && insertAt == -1)
                    {
                        insertAt = i;
                    }
            }

            if (isValid)
            {
                _alreadyExistsMsg.Visibility = Visibility.Hidden;
                EC2ColumnDefinition colDef = new EC2ColumnDefinition(newTagText, EC2ColumnDefinition.ColumnType.Tag);
                EC2ColumnDefinitionWrapper newTagWrapper = new EC2ColumnDefinitionWrapper(colDef, true);
                newTagWrapper.UnusedTag = true; // by definition at this point
                newTagWrapper.IsPendingAddition = true;
                if (insertAt != -1)
                    _customTagCollection.Insert(insertAt, newTagWrapper);
                else
                    _customTagCollection.Add(newTagWrapper);
                _newTagTextBox.Clear();
                _newTagTextBox.Focus();
            }
            else
            {
                btnAddTag.IsEnabled = false;
                _newTagTextBox.Focus();
            }
        }

        private void _newTagTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // can never get binding to work for this :-(
            btnAddTag.IsEnabled = _newTagTextBox.Text.Length > 0;
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                _alreadyExistsMsg.Visibility = Visibility.Hidden;
                _newTagTextBox.Clear(); // avoid hangovers on next show
            }
        }
    }
}

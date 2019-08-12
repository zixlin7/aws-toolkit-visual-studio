using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Amazon.AWSToolkit.CommonUI.Components
{
    /// <summary>
    /// Custom ComboBox control that presents the popup using a columnar ListView
    /// </summary>
    [ContentProperty("Columns")]
    public class ListViewComboBox : ComboBox
    {
        private const string partPopupDataGrid = "PART_PopupListView";
        private ObservableCollection<GridViewColumn> columns;
        private GridView popupDataListGrid;

        static ListViewComboBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ListViewComboBox), new FrameworkPropertyMetadata(typeof(ListViewComboBox)));
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public ObservableCollection<GridViewColumn> Columns
        {
            get
            {
                if (this.columns == null)
                {
                    this.columns = new ObservableCollection<GridViewColumn>();
                }

                return this.columns;
            }
        }

        public override void OnApplyTemplate()
        {
            if (popupDataListGrid == null)
            {
                ListView popupDataList = this.Template.FindName(partPopupDataGrid, this) as ListView;
                popupDataListGrid = popupDataList.View as GridView;
                if (popupDataListGrid != null && columns != null)
                {
                    for (int i = 0; i < columns.Count; i++)
                        popupDataListGrid.Columns.Add(columns[i]);

                    popupDataList.SelectionChanged += new SelectionChangedEventHandler(popupDataList_SelectionChanged);
                }
            }
            base.OnApplyTemplate();
        }

        void popupDataList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
                SelectedItem = null;
            else
                SelectedItem = e.AddedItems[0];

            IsDropDownOpen = false;
            e.Handled = true;
        }
    }
}

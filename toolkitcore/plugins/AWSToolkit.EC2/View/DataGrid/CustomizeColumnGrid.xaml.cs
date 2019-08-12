using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Amazon.Runtime.Internal.Settings;
using Amazon.AWSToolkit.CommonUI;

using Amazon.EC2;
using Amazon.EC2.Model;
using ThirdParty.Json.LitJson;

using log4net;

namespace Amazon.AWSToolkit.EC2.View.DataGrid
{
    /// <summary>
    /// Interaction logic for CustomizeColumnGrid.xaml
    /// </summary>
    public partial class CustomizeColumnGrid
    {
        const string EC2_USER_PREFERENCES = "EC2UsersPreferences";
        const string EC2_COLUMN_LAYOUTS = "EC2ColumnLayout";

        const string COLUMN_TYPE_PROPERTY = "Property";
        const string COLUMN_TYPE_TAG = "Tag";

        const string JSON_PROP_NAME = "Name";
        const string JSON_PROP_SIZE = "Size";
        const string JSON_PROP_TYPE = "Type";


        static readonly ILog LOGGER = LogManager.GetLogger(typeof(CustomizeColumnGrid));

        IAmazonEC2 _ec2Client;
        Dictionary<string, EC2ColumnDefinition> _allPropertyColumnDefinitions;

        string _userSettingsKey;
        string _defaultColumns;
        EC2ColumnDefinition[] _definitions;

        Guid _updatePreferencesToken;

        public CustomizeColumnGrid()
        {
            InitializeComponent();
            this._ctlDataGrid.ContextMenuOpening += onContextMenuOpening;
            this._ctlDataGrid.SelectionChanged += onSelectionChanged;
            this._ctlDataGrid.ColumnReordered += onColumnReordered;
        }

        void onColumnReordered(object sender, DataGridColumnEventArgs e)
        {
            this.persistColumnLayout();
        }

        public void Initialize(IAmazonEC2 ec2Client, EC2ColumnDefinition[] allPropertyColumnDefinitions, string defaultColumns, string userSettingsKey)
        {
            this._ec2Client = ec2Client;
            this._defaultColumns = defaultColumns;
            this._userSettingsKey = userSettingsKey;

            this._allPropertyColumnDefinitions = new Dictionary<string, EC2ColumnDefinition>();
            foreach (var prop in allPropertyColumnDefinitions)
            {
                this._allPropertyColumnDefinitions.Add(prop.FieldName, prop);
            }

            this.loadInitialColumnSet();

            this._ctlDataGrid.IsEnabled = true;
        }

        void loadInitialColumnSet()
        {
            var layouts = PersistenceManager.Instance.GetSettings(EC2_USER_PREFERENCES)[EC2_COLUMN_LAYOUTS];

            if (!string.IsNullOrEmpty(layouts[this._userSettingsKey]))
            {
                string data = layouts[this._userSettingsKey];
                try
                {
                    loadColumnSet(data);

                    // If no columns were actually loaded then revert back to default column layout
                    if (this._ctlDataGrid.Columns.Count == 0)
                    {
                        loadColumnSet(this._defaultColumns);
                    }
                }
                catch (Exception e)
                {
                    LOGGER.WarnFormat("Failed to load previous column definition: {0} \r\n{1}", e.Message, data);
                    loadColumnSet(this._defaultColumns);
                }
            }
            else
            {
                loadColumnSet(this._defaultColumns);
            }
        }

        void loadColumnSet(string jsonDoc)
        {
            var root = JsonMapper.ToObject(jsonDoc);

            var columns = new List<EC2ColumnDefinition>();
            foreach (JsonData data in root)
            {
                try
                {
                    string name = data[JSON_PROP_NAME].ToString();
                    string strType = data[JSON_PROP_TYPE].ToString();
                    EC2ColumnDefinition.ColumnType type = (EC2ColumnDefinition.ColumnType)Enum.Parse(typeof(EC2ColumnDefinition.ColumnType), strType);
                    EC2ColumnDefinition def = null;
                    if (type == EC2ColumnDefinition.ColumnType.Property)
                        this._allPropertyColumnDefinitions.TryGetValue(name, out def);
                    else if (type == EC2ColumnDefinition.ColumnType.Tag)
                        def = new EC2ColumnDefinition(name, EC2ColumnDefinition.ColumnType.Tag, name, false, null);

                    if (def != null)
                        columns.Add(def);

                    if (data[JSON_PROP_SIZE] != null)
                    {
                        double size = 0;
                        // Make sure we have a size and it is a "reasonable size"
                        if (double.TryParse(data[JSON_PROP_SIZE].ToString(), out size) && size > 0 && size < 1000)
                            def.Width = size;
                    }
                }
                catch(Exception e)
                {
                    LOGGER.Warn("Failed to parse column definition", e);
                }
            }

            this._definitions = columns.ToArray();
            buildColumns();
        }

        void persistColumnLayout()
        {
            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
                {
                    JsonWriter jw = new JsonWriter();

                    jw.WriteArrayStart();

                    foreach (DataGridColumn column in this._ctlDataGrid.Columns.OrderBy(x => x.DisplayIndex))
                    {
                        if (!(column is IEC2Column))
                            continue;

                        IEC2Column ec2Column = column as IEC2Column;

                        jw.WriteObjectStart();

                        jw.WritePropertyName(JSON_PROP_NAME);
                        jw.Write(ec2Column.Definition.FieldName);

                        jw.WritePropertyName(JSON_PROP_TYPE);
                        jw.Write(ec2Column.Definition.Type.ToString());

                        if (!column.Width.IsAuto)
                        {
                            jw.WritePropertyName(JSON_PROP_SIZE);
                            jw.Write(column.Width.DisplayValue);
                        }

                        jw.WriteObjectEnd();
                    }

                    jw.WriteArrayEnd();

                    var settings = PersistenceManager.Instance.GetSettings(EC2_USER_PREFERENCES);
                    var layouts = settings[EC2_COLUMN_LAYOUTS];
                    layouts[this._userSettingsKey] = jw.ToString();
                    PersistenceManager.Instance.SaveSettings(EC2_USER_PREFERENCES, settings);
                }));
        }

        public EC2ColumnDefinition[] ColumnDefinitions
        {
            get => this._definitions;

            set
            {
                this._definitions = value;
                buildColumns();
                persistColumnLayout();
            }
        }

        void buildColumns()
        {
            var currentColumns = new HashSet<string>();
            var newColumns = new HashSet<string>();

            foreach (IEC2Column column in this._ctlDataGrid.Columns)
            {
                currentColumns.Add(column.Definition.FieldName);
            }

            foreach (var def in this._definitions)
            {
                newColumns.Add(def.FieldName);
            }

            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
                {
                    for (int i = this._ctlDataGrid.Columns.Count - 1; i >= 0; i--)
                    {
                        IEC2Column ec2Column = this._ctlDataGrid.Columns[i] as IEC2Column;

                        if (!newColumns.Contains(ec2Column.Definition.FieldName))
                            this._ctlDataGrid.Columns.RemoveAt(i);
                    }

                    if (this._definitions == null)
                        return;

                    foreach (var definition in this._definitions)
                    {
                        if (currentColumns.Contains(definition.FieldName))
                            continue;

                        DataGridColumn column = null;
                        if (definition.Type == EC2ColumnDefinition.ColumnType.Tag)
                            column = new TagColumn(this, definition);
                        else if(definition.Type == EC2ColumnDefinition.ColumnType.Property)
                            column = new PropertyColumn(this, definition);

                        if (definition.Width > 0)
                        {
                            column.Width = new DataGridLength(definition.Width);
                        }

                        if(column != null)
                            this._ctlDataGrid.Columns.Add(column);
                    }

                    this._ctlDataGrid.CanUserReorderColumns = true;
                    this._ctlDataGrid.CanUserResizeColumns = true;
                }));
        }

        void onLoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        public System.Collections.IList SelectedItems => this._ctlDataGrid.SelectedItems;

        public object SelectedItem => this._ctlDataGrid.SelectedItem;

        public IList<T> GetSelectedItems<T>()
        {
            return DataGridHelper.GetSelectedItems<T>(this._ctlDataGrid);
        }

        public void SelectAndScrollIntoView(object obj)
        {
            DataGridHelper.SelectAndScrollIntoView(this._ctlDataGrid, obj);
        }

        public IEnumerable<IEC2Column> Columns
        {
            get 
            {
                IList<IEC2Column> columns = new List<IEC2Column>();
                foreach (var column in this._ctlDataGrid.Columns)
                {
                    if (!(column is IEC2Column))
                        continue;
                    columns.Add((IEC2Column)column);
                }
                return columns; 
            }
        }

        public bool SaveTagValue(string resourceId, string tagName, string tagValue)
        {
            try
            {
                if (string.IsNullOrEmpty(tagValue))
                {
                    var request = new DeleteTagsRequest()
                    {
                        Resources = new List<string>() { resourceId },
                        Tags = new List<Tag>()
                        {
                            new Tag(){ Key = tagName }
                        }
                    };

                    LOGGER.InfoFormat("Delete tag for resource {0} for key {1}", resourceId, tagName);
                    this._ec2Client.DeleteTags(request);
                    LOGGER.InfoFormat("Delete tag for resource {0} for key {1}", resourceId, tagName);
                    ToolkitFactory.Instance.ShellProvider.UpdateStatus(string.Format("Delete tag {0} on {1}", tagName, resourceId));
                }
                else
                {
                    var request = new CreateTagsRequest()
                    {
                        Resources = new List<string>() { resourceId },
                        Tags = new List<Tag>()
                        {
                            new Tag(){Key = tagName, Value= tagValue}
                        }
                    };

                    LOGGER.InfoFormat("Saving tag for resource {0} for key {1}", resourceId, tagName);
                    this._ec2Client.CreateTags(request);
                    LOGGER.InfoFormat("Saved tag for resource {0} for key {1}", resourceId, tagName);
                    ToolkitFactory.Instance.ShellProvider.UpdateStatus(string.Format("Updated tag {0} on {1} with value '{2}'", tagName, resourceId, tagValue));
                }
                
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error saving tag", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error saving tag: " + e.Message);
                return false;
            }
        }

        void SortHandler(object sender, DataGridSortingEventArgs e)
        {
            var ec2Column = e.Column as IEC2Column;
            if (ec2Column == null)
                return;

            e.Handled = true;

            ListSortDirection direction = (e.Column.SortDirection != ListSortDirection.Ascending) ? ListSortDirection.Ascending : ListSortDirection.Descending;
            e.Column.SortDirection = direction;

            //use a ListCollectionView to do the sort.
            ListCollectionView lcv = (ListCollectionView)CollectionViewSource.GetDefaultView(this._ctlDataGrid.ItemsSource);
            lcv.CustomSort = ec2Column;
        }

        public void BeginPersistingPreferences()
        {
            this._updatePreferencesToken = Guid.NewGuid();
            ThreadPool.QueueUserWorkItem(this.asyncRefresh, this._updatePreferencesToken);
        }

        void asyncRefresh(object state)
        {
            if (!(state is Guid))
                return;
            Guid updateToken = (Guid)state;
            if (this._updatePreferencesToken != updateToken)
                return;

            Thread.Sleep(1000);

            if (this._updatePreferencesToken == updateToken)
                persistColumnLayout();
        }

        #region Exposed Events
        public static RoutedEvent GridContextMenuOpeningEvent =
                EventManager.RegisterRoutedEvent(
                    "GridContextMenuOpening",
                   RoutingStrategy.Bubble,
                   typeof(RoutedEventHandler),
                   typeof(CustomizeColumnGrid));

        public event RoutedEventHandler GridContextMenuOpening
        {
            add => AddHandler(GridContextMenuOpeningEvent, value);
            remove => RemoveHandler(GridContextMenuOpeningEvent, value);
        }

        void onContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var arg = new RoutedEventArgs(GridContextMenuOpeningEvent, e);
            RaiseEvent(arg);
        }

        public static RoutedEvent GridSelectionChangedEvent =
                EventManager.RegisterRoutedEvent(
                    "GridSelectionChanged",
                   RoutingStrategy.Bubble,
                   typeof(RoutedEventHandler),
                   typeof(CustomizeColumnGrid));

        public event RoutedEventHandler GridSelectionChanged
        {
            add => AddHandler(GridSelectionChangedEvent, value);
            remove => RemoveHandler(GridSelectionChangedEvent, value);
        }

        void onSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var arg = new RoutedEventArgs(GridSelectionChangedEvent, e);
            RaiseEvent(arg);
        }
        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty KeyFieldProperty =
            DependencyProperty.Register("KeyField", typeof(string),
            typeof(CustomizeColumnGrid),
            new PropertyMetadata(null, null, null),
            null);

        public string KeyField
        {
            get => (string)GetValue(KeyFieldProperty);
            set => SetValue(KeyFieldProperty, value);
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(System.Collections.IEnumerable),
            typeof(CustomizeColumnGrid), 
            new PropertyMetadata(null, ItemsSourceChangedCallback, null), 
            null);

        public System.Collections.IEnumerable ItemsSource
        {
            get => (System.Collections.IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        private static void ItemsSourceChangedCallback(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var grid = obj as CustomizeColumnGrid;
            grid._ctlDataGrid.ItemsSource = e.NewValue as System.Collections.IEnumerable;
        }

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(Boolean),
            typeof(CustomizeColumnGrid),
            new PropertyMetadata(false, IsReadOnlyChangedCallback, null));

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        private static void IsReadOnlyChangedCallback(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var grid = obj as CustomizeColumnGrid;
            grid._ctlDataGrid.IsReadOnly = Convert.ToBoolean(e.NewValue);
        }

        #endregion
    }
}

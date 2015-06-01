using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Amazon.AWSToolkit.Primitives
{
    /// <summary>
    /// Interaction logic for TwoListMover.xaml
    /// </summary>
    public partial class TwoListMover : INotifyPropertyChanged
    {
        ObservableCollection<object> _sortedAvailable;
        ObservableCollection<object> _sortedAssigned;

        public event EventHandler OnDirty;
        bool _isDirty;

        public TwoListMover()
        {
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(onDataContextChanged); 
            InitializeComponent();
        }

        void onDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(this._ctlAssignedLabel != null)
                this._ctlAssignedLabel.DataContext = this;
            if(this._ctlAvailableLabel != null)
                this._ctlAvailableLabel.DataContext = this;
        }

        public IList AvailableItems
        {
            get 
            {
                return (IList)GetValue(AvailableItemsProperty); 
            }
            set 
            { 
                SetValue(AvailableItemsProperty, value);
                if (value is INotifyCollectionChanged)
                {
                    var notif = value as INotifyCollectionChanged;
                    notif.CollectionChanged += (s, e) =>
                        {
                            this.SortedAvailable = sortList((IList)value);
                        };
                }

                this.SortedAvailable = sortList(value);
            }
        }

        public static readonly DependencyProperty AvailableItemsProperty =
            DependencyProperty.Register("AvailableItems",
            typeof(IList),
            typeof(TwoListMover),
            new UIPropertyMetadata(null, collectionItemsCallback));


        public string AvailableItemsLabel
        {
            get
            {
                return (string)GetValue(AvailableItemsLabelProperty);
            }
            set
            {
                SetValue(AvailableItemsLabelProperty, value);
            }
        }

        public static readonly DependencyProperty AvailableItemsLabelProperty =
            DependencyProperty.Register("AvailableItemsLabel",
            typeof(string),
            typeof(TwoListMover));


        public IList AssignedItems
        {
            get 
            {
                return (IList)GetValue(AssignedItemsProperty); 
            }
            set 
            { 
                SetValue(AssignedItemsProperty, value);
                if (value is INotifyCollectionChanged)
                {
                    var notif = value as INotifyCollectionChanged;
                    notif.CollectionChanged += (s, e) =>
                    {
                        this.SortedAssigned = sortList((IList)value);
                    };
                }

                this.SortedAssigned = sortList(value);
            }
        }

        public static readonly DependencyProperty AssignedItemsProperty =
            DependencyProperty.Register("AssignedItems", 
            typeof(IList),
            typeof(TwoListMover),
            new UIPropertyMetadata(null, collectionItemsCallback));


        public string AssignedItemsLabel
        {
            get
            {
                return (string)GetValue(AssignedItemsLabelProperty);
            }
            set
            {
                SetValue(AssignedItemsLabelProperty, value);
            }
        }

        public static readonly DependencyProperty AssignedItemsLabelProperty =
            DependencyProperty.Register("AssignedItemsLabel",
            typeof(string),
            typeof(TwoListMover));


        public ObservableCollection<object> SortedAvailable
        {
            get { return this._sortedAvailable; }
            set
            {
                this._sortedAvailable = value;
                this.NotifyPropertyChanged("SortedAvailable");
            }
        }

        public ObservableCollection<object> SortedAssigned
        {
            get { return this._sortedAssigned; }
            set
            {
                this._sortedAssigned = value;
                this.NotifyPropertyChanged("SortedAssigned");
            }
        }

        public void ResetDirty()
        {
            this._isDirty = false;
        }

        void onMoveAllAvaliable(object sender, RoutedEventArgs e)
        {
            moveItems(this._ctlAvailableList.Items, this.AvailableItems, this.SortedAvailable, this.AssignedItems, this.SortedAssigned);
        }

        void onMoveSelectedAvaliable(object sender, RoutedEventArgs e)
        {
            moveItems(this._ctlAvailableList.SelectedItems, this.AvailableItems, this.SortedAvailable, this.AssignedItems, this.SortedAssigned);
        }

        void onMoveSelectedAssigned(object sender, RoutedEventArgs e)
        {
            moveItems(this._ctlAssignedList.SelectedItems, this.AssignedItems, this.SortedAssigned, this.AvailableItems, this.SortedAvailable);
        }

        void onMoveAllAssigned(object sender, RoutedEventArgs e)
        {
            moveItems(this._ctlAssignedList.Items, this.AssignedItems, this.SortedAssigned, this.AvailableItems, this.SortedAvailable);
        }

        void moveItems(IList itemsToMoveImmutable, IList unsortedSource, IList sortedSource, IList unsortedTarget, IList sortedTarget)
        {
            var itemsToMove = new object[itemsToMoveImmutable.Count];
            itemsToMoveImmutable.CopyTo(itemsToMove, 0);

            int targetIndex = sortedTarget.Count - 1;

            for (int i = itemsToMove.Length - 1; i >= 0; i--)
            {
                object itemToMove = itemsToMove[i];
                string displayName = itemToMove.ToString();
                bool inserted = false;
                for (; targetIndex >= 0; targetIndex--)
                {
                    if (sortedTarget[targetIndex].ToString().CompareTo(displayName) <= 0)
                    {
                        sortedTarget.Insert(targetIndex + 1, itemToMove);
                        inserted = true;
                        break;                       
                    }
                }

                if (!inserted)
                {
                    sortedTarget.Insert(0, itemToMove);
                }
            }

            foreach (var item in itemsToMove)
            {
                unsortedTarget.Add(item);
                unsortedSource.Remove(item);
                sortedSource.Remove(item);
            }

            raiseDirtyEvent();
        }

        static void collectionItemsCallback(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            TwoListMover mover = obj as TwoListMover;
            if (mover == null)
                return;

            IList listValue = args.NewValue as IList;

            if (args.Property.Name.Equals("AvailableItems"))
                mover.AvailableItems = listValue;
            else if (args.Property.Name.Equals("AssignedItems"))
                mover.AssignedItems = listValue;
        }

        ObservableCollection<object> sortList(IList unsortedList)
        {
            var items = new object[unsortedList.Count];
            unsortedList.CopyTo(items, 0);
            Array.Sort(items);
            return new ObservableCollection<object>(items);
        }

        void raiseDirtyEvent()
        {
            if (!this._isDirty && this.OnDirty != null)
                this.OnDirty(this, new EventArgs());

            this._isDirty = true;
        }
    }
}

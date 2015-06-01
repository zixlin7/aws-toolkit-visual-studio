using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Util;

using Amazon.EC2.Model;
using Amazon.AWSToolkit.EC2.Model;

namespace Amazon.AWSToolkit.EC2.View.DataGrid
{
    public class TagColumn : DataGridTextColumn, IEC2Column
    {
        static readonly Thickness TEXT_MARGIN = new Thickness(5, 0, 5, 0);

        CustomizeColumnGrid _parentGrid;
        EC2ColumnDefinition _definition;

        ITagSupport _currentlyEditedItem;
        string _originalvalue;

        public TagColumn(CustomizeColumnGrid parentGrid, EC2ColumnDefinition definition)
        {
            this._parentGrid = parentGrid;
            this._definition = definition;

            var panel = new WrapPanel();
            panel.Children.Add(IconHelper.GetIcon("editable_small.png", 10, 10));
            panel.Children.Add(new TextBlock() { Text = this._definition.Header, Margin = TEXT_MARGIN });


            this.Header = panel;
        }

        public string GetTextValue(object dataItem)
        {
            if (!(dataItem is ITagSupport))
                return null;

            var tag = ((ITagSupport)dataItem).FindTag(this._definition.FieldName);
            return tag == null ? null : tag.Value;
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (this.DataGridOwner == null || !this.DataGridOwner.IsEnabled || !string.Equals(e.Property.Name, "Width") || this.Width.IsAuto)
                return;

            this._parentGrid.BeginPersistingPreferences();
        }

        public EC2ColumnDefinition Definition
        {
            get { return this._definition; }
        }

        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            var tb = new TextBlock();
            tb.Margin = TEXT_MARGIN;
            if (dataItem is ITagSupport)
            {
                Tag tag = ((ITagSupport)dataItem).FindTag(this._definition.FieldName);
                if (tag != null)
                {
                    tb.Text = tag.Value;
                }
            }

            return tb;
        }

        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            if (!(dataItem is ITagSupport))
            {
                return GenerateElement(cell, dataItem);
            }


            this._originalvalue = null;
            this._currentlyEditedItem = ((ITagSupport)dataItem);
            var tb = new TextBox();

            Tag tag = this._currentlyEditedItem.FindTag(this._definition.FieldName);
            if (tag != null)
            {
                tb.Text = tag.Value;
                this._originalvalue = tag.Value;
            }

            return tb;
        }

        protected override bool CommitCellEdit(FrameworkElement editingElement)
        {
            var tb = editingElement as TextBox;
            var newValue = tb.Text;
            if (string.Equals(newValue, this._originalvalue))
                return true;

            var resourceId = ReflectionUtils.GetpropertyValue<string>(this._parentGrid.KeyField, this._currentlyEditedItem);
            if (this._parentGrid.SaveTagValue(resourceId, this._definition.FieldName, newValue))
            {
                this._currentlyEditedItem.SetTag(this._definition.FieldName, newValue);                
                return true;
            }

            return false;
        }

        public int Compare(object x, object y)
        {
            if (this.SortDirection == ListSortDirection.Descending)
            {
                object tmp = x;
                x = y;
                y = tmp;
            }

            ITagSupport x1 = x as ITagSupport;
            ITagSupport y1 = y as ITagSupport;

            if (x1 == null && y1 == null)
                return 0;

            if (x1 == null)
                return -1;

            if (y1 == null)
                return 1;

            var xtag = x1.FindTag(this._definition.FieldName);
            var ytag = y1.FindTag(this._definition.FieldName);

            if (xtag == null && ytag == null)
                return 0;

            if (xtag == null)
                return -1;

            if (ytag == null)
                return 1;

            var result = string.Compare(xtag.Value, ytag.Value);
            if (result == 0)
            {
                var xResourceId = ReflectionUtils.GetpropertyValue<string>(this._parentGrid.KeyField, x);
                var yResourceId = ReflectionUtils.GetpropertyValue<string>(this._parentGrid.KeyField, y);
                return string.Compare(xResourceId, yResourceId);
            }

            return result;
        }
    }
}

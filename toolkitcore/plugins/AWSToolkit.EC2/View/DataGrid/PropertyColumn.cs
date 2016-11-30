using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;

using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.CommonUI;



namespace Amazon.AWSToolkit.EC2.View.DataGrid
{
    public class PropertyColumn : DataGridColumn, IEC2Column
    {        
        static readonly Thickness TEXT_MARGIN = new Thickness(5, 0, 5, 0);

        CustomizeColumnGrid _parentGrid;
        EC2ColumnDefinition _definition;

        public PropertyColumn(CustomizeColumnGrid parentGrid, EC2ColumnDefinition definition)
        {
            this._parentGrid = parentGrid;
            this._definition = definition;
            this.Header = this._definition.Header;
            this.IsReadOnly = true;
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

        public string GetTextValue(object dataItem)
        {
            return ReflectionUtils.GetpropertyValue<string>(this._definition.FieldName, dataItem);
        }

        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            var tb = new TextBlock();
            var ovalue = ReflectionUtils.GetpropertyValue<object>(this._definition.FieldName, dataItem);
            if (ovalue != null)
                tb.Text = ovalue.ToString();

            tb.Margin = TEXT_MARGIN;

            Image icon = null;
            if (!string.IsNullOrEmpty(this._definition.Icon))
            {
                if (this._definition.IsIconDynamic)
                {
                    icon = new Image();
                    icon.Source = ReflectionUtils.GetpropertyValue<ImageSource>(this._definition.Icon, dataItem);
                    icon.Width = icon.Height = 16;
                }
                else
                {
                    string iconPath;
                    Assembly assembly;
                    int pos = this._definition.Icon.IndexOf(';');
                    if (pos > 0)
                    {
                        iconPath = this._definition.Icon.Substring(0, pos);
                        string assemblyName = this._definition.Icon.Substring(pos + 1);
                        assembly = Assembly.LoadWithPartialName(assemblyName);
                    }
                    else
                    {
                        iconPath = this._definition.Icon;
                        assembly = this.GetType().Assembly;
                    }

                    icon = IconHelper.GetIcon(assembly, iconPath);
                }
            }

            if (icon != null)
            {
                icon.Height = 16;
                icon.Width = 16;

                var panel = new WrapPanel();
                panel.Children.Add(icon);
                panel.Children.Add(tb);

                return panel;
            }
            else
            {
                return tb;
            }
        }

        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            return null;
        }

        public int Compare(object x, object y)
        {
            if (this.SortDirection == ListSortDirection.Descending)
            {
                object tmp = x;
                x = y;
                y = tmp;
            }

            IComparable xvalue = ReflectionUtils.GetpropertyValue<IComparable>(this._definition.FieldName, x);
            IComparable yvalue = ReflectionUtils.GetpropertyValue<IComparable>(this._definition.FieldName, y);

            if (xvalue == null && yvalue == null)
                return 0;

            if (xvalue == null)
                return -1;

            if (yvalue == null)
                return 1;

            var result = xvalue.CompareTo(yvalue);
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

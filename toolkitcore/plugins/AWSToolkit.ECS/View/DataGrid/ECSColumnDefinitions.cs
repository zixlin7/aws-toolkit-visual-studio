using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Amazon.AWSToolkit.CommonUI;
using Amazon.ECS.Model;

using Amazon.AWSToolkit.ECS.Model;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.ECS.View.DataGrid
{
    public class ECSColumnDefinition
    {
        public enum ColumnType { Property, Tag };

        public ECSColumnDefinition(string header, ColumnType columnType, string fieldName, bool isIconDynamic, string icon)
        {
            this.Header = header;
            this.Type = columnType;
            this.FieldName = fieldName;
            this.IsIconDynamic = isIconDynamic;
            this.Icon = icon;
        }

        public ECSColumnDefinition(string header, ColumnType columnType)
            : this(header, columnType, header, false, string.Empty)
        {
        }

        public string Header
        {
            get;
            private set;
        }

        public ColumnType Type
        {
            get;
            private set;
        }

        public string FieldName
        {
            get;
            private set;
        }

        public string Icon
        {
            get;
            private set;
        }

        public bool IsIconDynamic
        {
            get;
            private set;
        }

        public double Width
        {
            get;
            set;
        }

        public override string ToString()
        {
            return string.Format("Header: {0} IsTag: {1} FieldName: {2} IsDynamicIcon: {3} Icon: {4}", this.Header, this.Type, this.FieldName, this.IsIconDynamic, this.Icon);
        }

        public static ECSColumnDefinition[] GetPropertyColumnDefinitions(Type ecsType)
        {
            var columns = new List<ECSColumnDefinition>();
            foreach (var mi in ecsType.GetProperties())
            {
                var displayNameAtt = mi.GetCustomAttributes(true).FirstOrDefault(x => x is DisplayNameAttribute) as DisplayNameAttribute;
                if (displayNameAtt == null)
                    continue;

                var iconAttribute = mi.GetCustomAttributes(true).FirstOrDefault(x => x is AssociatedIconAttribute) as AssociatedIconAttribute;

                var def = iconAttribute == null 
                    ? new ECSColumnDefinition(displayNameAtt.DisplayName, ColumnType.Property, mi.Name, false, null) 
                    : new ECSColumnDefinition(displayNameAtt.DisplayName, ColumnType.Property, mi.Name, iconAttribute.IsIconDynamic, iconAttribute.IconPath);

                columns.Add(def);
            }

            return columns.ToArray();
        }
    }
}

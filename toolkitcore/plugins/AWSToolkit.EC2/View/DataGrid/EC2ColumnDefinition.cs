using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Amazon.AWSToolkit.CommonUI;
using Amazon.EC2.Model;

using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.EC2.View.DataGrid
{
    public class EC2ColumnDefinition
    {
        public enum ColumnType { Property, Tag };

        public EC2ColumnDefinition(string header, ColumnType columnType, string fieldName, bool isIconDynamic, string icon)
        {
            this.Header = header;
            this.Type = columnType;
            this.FieldName = fieldName;
            this.IsIconDynamic = isIconDynamic;
            this.Icon = icon;
        }

        public EC2ColumnDefinition(string header, ColumnType columnType)
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

        public static string[] GetListAvailableTags(System.Collections.IEnumerable ec2Objects)
        {
            HashSet<string> tagNames = new HashSet<string>();
            if (ec2Objects != null)
            {
                foreach (var ec2Object in ec2Objects)
                {
                    if (!(ec2Object is ITagSupport))
                        continue;

                    var tags = ((ITagSupport)ec2Object).Tags;
                    if (tags == null)
                        continue;

                    foreach (var tag in tags)
                    {
                        if (!tagNames.Contains(tag.Key))
                            tagNames.Add(tag.Key);
                    }
                }
            }

            return tagNames.ToArray();
        }

        public static EC2ColumnDefinition[] GetPropertyColumnDefinitions(Type ec2Type)
        {
            var columns = new List<EC2ColumnDefinition>();
            foreach (var mi in ec2Type.GetProperties())
            {
                var displayNameAtt = mi.GetCustomAttributes(true).FirstOrDefault(x => x is DisplayNameAttribute) as DisplayNameAttribute;
                if (displayNameAtt == null)
                    continue;

                var iconAttribute = mi.GetCustomAttributes(true).FirstOrDefault(x => x is AssociatedIconAttribute) as AssociatedIconAttribute;

                EC2ColumnDefinition def;
                if (iconAttribute == null)
                    def = new EC2ColumnDefinition(displayNameAtt.DisplayName, ColumnType.Property, mi.Name, false, null);
                else
                    def = new EC2ColumnDefinition(displayNameAtt.DisplayName, ColumnType.Property, mi.Name, iconAttribute.IsIconDynamic, iconAttribute.IconPath);

                columns.Add(def);
            }

            return columns.ToArray();
        }
    }
}

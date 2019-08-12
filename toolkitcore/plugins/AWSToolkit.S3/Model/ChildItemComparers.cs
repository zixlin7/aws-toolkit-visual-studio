using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Amazon.AWSToolkit.S3.Model
{

    public abstract class BaseComparer : IComparer, IComparer<BucketBrowserModel.ChildItem>
    {
        ListSortDirection _direction;
        public BaseComparer(ListSortDirection direction)
        {
            this._direction = direction;
        }

        public int Compare(object x, object y)
        {
            return Compare((BucketBrowserModel.ChildItem)x, (BucketBrowserModel.ChildItem)y);
        }

        public int Compare(BucketBrowserModel.ChildItem x, BucketBrowserModel.ChildItem y)
        {
            var xitem = (BucketBrowserModel.ChildItem)x;
            var yitem = (BucketBrowserModel.ChildItem)y;

            if (xitem == yitem)
            {
                return 0;
            }
            else if (xitem.ChildType == BucketBrowserModel.ChildType.LinkToParent)
            {
                return -1;
            }
            else if (yitem.ChildType == BucketBrowserModel.ChildType.LinkToParent)
            {
                return 1;
            }
            else if (xitem.ChildType != yitem.ChildType)
            {
                return xitem.ChildType == BucketBrowserModel.ChildType.Folder ? -1 : 1;
            }

            if(xitem.ChildType == BucketBrowserModel.ChildType.LinkToParent && yitem.ChildType == BucketBrowserModel.ChildType.LinkToParent)
            {
                Console.WriteLine("Stuff");
            }
        

            if (_direction == ListSortDirection.Ascending)
                return RealCompare(yitem, xitem);

            return RealCompare(xitem, yitem);
        }

        public abstract int RealCompare(BucketBrowserModel.ChildItem xitem, BucketBrowserModel.ChildItem yitem);
    }

    public class NameComparer : BaseComparer
    {
        public NameComparer(ListSortDirection direction)
            : base(direction)
        {
        }

        public override int RealCompare(BucketBrowserModel.ChildItem xitem, BucketBrowserModel.ChildItem yitem)
        {
            return xitem.Title.ToLower().CompareTo(yitem.Title.ToLower());
        }
    }

    public class SizeComparer : BaseComparer
    {
        public SizeComparer(ListSortDirection direction)
            : base(direction)
        {
        }

        public override int RealCompare(BucketBrowserModel.ChildItem xitem, BucketBrowserModel.ChildItem yitem)
        {
            int sizeX = getInt(xitem.FormattedSize);
            int sizeY = getInt(yitem.FormattedSize);

            int value = sizeY.CompareTo(sizeX);
            if (value == 0)
                value = xitem.Title.ToLower().CompareTo(yitem.Title.ToLower());
            return value;
        }

        private int getInt(string strSize)
        {
            if ("--".Equals(strSize))
                return -1;

            int pos = strSize.IndexOf(' ');
            strSize = strSize.Substring(0, pos);
            strSize = strSize.Replace(",", "");
            return int.Parse(strSize);
        }
    }

    public class DateComparer : BaseComparer
    {
        public DateComparer(ListSortDirection direction)
            : base(direction)
        {
        }

        public override int RealCompare(BucketBrowserModel.ChildItem xitem, BucketBrowserModel.ChildItem yitem)
        {
            int value = xitem.FormattedLastModifiedDate.GetValueOrDefault().CompareTo(yitem.FormattedLastModifiedDate);
            if (value == 0)
                value = xitem.Title.ToLower().CompareTo(yitem.Title.ToLower());
            return value;
        }
    }

}
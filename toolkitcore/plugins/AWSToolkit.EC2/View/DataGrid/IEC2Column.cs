using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.EC2.View.DataGrid
{
    public interface IEC2Column : System.Collections.IComparer
    {
        EC2ColumnDefinition Definition { get; }

        string GetTextValue(object dataItem);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.Util
{
    public class HeaderedCsvFile
    {
        public HeaderedCsvFile(string csvFilename)
        {
            using (var sr = new StreamReader(csvFilename))
            {
                var line = sr.ReadLine();
                while (line != null)
                {
                    if (ColumnHeaders == null)
                    {
                        var headerValues = line.Split(new[] { ',' }, StringSplitOptions.None);
                        ColumnHeaders = new List<string>(headerValues);
                    }
                    else
                    {
                        var values = line.Split(new[] { ',' }, StringSplitOptions.None);
                        _rowData.Add(values);
                    }

                    line = sr.ReadLine();
                }
            }
        }

        public HeaderedCsvFile(IEnumerable<string> columnHeaders)
        {
            ColumnHeaders = columnHeaders;
        }

        public IEnumerable<string> ColumnHeaders { get; }

        public void AddRowData(IEnumerable<string> values)
        {
            var valArray = new string[values.Count()];
            int colIndex = 0;
            foreach (var v in values)
            {
                valArray[colIndex++] = v;
            }

            _rowData.Add(valArray);
        }

        public void WriteTo(string filename)
        {
            using (var f = new StreamWriter(filename))
            {
                var headers = new StringBuilder();
                foreach (var c in ColumnHeaders)
                {
                    if (headers.Length > 0)
                        headers.Append(',');
                    headers.Append(c);
                }

                f.WriteLine(headers.ToString());

                foreach (var r in _rowData)
                {
                    var row = new StringBuilder();
                    foreach (var d in r)
                    {
                        if (row.Length > 0)
                            row.Append(',');
                        row.Append(d);
                    }

                    f.WriteLine(row);
                }
            }
        }

        public int ColumnIndexOfHeader(string header)
        {
            var index = 0;
            foreach (var h in ColumnHeaders)
            {
                if (h.Equals(header, StringComparison.OrdinalIgnoreCase))
                    return index;

                index++;
            }

            return -1;
        }

        public IEnumerable<string> ColumnValuesForRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _rowData.Count)
                throw new ArgumentOutOfRangeException();

            return _rowData[rowIndex];
        }

        public int RowCount
        {
            get { return _rowData.Count; }
        }

        private readonly List<string[]> _rowData = new List<string[]>();
    }

}

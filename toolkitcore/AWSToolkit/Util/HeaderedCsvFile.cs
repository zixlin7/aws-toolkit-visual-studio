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

        /// <summary>
        /// Reads the data at the specified row to return key:value map of the available data.
        /// If the file does not conform to the expected layout, an exception is thrown.
        /// </summary>
        /// <param name="columnsToRead">The headers for the columns we want to read</param>
        /// <param name="row">The data row we want to read from</param>
        /// <returns>Dictionary of read data, with the column names as keys</returns>
        public IDictionary<string, string> ReadHeaderedData(IEnumerable<string> columnsToRead, int row)
        {
            var d = new Dictionary<string, string>();

            var expectedColumnCount = columnsToRead.Count();
            var colIndices = new List<int>(expectedColumnCount);

            foreach (var c in columnsToRead)
            {
                var index = ColumnIndexOfHeader(c);
                if (index >= 0)
                {
                    colIndices.Add(index);    
                }
            }

            if (colIndices.Count != expectedColumnCount)
            {
                var err = new StringBuilder("The csv file does not conform to expected layout.\r\n\r\nExpected to find column(s) named: ");
                var i = 0;
                foreach (var c in columnsToRead)
                {
                    if (i > 0)
                    {
                        err.Append(i == expectedColumnCount - 1 ? " and " : ", ");
                    }

                    err.AppendFormat("'{0}'", c);
                    i++;
                }

                throw new Exception(err.ToString());
            }

            var rowData = ColumnValuesForRow(row);
            if (rowData == null)
            {
                var err = row == 0
                    ? "The csv file contains no data beyond column headers"
                    : "The csv file contains no data at row " + row;
                throw new Exception(err);
            }

            foreach (var i in colIndices)
            {
                var data = rowData.ElementAt(i);
                var key = columnsToRead.ElementAt(i);

                d.Add(key, data);
            }

            return d;
        }

        private readonly List<string[]> _rowData = new List<string[]>();
    }

}

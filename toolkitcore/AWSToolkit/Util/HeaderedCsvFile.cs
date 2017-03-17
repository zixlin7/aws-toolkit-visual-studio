using System;
using System.Collections.Generic;
using System.IO;

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
                        RowData.Add(values);
                    }

                    line = sr.ReadLine();
                }
            }
        }

        public IEnumerable<string> ColumnHeaders { get; }

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
            if (rowIndex < 0 || rowIndex >= RowData.Count)
                throw new ArgumentOutOfRangeException();

            return RowData[rowIndex];
        }

        public int RowCount
        {
            get { return RowData.Count; }
        }

        private readonly List<string[]> RowData = new List<string[]>();
    }

}

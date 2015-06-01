using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.Util
{
    public class StringUtils
    {
        public static string DecodeFrom64(string encodedData)
        {
            try
            {
                byte[] encodedDataAsBytes = System.Convert.FromBase64String(encodedData);
                string returnValue = System.Text.ASCIIEncoding.ASCII.GetString(encodedDataAsBytes);

                return returnValue;
            }
            catch
            {
                return encodedData;
            }
        }


        public static string EncodeTo64(string plainText)
        {
            try
            {
                byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(plainText);
                string returnValue = System.Convert.ToBase64String(toEncodeAsBytes);

                return returnValue;
            }
            catch
            {
                return plainText;
            }
        }

        public static List<string> ParseCommaDelimitedList(string value)
        {
            List<string> tokens = new List<string>();
            foreach (var token in value.Split(','))
            {
                var trimmed = token.Trim();
                if (trimmed == "")
                    continue;

                tokens.Add(trimmed);
            }

            return tokens;
        }

        public static string CreateCommaDelimitedList(System.Collections.IEnumerable values)
        {
            return CreateCommaDelimitedList(values, ',');
        }

        public static string CreateCommaDelimitedList(System.Collections.IEnumerable values, char delimiter)
        {
            string separator = string.Format("{0} ", delimiter);
            StringBuilder sb = new StringBuilder();
            foreach (object value in values)
            {
                if (sb.Length > 0)
                    sb.Append(separator);
                sb.Append(value.ToString());
            }

            return sb.ToString();
        }

        public static string CreateCommaDelimitedList<T>(System.Collections.IEnumerable values, Func<T, string> funcToDisplay)
        {
            return CreateCommaDelimitedList<T>(values, ',', funcToDisplay);
        }

        public static string CreateCommaDelimitedList<T>(System.Collections.IEnumerable values, char delimiter, Func<T, string> funcToDisplay)
        {
            string separator = string.Format("{0} ", delimiter);
            StringBuilder sb = new StringBuilder();
            foreach (var value in values)
            {
                if (sb.Length > 0)
                    sb.Append(separator);
                sb.Append(funcToDisplay.Invoke((T)value));
            }

            return sb.ToString();
        }
    }
}

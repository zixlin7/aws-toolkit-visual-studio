using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using Amazon.AWSToolkit.CommonUI;

using Amazon.Auth.AccessControlPolicy;


namespace Amazon.AWSToolkit.PolicyEditor.Model
{
    public class ConditionModel : BaseModel
    {
        Condition _condition;

        public ConditionModel(Condition condition)
        {
            this._condition = condition;

            // Set these so we make sure the cases are correct.
            this.Key = this._condition.ConditionKey;
            this.Type = this._condition.Type;
        }

        public Condition InternalCondition
        {
            get { return this._condition; }
        }

        public string MainLabel
        {
            get { return this._condition.Type; }
        }

        public string SubLabel
        {
            get { return string.Format("{0}: {1}", this._condition.ConditionKey, getValuesAsLabel()); }
        }

        public string Type
        {
            get { return this._condition.Type; }
            set
            {
                if(value == null)
                {
                    this._condition.Type = null;
                }
                else
                {
                    var lowerValue = value.ToLower();
                    string foundValue = value;
                    foreach (var type in PossibleConditionTypes)
                    {
                        if (lowerValue.Equals(type.ToLower()))
                        {
                            foundValue = type;
                            break;
                        }
                    }

                    this._condition.Type = foundValue;
                }

                base.NotifyPropertyChanged("Type");
                base.NotifyPropertyChanged("MainLabel");
            }
        }

        public string Key
        {
            get { return this._condition.ConditionKey; }
            set
            {
                if (value == null)
                {
                    this._condition.ConditionKey = null;
                }
                else
                {
                    var lowerValue = value.ToLower();
                    string foundValue = value;
                    foreach (var key in PossibleConditionKeys)
                    {
                        if (lowerValue.Equals(key.ToLower()))
                        {
                            foundValue = key;
                            break;
                        }
                    }

                    this._condition.ConditionKey = foundValue;
                }

                base.NotifyPropertyChanged("Key");
                base.NotifyPropertyChanged("SubLabel");
            }
        }

        public string Values
        {
            get
            {
                if(this._condition.Values == null)
                    return string.Empty;

                StringBuilder sb = new StringBuilder();
                foreach (var value in this._condition.Values)
                {
                    if (sb.Length > 0)
                        sb.Append(", ");

                    sb.AppendFormat("{0}", value);
                }


                return sb.ToString();
            }
            set
            {
                string[] values = value == null ? new string[0] : value.Split(',');
                List<string> editedValues = new List<string>();
                foreach(var token in values)
                {
                    if (token == null)
                        continue;
                    var token2 = token.Trim();
                    if (string.IsNullOrEmpty(token2))
                        continue;
                    editedValues.Add(token2);
                }
                this._condition.Values = editedValues.ToArray();
                base.NotifyPropertyChanged("Values");
                base.NotifyPropertyChanged("SubLabel");
            }
        }

        public string[] PossibleConditionTypes
        {
            get
            {
                if (_possibleConditionTypes == null)
                {
                    loadConditionMetadata();
                }
                return _possibleConditionTypes;
            }
        }

        public string[] PossibleConditionKeys
        {
            get
            {
                if (_possibleConditionKeys == null)
                {
                    loadConditionMetadata();
                }
                return _possibleConditionKeys;
            }
        }

        private static string[] _possibleConditionTypes;
        private static string[] _possibleConditionKeys;
        static void loadConditionMetadata()
        {
            string config = S3FileFetcher.Instance.GetFileContent("IAMConfiguration.xml");
            if (string.IsNullOrEmpty(config))
            {
                return;
            }

            List<string> values = new List<string>();
            XmlDocument xdoc = new XmlDocument();
            xdoc.LoadXml(config);
            foreach (XmlElement node in xdoc.DocumentElement.SelectNodes("condition/types/type"))
            {
                string v = node.SelectSingleNode("display-name").InnerText;
                values.Add(v);
            }
            _possibleConditionTypes = values.ToArray();

            values.Clear();
            foreach (XmlElement node in xdoc.DocumentElement.SelectNodes("condition/keys/key"))
            {
                string v = node.SelectSingleNode("display-name").InnerText;
                values.Add(v);
            }
            _possibleConditionKeys = values.ToArray();
        }
        

        private string getValuesAsLabel()
        {
            if (this._condition.Values == null || this._condition.Values.Length == 0)
                return "";
            if (this._condition.Values.Length == 1)
                return string.Format("\"{0}\"", this._condition.Values[0]);

            StringBuilder sb = new StringBuilder();
            foreach (var value in this._condition.Values)
            {
                if (sb.Length > 0)
                    sb.Append(", ");

                sb.AppendFormat("\"{0}\"", value);
            }

            return string.Format("[{0}]", sb.ToString());
        }
    }
}

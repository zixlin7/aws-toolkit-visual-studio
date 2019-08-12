using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.SimpleDB.Model
{
    public class EditAttributeModel : BaseModel
    {
        string _attributeName;
        ObservableCollection<MutableString> _values;

        public EditAttributeModel()
            : this("", new List<string>())
        {
        }

        public EditAttributeModel(string attributeName, List<string> values)
        {
            this._attributeName = attributeName;
            this._values = new ObservableCollection<MutableString>();
            foreach(var v in values)
                this._values.Add(new MutableString(v));
        }

        public string AttributeName
        {
            get => this._attributeName;
            set
            {
                this._attributeName = value;
                base.NotifyPropertyChanged("AttributeName");
            }
        }

        public List<string> GetValues()
        {
            List<string> values = new List<string>();
            foreach (var mutableValue in this.Values)
            {
                var v = mutableValue.Value;
                if (v == null)
                    continue;
                v = v.Trim();
                if (v.Equals(""))
                    continue;

                values.Add(v);
            }
            return values;
        }

        public ObservableCollection<MutableString> Values
        {
            get => this._values;
            set
            {
                this._values = value;
                base.NotifyPropertyChanged("Values");
            }
        }
    }
}

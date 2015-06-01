using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;



namespace Amazon.AWSToolkit.Util
{
    public class MutableString : INotifyPropertyChanged
    {
        string _value;
        int _hashCode = new Random().Next();

        public MutableString()
        {
            this._value = string.Empty;
        }

        public MutableString(string value)
        {
            this._value = value;
        }

        public string Value
        {
            get { return this._value; }
            set
            {
                this._value = value;
                this.NotifyPropertyChanged("Value");
            }
        }

        public override string ToString()
        {
            return this.Value;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public override bool Equals(object obj)
        {
            var other = obj as MutableString;
            if (other == null)
                return false;

            return string.Equals(this.Value, other.Value);
        }

        public override int GetHashCode()
        {
            return this._hashCode;
        }
    }
}

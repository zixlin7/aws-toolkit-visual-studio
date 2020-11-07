using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Amazon.AWSToolkit.CommonUI
{
    public abstract class BaseModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets the property value if different, and notifies of a change
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyField">Field that stores the property value</param>
        /// <param name="value">Value to assign to the property</param>
        /// <param name="property">Expression that resolves to the Property being set (this is used to derive the property name)</param>
        protected void SetProperty<T>(ref T propertyField, T value, Expression<Func<T>> property)
        {
            if (object.Equals(propertyField, value)) return;

            propertyField = value;

            if (property.Body is MemberExpression memberExpression)
            {
                NotifyPropertyChanged(memberExpression.Member.Name);
            }
        }
    }
}

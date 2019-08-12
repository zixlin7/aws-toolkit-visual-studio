using System;
using System.ComponentModel;


namespace Amazon.AWSToolkit.CommonUI
{
    public abstract class PropertiesModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public abstract void GetPropertyNames(out string className, out string componentName);

        public PropertyObject CreatePropertyProxy()
        {
            return new PropertyObject(this);
        }

        public class PropertyObject : CustomTypeDescriptor
        {
            PropertiesModel _model;
            string _className;
            string _componentName;

            public PropertyObject(PropertiesModel model)
            {
                this._model = model;
                this._model.GetPropertyNames(out this._className, out this._componentName);
            }

            public override string GetClassName()
            {
                return this._className;
            }

            public override string GetComponentName()
            {
                return this._componentName;
            }

            public override PropertyDescriptorCollection GetProperties()
            {
                return TypeDescriptor.GetProperties(this._model);
            }

            public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
            {
                return TypeDescriptor.GetProperties(this._model, attributes);
            }

            public override AttributeCollection GetAttributes()
            {
                return TypeDescriptor.GetAttributes(this._model);
            }

            public override TypeConverter GetConverter()
            {
                return TypeDescriptor.GetConverter(this._model);
            }

            public override object GetPropertyOwner(PropertyDescriptor pd)
            {
                return this._model;
            }

            public override EventDescriptor GetDefaultEvent()
            {
                return TypeDescriptor.GetDefaultEvent(this._model);
            }

            public override PropertyDescriptor GetDefaultProperty()
            {
                return TypeDescriptor.GetDefaultProperty(this._model);
            }

            public override object GetEditor(Type editorBaseType)
            {
                return TypeDescriptor.GetEditor(this._model, editorBaseType);
            }

            public override EventDescriptorCollection GetEvents()
            {
                return TypeDescriptor.GetEvents(this._model);
            }

            public override EventDescriptorCollection GetEvents(Attribute[] attributes)
            {
                return TypeDescriptor.GetEvents(this._model, attributes);
            }
        }
    }
}

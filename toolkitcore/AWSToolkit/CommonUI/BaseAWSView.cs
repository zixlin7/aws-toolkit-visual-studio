using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Amazon.AWSToolkit.CommonUI
{
    public class BaseAWSView : BaseAWSControl, IPropertySupport
    {
        public BaseAWSView()
        {
            SetResourceReference(Control.BackgroundProperty, "awsWindowBackgroundBrushKey");
            SetResourceReference(Control.ForegroundProperty, "awsWindowTextBrushKey");
        }

        public void ShowProperties(IList<PropertiesModel> objs)
        {
            if (this._onPropertyChange != null)
            {
                this._onPropertyChange(this, true, createPropertyProxies(objs));
            }
        }

        public void UpdateProperties(IList<PropertiesModel> objs)
        {
            if (this._onPropertyChange != null)
            {
                this._onPropertyChange(this, false, createPropertyProxies(objs));
            }
        }

        public void PropageProperties(bool forceShow, System.Collections.IList propertyObjects)
        {
            if (this._onPropertyChange != null)
            {
                this._onPropertyChange(this, forceShow, propertyObjects);
            }
        }

        System.Collections.IList createPropertyProxies(IList<PropertiesModel> objs)
        {
            System.Collections.IList values = new System.Collections.ArrayList();
            foreach (var obj in objs)
            {
                values.Add(obj.CreatePropertyProxy());
            }
            return values;
        }

        private PropertySourceChange _onPropertyChange;
        event PropertySourceChange IPropertySupport.OnPropertyChange
        {
            add
            {
                this._onPropertyChange += value;
            }
            remove
            {
                this._onPropertyChange -= value;
            }
        }
    }
}

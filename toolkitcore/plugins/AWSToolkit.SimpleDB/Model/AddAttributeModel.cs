using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.SimpleDB.Model
{
    public class AddAttributeModel
    {
        string _attributeName;

        public string AttributeName
        {
            get { return this._attributeName; }
            set { this._attributeName = value; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.CloudFormation.Parser.Schema
{
    public class PseudoParameter
    {

        public PseudoParameter(string name, string type, string arrayType, string description)
        {
            this.Name = name;
            this.Type = type;
            this.ArrayType = arrayType;
            this.Description = description;
        }

        public string Name
        {
            get;
            private set;
        }

        public string Type
        {
            get;
            private set;
        }

        public string ArrayType
        {
            get;
            private set;
        }

        public string Description
        {
            get;
            private set;
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}

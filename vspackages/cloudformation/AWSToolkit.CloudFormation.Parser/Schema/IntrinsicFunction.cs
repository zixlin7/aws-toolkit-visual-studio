using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.CloudFormation.Parser.Schema
{
    public class IntrinsicFunction
    {

        public IntrinsicFunction(string name, string parameter, string returnType, string description, string skeleton)
        {
            this.Name = name;
            this.Parameter = parameter;
            this.ReturnType = returnType;
            this.Description = description;
            this.Skeleton = skeleton;
        }

        public string Name
        {
            get;
            private set;
        }

        public string Parameter
        {
            get;
            private set;
        }

        public string ReturnType
        {
            get;
            private set;
        }

        public string Description
        {
            get;
            private set;
        }

        public string Skeleton
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

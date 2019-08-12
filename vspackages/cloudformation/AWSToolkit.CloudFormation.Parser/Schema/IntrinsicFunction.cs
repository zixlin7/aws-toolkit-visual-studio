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
        }

        public string Parameter
        {
            get;
        }

        public string ReturnType
        {
            get;
        }

        public string Description
        {
            get;
        }

        public string Skeleton
        {
            get;
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}

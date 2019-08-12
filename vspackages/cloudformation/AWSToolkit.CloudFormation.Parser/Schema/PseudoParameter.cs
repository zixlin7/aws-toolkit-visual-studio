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
        }

        public string Type
        {
            get;
        }

        public string ArrayType
        {
            get;
        }

        public string Description
        {
            get;
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}

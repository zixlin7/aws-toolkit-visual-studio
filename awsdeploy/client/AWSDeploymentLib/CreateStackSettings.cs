namespace AWSDeployment
{
    public class CreateStackSettings
    {
        public string SNSTopic { set; get; }
        public int CreationTimeout { set; get; }
        public bool RollbackOnFailure { set; get; }

        public CreateStackSettings()
        {
            RollbackOnFailure = true;
        }
    }
}

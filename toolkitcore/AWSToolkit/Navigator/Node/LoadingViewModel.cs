namespace Amazon.AWSToolkit.Navigator.Node
{
    public class LoadingViewModel : AbstractViewModel
    {
        public LoadingViewModel()
            : base(new LoadingMetaNode(), null, "Loading")
        {
        }

        protected override string IconName => null;
    }
}

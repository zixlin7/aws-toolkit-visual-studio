namespace Amazon.AWSToolkit.Navigator
{
    /// <summary>
    /// Executes <see cref="IContextCommand"/> commands that can be created
    /// using an empty constructor on the UI Thread.
    /// </summary>
    public class CommandInstantiator<T> : ContextCommandExecutor where T : IContextCommand, new()
    {
        public CommandInstantiator() : base(() => new T())
        {
        }
    }
}

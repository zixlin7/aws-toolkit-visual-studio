namespace Amazon.AwsToolkit.CodeWhisperer.Credentials
{
    /// <summary>
    /// This interface exists so that ExpirationNotificationManager
    /// can be stubbed for testing in anything that takes a dependency on
    /// ExpirationNotificationManager and interacts with it.
    /// </summary>
    internal interface IExpirationNotificationManager
    {
        // this remains empty until another component needs to interact with ExpirationNotificationManager
    }
}

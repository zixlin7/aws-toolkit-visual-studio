namespace Amazon.AWSToolkit.SQS.Util
{
    public class SqsHelpers
    {
        public static bool IsFifo(string queueName)
        {
            return !string.IsNullOrWhiteSpace(queueName) && queueName.EndsWith(".fifo");
        }
    }
}

namespace TP24.Alton.Tests.sdk;

internal class ReceiveMessagesRequest
{
    public int VisibilityTimeout { get; set; } = 60;

    public int MaxNumberOfMessages { get; set; } = 10;
}

internal class DeleteMessagesRequest
{
    public string ReceiptHandle { get; set; } = null!;
}

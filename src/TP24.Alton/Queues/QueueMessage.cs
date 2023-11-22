namespace TP24.Alton.QueueManagement;

using System.Collections.Generic;

public class QueueMessage
{
    public string Body { get; set; } = null!;

    public string ReceiptHandle { get; set; } = null!;
    public IEnumerable<QueueMessageAttribute> Attributes { get; set; } = null!;
}

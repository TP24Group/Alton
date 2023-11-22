namespace TP24.Alton.QueueManagement;

internal class QueueStateResponse
{
    public string Name { get; set; } = null!;

    public long MessagesInQueue { get; set; }

    public long MessagesInDeadLetterQueue { get; set; }
}

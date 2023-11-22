namespace TP24.Alton;

using Amazon;
using Amazon.SQS;

public class RegionalSqsClientCache
{
    private readonly Dictionary<string, IAmazonSQS> clients = new();

    public IAmazonSQS GetClientForQueue(AltonQueueComponent queue)
    {
        var region = this.GetRegionFromQueue(queue);
        IAmazonSQS client;
        if (this.clients.TryGetValue(region, out var existingClient))
        {
            client = existingClient;
        }
        else
        {
            client = new AmazonSQSClient(RegionEndpoint.GetBySystemName(region));
            this.clients.Add(region, client);
        }

        return client;
    }

    public string GetRegionFromQueue(AltonQueueComponent queue)
    {
        var queueUri = new Uri(queue.QueueUrl);
        return queueUri.Host.Split('.')[1];
    }
}

namespace TP24.Alton.Tests;

using Amazon;
using Shouldly;

public class RegionalSqsClientCacheTests
{
    [Theory]
    [InlineData("https://sqs.us-east-1.amazonaws.com/177715257436/MyQueue", "us-east-1")]
    [InlineData("https://sqs.eu-central-1.amazonaws.com/943857634236/dead-letter-queue", "eu-central-1")]
    [InlineData("https://sqs.ap-southeast-2.amazonaws.com/352647846237/Some-Other_Queue", "ap-southeast-2")]
    public void Gets_region_from_queue_url(string queueUrl, string expectedRegion)
    {
        var queue = new AltonQueueComponent
        {
            QueueUrl = queueUrl
        };

        var cache = new RegionalSqsClientCache();
        var region = cache.GetRegionFromQueue(queue);

        region.ShouldBe(expectedRegion);
        RegionEndpoint.GetBySystemName(region).DisplayName.ShouldNotBe("Unknown"); // the display name should be unknown if it is invalid
    }
}

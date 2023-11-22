namespace TP24.Alton.Tests.Sdk;

using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;

[Collection("sqs-integration")]
public class SqsIntegration
{
    private readonly SqsFixture fixture;
    public SqsIntegration(SqsFixture fixture) => this.fixture = fixture;
}

[CollectionDefinition("sqs-integration")]
public class SqsCollection : ICollectionFixture<SqsFixture>
{
}

public class SqsFixture : IAsyncLifetime
{
    private IContainerService elasticMq = null!;
    public IAmazonSQS SqsClient { get; private set; } = null!;

    public Task InitializeAsync()
    {
        this.elasticMq = new Builder()
            .UseContainer()
            .DeleteIfExists(true, true)
            .WithName("alton-sqs")
            .Command("/usr/bin/java", "-jar /opt/elasticmq/lib/elasticmq.jar")
            .UseImage("softwaremill/elasticmq:latest")
            .ExposePort(9324, 9324) // SQS REST API
            .ExposePort(9325, 9325) // ElasticMq UI
            .WaitForPort("9324/tcp", 10000)
            .RemoveVolumesOnDispose()
            .Build()
            .Start();

        var awsCreds = new BasicAWSCredentials("ignored", "ignored");
        var sqsConfig = new AmazonSQSConfig { ServiceURL = "http://localhost:9324" };
        this.SqsClient = new AmazonSQSClient(awsCreds, sqsConfig);

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        this.elasticMq.Dispose();
        return Task.CompletedTask;
    }

    public async Task<FixtureQueue> ProvisionQueue(string? queueName = null, string? dlqName = null)
    {
        queueName ??= Guid.NewGuid().ToString("N");
        dlqName ??= $"{queueName}-dlq";
        var createDlqResponse = await this.SqsClient.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = dlqName,
            Attributes = new Dictionary<string, string>
            {
                {"MessageRetentionPeriod", $"{(int)TimeSpan.FromDays(14).TotalSeconds}"}
            }
        });
        var dlqAttributes = await this.SqsClient.GetQueueAttributesAsync(
            createDlqResponse.QueueUrl,
            new List<string> {"QueueArn"});

        var createQueueResponse = await this.SqsClient.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = queueName,
            Attributes = new Dictionary<string, string>
            {
                {"RedrivePolicy", $@"{{
                    ""deadLetterTargetArn"": ""{dlqAttributes.QueueARN}"",
                    ""maxReceiveCount"": ""5""
                }}"},
                {"VisibilityTimeout", $"{(int)TimeSpan.FromMinutes(5).TotalSeconds}"}
            }
        });

        return new FixtureQueue(queueName, createQueueResponse.QueueUrl, dlqName, createDlqResponse.QueueUrl);
    }

    public record FixtureQueue(string QueueName, string QueueUrl, string DlqName, string DlqUrl);
}



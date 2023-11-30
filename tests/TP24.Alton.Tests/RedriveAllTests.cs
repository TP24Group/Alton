namespace TP24.Alton.Tests;

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using global::Alton.Tests.Sdk;
using Sdk;
using Shouldly;

public class RedriveAllTests : SqsIntegration
{
    private readonly SqsFixture fixture;

    public RedriveAllTests(SqsFixture fixture) : base(fixture) => this.fixture = fixture;

    [Fact]
    public async Task Redrive_all_starts_move_messages_task()
    {
        var sqsInterceptor = new SqsInterceptor(this.fixture.SqsClient);
        var queue = await this.fixture.ProvisionQueue();
        var client = await HttpServerFixture.GetTestHttpClient(sqsInterceptor, configureOptions: options =>
        {
            options.QueuesToManage = new Dictionary<string, AltonQueueComponent>
            {
                [queue.QueueName] = new() { QueueUrl = queue.QueueUrl, DeadLetterQueueUrl = queue.DlqUrl }
            };
        });

        await this.fixture.SqsClient.SendMessageAsync(queue.DlqUrl, "test1");
        await this.fixture.SqsClient.SendMessageAsync(queue.DlqUrl, "test2");
        await this.fixture.SqsClient.SendMessageAsync(queue.DlqUrl, "test3");

        var response = await client.PostAsync($"queue-management/queues/{queue.QueueName}/redrive-all", null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var queueAttributes = await this.fixture.SqsClient.GetQueueAttributesAsync(queue.QueueUrl, new List<string> {"QueueArn"});
        var dlqAttributes = await this.fixture.SqsClient.GetQueueAttributesAsync(queue.DlqUrl, new List<string> {"QueueArn"});

        sqsInterceptor.MessageMoveTasks.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            task => task.SourceArn.ShouldBe(dlqAttributes.QueueARN),
            task => task.DestinationArn.ShouldBe(queueAttributes.QueueARN),
            task => task.MaxNumberOfMessagesPerSecond.ShouldBe(0));
    }

    [Fact]
    public async Task Redrive_all_returns_not_found_when_queue_does_not_exist()
    {
        var client = await HttpServerFixture.GetTestHttpClient(this.fixture.SqsClient);
        var response = await client.PostAsync($"queue-management/queues/nonexistentQueue/redrive-all", null);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}

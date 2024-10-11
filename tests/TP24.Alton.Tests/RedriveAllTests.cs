namespace TP24.Alton.Tests;

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Sdk;
using Shouldly;

public class RedriveAllTests : SqsIntegration
{
    private readonly SqsFixture fixture;

    public RedriveAllTests(SqsFixture fixture) : base(fixture) => this.fixture = fixture;

    [Fact]
    public async Task Redrive_all_starts_move_messages_task()
    {
        var queue = await this.fixture.ProvisionQueue();
        var client = await HttpServerFixture.GetTestHttpClient(this.fixture.SqsClient, configureOptions: options =>
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

        var receiveMessageResponse1 = await this.fixture.SqsClient.ReceiveMessageAsync(queue.QueueUrl);
        var receiveMessageResponse2 = await this.fixture.SqsClient.ReceiveMessageAsync(queue.QueueUrl);
        var receiveMessageResponse3 = await this.fixture.SqsClient.ReceiveMessageAsync(queue.QueueUrl);
        var messages = receiveMessageResponse1.Messages.Concat(receiveMessageResponse2.Messages).Concat(receiveMessageResponse3.Messages).ToList();

        messages.ShouldSatisfyAllConditions(
            () => messages.Count.ShouldBe(3),
            () => messages.ShouldContain(m => m.Body == "test1"),
            () => messages.ShouldContain(m => m.Body == "test2"),
            () => messages.ShouldContain(m => m.Body == "test3"));
    }

    [Fact]
    public async Task Redrive_all_returns_not_found_when_queue_does_not_exist()
    {
        var client = await HttpServerFixture.GetTestHttpClient(this.fixture.SqsClient);
        var response = await client.PostAsync($"queue-management/queues/nonexistentQueue/redrive-all", null);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}

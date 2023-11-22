namespace TP24.Alton.Tests;

using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using TP24.Alton.Tests.Sdk;
using Shouldly;

public class PurgeQueueTests : SqsIntegration
{
    private readonly SqsFixture fixture;

    public PurgeQueueTests(SqsFixture fixture) : base(fixture) => this.fixture = fixture;

    [Fact]
    public async Task Purge_queue_removes_messages_from_dead_letter_queue()
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

        var response = await client.DeleteAsync($"queue-management/queues/{queue.QueueName}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var receiveMessageResponse = await this.fixture.SqsClient.ReceiveMessageAsync(queue.DlqUrl);
        receiveMessageResponse.Messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task Purge_queue_returns_ok_on_empty_queue()
    {
        var queue = await this.fixture.ProvisionQueue();
        var client = await HttpServerFixture.GetTestHttpClient(this.fixture.SqsClient, configureOptions: options =>
        {
            options.QueuesToManage = new Dictionary<string, AltonQueueComponent>
            {
                [queue.QueueName] = new() { QueueUrl = queue.QueueUrl, DeadLetterQueueUrl = queue.DlqUrl }
            };
        });

        var response = await client.DeleteAsync($"queue-management/queues/{queue.QueueName}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var receiveMessageResponse = await this.fixture.SqsClient.ReceiveMessageAsync(queue.DlqUrl);
        receiveMessageResponse.Messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task Purge_queue_only_effects_dlq()
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
        await this.fixture.SqsClient.SendMessageAsync(queue.QueueUrl, "test1");

        var response = await client.DeleteAsync($"queue-management/queues/{queue.QueueName}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var receiveMessageResponseDlq = await this.fixture.SqsClient.ReceiveMessageAsync(queue.DlqUrl);
        receiveMessageResponseDlq.Messages.ShouldBeEmpty();

        var receiveMessageResponse = await this.fixture.SqsClient.ReceiveMessageAsync(queue.QueueUrl);
        receiveMessageResponse.Messages.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Purge_queue_when_queue_doesnt_exist_returns_404()
    {
        var client = await HttpServerFixture.GetTestHttpClient(this.fixture.SqsClient);
        var response = await client.DeleteAsync($"queue-management/queues/MrIDontExist");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}

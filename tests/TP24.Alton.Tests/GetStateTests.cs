namespace TP24.Alton.Tests;

using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net;
using System.Threading.Tasks;
using Sdk;
using Shouldly;

public class GetStateTests : SqsIntegration
{
    private readonly SqsFixture fixture;

    public GetStateTests(SqsFixture fixture) : base(fixture) => this.fixture = fixture;

    [Fact]
    public async Task Get_queue_state_returns_approximate_messages_for_all_queues()
    {
        var queue1 = await this.fixture.ProvisionQueue();
        var queue2 = await this.fixture.ProvisionQueue();
        var client = await HttpServerFixture.GetTestHttpClient(this.fixture.SqsClient, configureOptions: options =>
        {
            options.QueuesToManage = new Dictionary<string, AltonQueueComponent>
            {
                [queue1.QueueName] = new()
                {
                    QueueUrl = queue1.QueueUrl,
                    DeadLetterQueueUrl = queue1.DlqUrl
                },
                [queue2.QueueName] = new()
                {
                    QueueUrl = queue2.QueueUrl,
                    DeadLetterQueueUrl = queue2.DlqUrl
                }
            };
        });

        await this.fixture.SqsClient.SendMessageAsync(queue1.QueueUrl, "test");
        await this.fixture.SqsClient.SendMessageAsync(queue1.QueueUrl, "test");
        await this.fixture.SqsClient.SendMessageAsync(queue1.DlqUrl, "test");

        var response = await client.GetAsync("queue-management/queue-states");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var queueState = await response.Content.ReadFromJsonAsync<List<QueueStateResponse>>();
        queueState.ShouldNotBeNull().ShouldSatisfyAllConditions(
            () => queueState.Single(q => q.Name == queue1.QueueName).ShouldSatisfyAllConditions(
                queue => queue.MessagesInQueue.ShouldBe(2),
                queue => queue.MessagesInDeadLetterQueue.ShouldBe(1)),
            () => queueState.Single(q => q.Name == queue2.QueueName).ShouldSatisfyAllConditions(
                queue => queue.MessagesInQueue.ShouldBe(0),
                queue => queue.MessagesInDeadLetterQueue.ShouldBe(0)));
    }

    [Fact]
    public async Task Get_queue_state_returns_ok_when_no_messages_in_queues()
    {
        var queue = await this.fixture.ProvisionQueue();
        var client = await HttpServerFixture.GetTestHttpClient(this.fixture.SqsClient, configureOptions: options =>
        {
            options.QueuesToManage = new Dictionary<string, AltonQueueComponent>
            {
                [queue.QueueName] = new()
                {
                    QueueUrl = queue.QueueUrl,
                    DeadLetterQueueUrl = queue.DlqUrl
                }
            };
        });

        var response = await client.GetAsync("queue-management/queue-states");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var queueState = await response.Content.ReadFromJsonAsync<List<QueueStateResponse>>();
        queueState.ShouldNotBeNull().ShouldSatisfyAllConditions(
            () => queueState.Single(q => q.Name == queue.QueueName).ShouldSatisfyAllConditions(
                state => state.MessagesInQueue.ShouldBe(0),
                state => state.MessagesInDeadLetterQueue.ShouldBe(0)));
    }

    [Fact]
    public async Task Get_queue_state_filters_by_has_dlq_messages_true()
    {
        var messageInQueue = await this.fixture.ProvisionQueue();
        var messageInDlq = await this.fixture.ProvisionQueue();
        var messageInBoth = await this.fixture.ProvisionQueue();
        var noMessages = await this.fixture.ProvisionQueue();
        var client = await HttpServerFixture.GetTestHttpClient(this.fixture.SqsClient, configureOptions: options =>
        {
            options.QueuesToManage = new Dictionary<string, AltonQueueComponent>
            {
                [messageInQueue.QueueName] = new()
                {
                    QueueUrl = messageInQueue.QueueUrl,
                    DeadLetterQueueUrl = messageInQueue.DlqUrl
                },
                [messageInDlq.QueueName] = new()
                {
                    QueueUrl = messageInDlq.QueueUrl,
                    DeadLetterQueueUrl = messageInDlq.DlqUrl
                },
                [messageInBoth.QueueName] = new()
                {
                    QueueUrl = messageInBoth.QueueUrl,
                    DeadLetterQueueUrl = messageInBoth.DlqUrl
                },
                [noMessages.QueueName] = new()
                {
                    QueueUrl = noMessages.QueueUrl,
                    DeadLetterQueueUrl = noMessages.DlqUrl
                }
            };
        });

        await this.fixture.SqsClient.SendMessageAsync(messageInQueue.QueueUrl, "test");
        await this.fixture.SqsClient.SendMessageAsync(messageInDlq.DlqUrl, "test");
        await this.fixture.SqsClient.SendMessageAsync(messageInBoth.QueueUrl, "test");
        await this.fixture.SqsClient.SendMessageAsync(messageInBoth.DlqUrl, "test");

        var response = await client.GetAsync("queue-management/queue-states?hasDlqMessages=true");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var queueState = await response.Content.ReadFromJsonAsync<List<QueueStateResponse>>();
        queueState.ShouldNotBeNull().ShouldSatisfyAllConditions(
            () => queueState.Count.ShouldBe(2),
            () => queueState.ShouldContain(q => q.Name == messageInDlq.QueueName),
            () => queueState.ShouldContain(q => q.Name == messageInBoth.QueueName),
            () => queueState.ShouldNotContain(q => q.Name == messageInQueue.QueueName),
            () => queueState.ShouldNotContain(q => q.Name == noMessages.QueueName));
    }

    [Fact]
    public async Task Get_queue_state_filters_by_has_messages_false()
    {
        var messageInQueue = await this.fixture.ProvisionQueue();
        var messageInDlq = await this.fixture.ProvisionQueue();
        var messageInBoth = await this.fixture.ProvisionQueue();
        var noMessages = await this.fixture.ProvisionQueue();
        var client = await HttpServerFixture.GetTestHttpClient(this.fixture.SqsClient, configureOptions: options =>
        {
            options.QueuesToManage = new Dictionary<string, AltonQueueComponent>
            {
                [messageInQueue.QueueName] = new()
                {
                    QueueUrl = messageInQueue.QueueUrl,
                    DeadLetterQueueUrl = messageInQueue.DlqUrl
                },
                [messageInDlq.QueueName] = new()
                {
                    QueueUrl = messageInDlq.QueueUrl,
                    DeadLetterQueueUrl = messageInDlq.DlqUrl
                },
                [messageInBoth.QueueName] = new()
                {
                    QueueUrl = messageInBoth.QueueUrl,
                    DeadLetterQueueUrl = messageInBoth.DlqUrl
                },
                [noMessages.QueueName] = new()
                {
                    QueueUrl = noMessages.QueueUrl,
                    DeadLetterQueueUrl = noMessages.DlqUrl
                }
            };
        });

        await this.fixture.SqsClient.SendMessageAsync(messageInQueue.QueueUrl, "test");
        await this.fixture.SqsClient.SendMessageAsync(messageInDlq.DlqUrl, "test");
        await this.fixture.SqsClient.SendMessageAsync(messageInBoth.QueueUrl, "test");
        await this.fixture.SqsClient.SendMessageAsync(messageInBoth.DlqUrl, "test");

        var response = await client.GetAsync("queue-management/queue-states?hasDlqMessages=false");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var queueState = await response.Content.ReadFromJsonAsync<List<QueueStateResponse>>();
        queueState.ShouldNotBeNull().ShouldSatisfyAllConditions(
            () => queueState.Count.ShouldBe(2),
            () => queueState.ShouldNotContain(q => q.Name == messageInDlq.QueueName),
            () => queueState.ShouldNotContain(q => q.Name == messageInBoth.QueueName),
            () => queueState.ShouldContain(q => q.Name == messageInQueue.QueueName),
            () => queueState.ShouldContain(q => q.Name == noMessages.QueueName));
    }
}

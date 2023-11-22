namespace TP24.Alton.Tests;

using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net;
using System.Threading.Tasks;
using TP24.Alton.Tests.Sdk;
using Shouldly;
using Amazon.SQS.Model;
using TP24.Alton.QueueManagement;
using TP24.Alton.Tests.sdk;

public class GetRetrieveMessagesTests : SqsIntegration
{
    private readonly SqsFixture fixture;

    public GetRetrieveMessagesTests(SqsFixture fixture) : base(fixture) => this.fixture = fixture;

    [Fact]
    public async Task Get_retrieve_messages_returns_message_on_dead_letter_queue()
    {
        var queue = await this.fixture.ProvisionQueue();
        var client = await HttpServerFixture.GetTestHttpClient(this.fixture.SqsClient, configureOptions: options =>
        {
            options.QueuesToManage = new Dictionary<string, AltonQueueComponent>
            {
                [queue.QueueName] = new() {QueueUrl = queue.QueueUrl, DeadLetterQueueUrl = queue.DlqUrl}
            };
        });

        await this.fixture.SqsClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = queue.DlqUrl,
            MessageBody = "test",
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["FavouriteColour"] = new() {DataType = "String", StringValue = "red"},
                ["FavouriteNumber"] = new() {DataType = "Number", StringValue = "2112"}
            }
        });

        var response = await client.PostAsJsonAsync($"queue-management/queues/{queue.QueueName}/retrieve-messages", new ReceiveMessagesRequest());
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var messages = await response.Content.ReadFromJsonAsync<List<QueueMessage>>();
        messages.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            msg => msg.Body.ShouldBe("test"),
            msg => msg.ReceiptHandle.ShouldNotBeNullOrEmpty(),
            msg => msg.Attributes.Count().ShouldBe(2),
            msg => msg.Attributes.Single(x => x.Key == "FavouriteColour").ShouldSatisfyAllConditions(
                attribute => attribute.DataType.ShouldBe("String"),
                attribute => attribute.Value.ShouldBe("red")),
            msg => msg.Attributes.Single(x => x.Key == "FavouriteNumber").ShouldSatisfyAllConditions(
                attribute => attribute.DataType.ShouldBe("Number"),
                attribute => attribute.Value.ShouldBe("2112")));
    }

    [Fact]
    public async Task Get_retrieve_messages_returns_empty_with_no_messages()
    {
        var queue = await this.fixture.ProvisionQueue();
        var client = await HttpServerFixture.GetTestHttpClient(this.fixture.SqsClient, configureOptions: options =>
        {
            options.QueuesToManage = new Dictionary<string, AltonQueueComponent>
            {
                [queue.QueueName] = new() {QueueUrl = queue.QueueUrl, DeadLetterQueueUrl = queue.DlqUrl}
            };
        });

        var response = await client.PostAsJsonAsync($"queue-management/queues/{queue.QueueName}/retrieve-messages", new ReceiveMessagesRequest());
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var messages = await response.Content.ReadFromJsonAsync<List<QueueMessage>>();
        messages.ShouldNotBeNull().Count.ShouldBe(0);
    }

    [Fact]
    public async Task Get_retrieve_messages_returns_multiple_messages_on_dlq()
    {
        var queue = await this.fixture.ProvisionQueue();
        var client = await HttpServerFixture.GetTestHttpClient(this.fixture.SqsClient, configureOptions: options =>
        {
            options.QueuesToManage = new Dictionary<string, AltonQueueComponent>
            {
                [queue.QueueName] = new() {QueueUrl = queue.QueueUrl, DeadLetterQueueUrl = queue.DlqUrl}
            };
        });

        await this.fixture.SqsClient.SendMessageAsync(queue.DlqUrl, "test1");
        await this.fixture.SqsClient.SendMessageAsync(queue.DlqUrl, "test2");
        await this.fixture.SqsClient.SendMessageAsync(queue.DlqUrl, "test3");

        var response = await client.PostAsJsonAsync($"queue-management/queues/{queue.QueueName}/retrieve-messages", new ReceiveMessagesRequest());
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var messages = await response.Content.ReadFromJsonAsync<List<QueueMessage>>();
        messages.ShouldNotBeNull().ShouldSatisfyAllConditions(
            () => messages.Count.ShouldBe(3),
            () => messages.ShouldContain(msg => msg.Body == "test1"),
            () => messages.ShouldContain(msg => msg.Body == "test2"),
            () => messages.ShouldContain(msg => msg.Body == "test3"));
    }

    [Fact]
    public async Task Get_retrieve_messages_returns_specified_amount_of_messages_on_dlq()
    {
        var queue = await this.fixture.ProvisionQueue();
        var client = await HttpServerFixture.GetTestHttpClient(this.fixture.SqsClient, configureOptions: options =>
        {
            options.QueuesToManage = new Dictionary<string, AltonQueueComponent>
            {
                [queue.QueueName] = new() {QueueUrl = queue.QueueUrl, DeadLetterQueueUrl = queue.DlqUrl}
            };
        });

        await this.fixture.SqsClient.SendMessageAsync(queue.DlqUrl, "test1");
        await this.fixture.SqsClient.SendMessageAsync(queue.DlqUrl, "test2");
        await this.fixture.SqsClient.SendMessageAsync(queue.DlqUrl, "test3"); // This one gets ignored!

        var response = await client.PostAsJsonAsync($"queue-management/queues/{queue.QueueName}/retrieve-messages", new ReceiveMessagesRequest {MaxNumberOfMessages = 2});
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var messages = await response.Content.ReadFromJsonAsync<List<QueueMessage>>();
        messages.ShouldNotBeNull().ShouldSatisfyAllConditions(
            () => messages.Count.ShouldBe(2),
            () => messages.ShouldContain(msg => msg.Body == "test1"),
            () => messages.ShouldContain(msg => msg.Body == "test2"));
    }

    [Fact]
    public async Task Get_retrieve_messages_makes_messages_on_queue_temporarily_invisible()
    {
        var queue = await this.fixture.ProvisionQueue();
        var client = await HttpServerFixture.GetTestHttpClient(this.fixture.SqsClient, configureOptions: options =>
        {
            options.QueuesToManage = new Dictionary<string, AltonQueueComponent>
            {
                [queue.QueueName] = new() {QueueUrl = queue.QueueUrl, DeadLetterQueueUrl = queue.DlqUrl}
            };
        });

        await this.fixture.SqsClient.SendMessageAsync(queue.DlqUrl, "test1");

        var response = await client.PostAsJsonAsync($"queue-management/queues/{queue.QueueName}/retrieve-messages", new ReceiveMessagesRequest {VisibilityTimeout = 100});
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var messages = await response.Content.ReadFromJsonAsync<List<QueueMessage>>();
        messages.ShouldHaveSingleItem();

        var responseForSecondRequest = await client.PostAsJsonAsync($"queue-management/queues/{queue.QueueName}/retrieve-messages", new ReceiveMessagesRequest());
        responseForSecondRequest.StatusCode.ShouldBe(HttpStatusCode.OK);

        var currentlyVisibleMessages = await responseForSecondRequest.Content.ReadFromJsonAsync<List<QueueMessage>>();
        currentlyVisibleMessages.ShouldBeEmpty();
    }

    [Fact]
    public async Task Get_retrieve_messages_makes_messages_returns_only_visible_messages()
    {
        var queue = await this.fixture.ProvisionQueue();
        var client = await HttpServerFixture.GetTestHttpClient(this.fixture.SqsClient, configureOptions: options =>
        {
            options.QueuesToManage = new Dictionary<string, AltonQueueComponent>
            {
                [queue.QueueName] = new() {QueueUrl = queue.QueueUrl, DeadLetterQueueUrl = queue.DlqUrl}
            };
        });

        await this.fixture.SqsClient.SendMessageAsync(queue.DlqUrl, "test1");
        await this.fixture.SqsClient.SendMessageAsync(queue.DlqUrl, "test2");
        await this.fixture.SqsClient.SendMessageAsync(queue.DlqUrl, "test3");

        var firstResponse = await client.PostAsJsonAsync($"queue-management/queues/{queue.QueueName}/retrieve-messages", new ReceiveMessagesRequest() { MaxNumberOfMessages = 1, VisibilityTimeout = 100 });
        var firstMessages = await firstResponse.Content.ReadFromJsonAsync<List<QueueMessage>>();
        firstMessages.ShouldHaveSingleItem().Body.ShouldBe("test1");

        var secondResponse = await client.PostAsJsonAsync($"queue-management/queues/{queue.QueueName}/retrieve-messages", new ReceiveMessagesRequest() { MaxNumberOfMessages = 1,  VisibilityTimeout = 100 });
        var secondMessages = await secondResponse.Content.ReadFromJsonAsync<List<QueueMessage>>();
        secondMessages.ShouldHaveSingleItem().Body.ShouldBe("test2");

        var thirdResponse = await client.PostAsJsonAsync($"queue-management/queues/{queue.QueueName}/retrieve-messages", new ReceiveMessagesRequest() { MaxNumberOfMessages = 1, VisibilityTimeout = 100 });
        var thirdMessages = await thirdResponse.Content.ReadFromJsonAsync<List<QueueMessage>>();
        thirdMessages.ShouldHaveSingleItem().Body.ShouldBe("test3");
    }

    [Fact]
    public async Task Get_retrieve_messages_does_not_return_messages_on_normal_queue()
    {
        var queue = await this.fixture.ProvisionQueue();
        var client = await HttpServerFixture.GetTestHttpClient(this.fixture.SqsClient, configureOptions: options =>
        {
            options.QueuesToManage = new Dictionary<string, AltonQueueComponent>
            {
                [queue.QueueName] = new() {QueueUrl = queue.QueueUrl, DeadLetterQueueUrl = queue.DlqUrl}
            };
        });

        await this.fixture.SqsClient.SendMessageAsync(queue.QueueUrl, "test1");
        await this.fixture.SqsClient.SendMessageAsync(queue.QueueUrl, "test2");
        await this.fixture.SqsClient.SendMessageAsync(queue.QueueUrl, "test3");

        var response = await client.PostAsJsonAsync($"queue-management/queues/{queue.QueueName}/retrieve-messages", new ReceiveMessagesRequest());
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var messages = await response.Content.ReadFromJsonAsync<List<QueueMessage>>();
        messages.ShouldBeEmpty();
    }
}

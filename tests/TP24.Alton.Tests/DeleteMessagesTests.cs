namespace TP24.Alton.Tests;

using System.Net;
using System.Threading.Tasks;
using TP24.Alton.Tests.Sdk;
using Shouldly;
using System.Net.Http.Json;
using TP24.Alton.QueueManagement;
using System.Web;
using TP24.Alton.Tests.sdk;

public class DeleteMessageTests : SqsIntegration
{
    private readonly SqsFixture fixture;

    public DeleteMessageTests(SqsFixture fixture) : base(fixture) => this.fixture = fixture;

    [Fact]
    public async Task Delete_message_removes_message_from_dead_letter_queue()
    {
        var queue = await this.fixture.ProvisionQueue();
        var client = await HttpServerFixture.GetTestHttpClient(this.fixture.SqsClient, configureOptions: options =>
        {
            options.QueuesToManage = new Dictionary<string, AltonQueueComponent>
            {
                [queue.QueueName] = new() { QueueUrl = queue.QueueUrl, DeadLetterQueueUrl = queue.DlqUrl }
            };
        });

        var sendMessageResponse = await this.fixture.SqsClient.SendMessageAsync(queue.DlqUrl, "test1");

        var retrieveMessageResponse = await client.PostAsJsonAsync($"queue-management/queues/{queue.QueueName}/retrieve-messages", new ReceiveMessagesRequest());
        var messages = await retrieveMessageResponse.Content.ReadFromJsonAsync<List<QueueMessage>>();
        var receipt = messages!.FirstOrDefault()!.ReceiptHandle;

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/queue-management/queues/{queue.QueueName}/messages")
            {
                Content = JsonContent.Create(new DeleteMessagesRequest { ReceiptHandle = receipt })
            });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var receiveMessageResponse = await this.fixture.SqsClient.ReceiveMessageAsync(queue.DlqUrl);
        receiveMessageResponse.Messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task Delete_message_returns_404_when_queue_does_not_exist()
    {
        var client = await HttpServerFixture.GetTestHttpClient(this.fixture.SqsClient);
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/queue-management/queues/queue/messages")
        {
            Content = JsonContent.Create(new DeleteMessagesRequest { ReceiptHandle = "messageHandleReceipt" })
        });
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_message_with_not_found_receipt_returns_error()
    {
        var queue = await this.fixture.ProvisionQueue();
        var client = await HttpServerFixture.GetTestHttpClient(this.fixture.SqsClient, configureOptions: options =>
        {
            options.QueuesToManage = new Dictionary<string, AltonQueueComponent>
            {
                [queue.QueueName] = new() { QueueUrl = queue.QueueUrl, DeadLetterQueueUrl = queue.DlqUrl }
            };
        });

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/queue-management/queues/{queue.QueueName}/messages")
        {
            Content = JsonContent.Create(new DeleteMessagesRequest { ReceiptHandle = "messageHandleReceipt" })
        });
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_message_returns_404_when_dead_letter_queue_url_is_null()
    {
        var queue = await this.fixture.ProvisionQueue();

        var client = await HttpServerFixture.GetTestHttpClient(this.fixture.SqsClient, configureOptions: options =>
        {
            options.QueuesToManage = new Dictionary<string, AltonQueueComponent>
            {
                [queue.QueueName] = new() { QueueUrl = queue.QueueUrl }
            };
        });

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/queue-management/queues/{queue.QueueName}/messages")
        {
            Content = JsonContent.Create(new DeleteMessagesRequest { ReceiptHandle = "messageHandleReceipt" })
        });
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}

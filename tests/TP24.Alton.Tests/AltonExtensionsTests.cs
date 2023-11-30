namespace TP24.Alton.Tests;

using System.Net;
using Sdk;
using Shouldly;

public class AltonExtensionsTests : SqsIntegration
{
    private readonly SqsFixture fixture;

    public AltonExtensionsTests(SqsFixture fixture) : base(fixture) => this.fixture = fixture;

    [Theory]
    [InlineData("queue-management/queues/testQueue/redrive-all", "POST")]
    [InlineData("queue-management/queue-states", "GET")]
    [InlineData("queue-management/queues/testQueue/retrieve-messages", "POST")]
    [InlineData("queue-management/queues/testQueue", "DELETE")]
    public async Task Endpoints_return_401_unauthorised_without_authenticated_user(string url, string method)
    {
        var client = await HttpServerFixture.GetTestHttpClient(this.fixture.SqsClient, false);

        var msg = new HttpRequestMessage(method switch
        {
            "POST" => HttpMethod.Post, "GET" => HttpMethod.Get, "DELETE" => HttpMethod.Delete, _ => null!
        }, url);

        var response = await client.SendAsync(msg);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Invalid_endpoint_returns_404()
    {
        var client = await HttpServerFixture.GetTestHttpClient(this.fixture.SqsClient);

        var response = await client.GetAsync("queue-management/MrIDontExist");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}

internal class QueueStateResponse
{
    public string Name { get; set; } = null!;

    public long MessagesInQueue { get; set; }

    public long MessagesInDeadLetterQueue { get; set; }
}

namespace TP24.Alton.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http.Json;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using TP24.Alton.Tests.Sdk;
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
                    queue => queue.MessagesInQueue.ShouldBe(0),
                    queue => queue.MessagesInDeadLetterQueue.ShouldBe(0)));
        }

    }
}

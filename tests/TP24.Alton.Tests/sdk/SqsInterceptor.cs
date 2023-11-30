namespace Alton.Tests.Sdk;

using System.Net;
using Amazon.Runtime;
using Amazon.Runtime.Endpoints;
using Amazon.SQS;
using Amazon.SQS.Model;

public class SqsInterceptor : IAmazonSQS
{
    private readonly IAmazonSQS sqs;

    public SqsInterceptor(IAmazonSQS sqs) => this.sqs = sqs;

    public List<StartMessageMoveTaskRequest> MessageMoveTasks { get; } = new();

    /// <summary>
    /// Not currently supported by elasticmq and localstack. This just captures our requests so we can validate state.
    /// Alternatives would be to use SQS proper but I don't think we need to.
    /// </summary>
    public Task<StartMessageMoveTaskResponse> StartMessageMoveTaskAsync(StartMessageMoveTaskRequest request,
        CancellationToken cancellationToken = new CancellationToken())
    {
        this.MessageMoveTasks.Add(request);
        return Task.FromResult(new StartMessageMoveTaskResponse
        {
            HttpStatusCode = HttpStatusCode.OK,
            TaskHandle = $"task/{Guid.NewGuid():N}"
        });
    }

    public void Dispose() => this.sqs.Dispose();

    public Task<Dictionary<string, string>> GetAttributesAsync(string queueUrl) => this.sqs.GetAttributesAsync(queueUrl);

    public Task SetAttributesAsync(string queueUrl, Dictionary<string, string> attributes) => this.sqs.SetAttributesAsync(queueUrl, attributes);

    public IClientConfig Config => this.sqs.Config;

    public Task<string> AuthorizeS3ToSendMessageAsync(string queueUrl, string bucket) => this.sqs.AuthorizeS3ToSendMessageAsync(queueUrl, bucket);

    public Task<AddPermissionResponse> AddPermissionAsync(string queueUrl, string label, List<string> awsAccountIds, List<string> actions,
        CancellationToken cancellationToken = new CancellationToken()) =>
        this.sqs.AddPermissionAsync(queueUrl, label, awsAccountIds, actions, cancellationToken);

    public Task<AddPermissionResponse> AddPermissionAsync(AddPermissionRequest request, CancellationToken cancellationToken = new CancellationToken()) => this.sqs.AddPermissionAsync(request, cancellationToken);

    public Task<CancelMessageMoveTaskResponse> CancelMessageMoveTaskAsync(CancelMessageMoveTaskRequest request,
        CancellationToken cancellationToken = new CancellationToken()) =>
        this.sqs.CancelMessageMoveTaskAsync(request, cancellationToken);

    public Task<ChangeMessageVisibilityResponse> ChangeMessageVisibilityAsync(string queueUrl, string receiptHandle, int visibilityTimeout,
        CancellationToken cancellationToken = new CancellationToken()) =>
        this.sqs.ChangeMessageVisibilityAsync(queueUrl, receiptHandle, visibilityTimeout, cancellationToken);

    public Task<ChangeMessageVisibilityResponse> ChangeMessageVisibilityAsync(ChangeMessageVisibilityRequest request,
        CancellationToken cancellationToken = new CancellationToken()) =>
        this.sqs.ChangeMessageVisibilityAsync(request, cancellationToken);

    public Task<ChangeMessageVisibilityBatchResponse> ChangeMessageVisibilityBatchAsync(string queueUrl, List<ChangeMessageVisibilityBatchRequestEntry> entries,
        CancellationToken cancellationToken = new CancellationToken()) =>
        this.sqs.ChangeMessageVisibilityBatchAsync(queueUrl, entries, cancellationToken);

    public Task<ChangeMessageVisibilityBatchResponse> ChangeMessageVisibilityBatchAsync(ChangeMessageVisibilityBatchRequest request,
        CancellationToken cancellationToken = new CancellationToken()) =>
        this.sqs.ChangeMessageVisibilityBatchAsync(request, cancellationToken);

    public Task<CreateQueueResponse> CreateQueueAsync(string queueName, CancellationToken cancellationToken = new CancellationToken()) => this.sqs.CreateQueueAsync(queueName, cancellationToken);

    public Task<CreateQueueResponse> CreateQueueAsync(CreateQueueRequest request, CancellationToken cancellationToken = new CancellationToken()) => this.sqs.CreateQueueAsync(request, cancellationToken);

    public Task<DeleteMessageResponse> DeleteMessageAsync(string queueUrl, string receiptHandle,
        CancellationToken cancellationToken = new CancellationToken()) =>
        this.sqs.DeleteMessageAsync(queueUrl, receiptHandle, cancellationToken);

    public Task<DeleteMessageResponse> DeleteMessageAsync(DeleteMessageRequest request, CancellationToken cancellationToken = new CancellationToken()) => this.sqs.DeleteMessageAsync(request, cancellationToken);

    public Task<DeleteMessageBatchResponse> DeleteMessageBatchAsync(string queueUrl, List<DeleteMessageBatchRequestEntry> entries,
        CancellationToken cancellationToken = new CancellationToken()) =>
        this.sqs.DeleteMessageBatchAsync(queueUrl, entries, cancellationToken);

    public Task<DeleteMessageBatchResponse> DeleteMessageBatchAsync(DeleteMessageBatchRequest request,
        CancellationToken cancellationToken = new CancellationToken()) =>
        this.sqs.DeleteMessageBatchAsync(request, cancellationToken);

    public Task<DeleteQueueResponse> DeleteQueueAsync(string queueUrl, CancellationToken cancellationToken = new CancellationToken()) => this.sqs.DeleteQueueAsync(queueUrl, cancellationToken);

    public Task<DeleteQueueResponse> DeleteQueueAsync(DeleteQueueRequest request, CancellationToken cancellationToken = new CancellationToken()) => this.sqs.DeleteQueueAsync(request, cancellationToken);

    public Task<GetQueueAttributesResponse> GetQueueAttributesAsync(string queueUrl, List<string> attributeNames,
        CancellationToken cancellationToken = new CancellationToken()) =>
        this.sqs.GetQueueAttributesAsync(queueUrl, attributeNames, cancellationToken);

    public Task<GetQueueAttributesResponse> GetQueueAttributesAsync(GetQueueAttributesRequest request,
        CancellationToken cancellationToken = new CancellationToken()) =>
        this.sqs.GetQueueAttributesAsync(request, cancellationToken);

    public Task<GetQueueUrlResponse> GetQueueUrlAsync(string queueName, CancellationToken cancellationToken = new CancellationToken()) => this.sqs.GetQueueUrlAsync(queueName, cancellationToken);

    public Task<GetQueueUrlResponse> GetQueueUrlAsync(GetQueueUrlRequest request, CancellationToken cancellationToken = new CancellationToken()) => this.sqs.GetQueueUrlAsync(request, cancellationToken);

    public Task<ListDeadLetterSourceQueuesResponse> ListDeadLetterSourceQueuesAsync(ListDeadLetterSourceQueuesRequest request,
        CancellationToken cancellationToken = new CancellationToken()) =>
        this.sqs.ListDeadLetterSourceQueuesAsync(request, cancellationToken);

    public Task<ListMessageMoveTasksResponse> ListMessageMoveTasksAsync(ListMessageMoveTasksRequest request,
        CancellationToken cancellationToken = new CancellationToken()) =>
        this.sqs.ListMessageMoveTasksAsync(request, cancellationToken);

    public Task<ListQueuesResponse> ListQueuesAsync(string queueNamePrefix, CancellationToken cancellationToken = new CancellationToken()) => this.sqs.ListQueuesAsync(queueNamePrefix, cancellationToken);

    public Task<ListQueuesResponse> ListQueuesAsync(ListQueuesRequest request, CancellationToken cancellationToken = new CancellationToken()) => this.sqs.ListQueuesAsync(request, cancellationToken);

    public Task<ListQueueTagsResponse> ListQueueTagsAsync(ListQueueTagsRequest request, CancellationToken cancellationToken = new CancellationToken()) => this.sqs.ListQueueTagsAsync(request, cancellationToken);

    public Task<PurgeQueueResponse> PurgeQueueAsync(string queueUrl, CancellationToken cancellationToken = new CancellationToken()) => this.sqs.PurgeQueueAsync(queueUrl, cancellationToken);

    public Task<PurgeQueueResponse> PurgeQueueAsync(PurgeQueueRequest request, CancellationToken cancellationToken = new CancellationToken()) => this.sqs.PurgeQueueAsync(request, cancellationToken);

    public Task<ReceiveMessageResponse> ReceiveMessageAsync(string queueUrl, CancellationToken cancellationToken = new CancellationToken()) => this.sqs.ReceiveMessageAsync(queueUrl, cancellationToken);

    public Task<ReceiveMessageResponse> ReceiveMessageAsync(ReceiveMessageRequest request, CancellationToken cancellationToken = new CancellationToken()) => this.sqs.ReceiveMessageAsync(request, cancellationToken);

    public Task<RemovePermissionResponse> RemovePermissionAsync(string queueUrl, string label,
        CancellationToken cancellationToken = new CancellationToken()) =>
        this.sqs.RemovePermissionAsync(queueUrl, label, cancellationToken);

    public Task<RemovePermissionResponse> RemovePermissionAsync(RemovePermissionRequest request,
        CancellationToken cancellationToken = new CancellationToken()) =>
        this.sqs.RemovePermissionAsync(request, cancellationToken);

    public Task<SendMessageResponse> SendMessageAsync(string queueUrl, string messageBody,
        CancellationToken cancellationToken = new CancellationToken()) =>
        this.sqs.SendMessageAsync(queueUrl, messageBody, cancellationToken);

    public Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = new CancellationToken()) => this.sqs.SendMessageAsync(request, cancellationToken);

    public Task<SendMessageBatchResponse> SendMessageBatchAsync(string queueUrl, List<SendMessageBatchRequestEntry> entries,
        CancellationToken cancellationToken = new CancellationToken()) =>
        this.sqs.SendMessageBatchAsync(queueUrl, entries, cancellationToken);

    public Task<SendMessageBatchResponse> SendMessageBatchAsync(SendMessageBatchRequest request,
        CancellationToken cancellationToken = new CancellationToken()) =>
        this.sqs.SendMessageBatchAsync(request, cancellationToken);

    public Task<SetQueueAttributesResponse> SetQueueAttributesAsync(string queueUrl, Dictionary<string, string> attributes,
        CancellationToken cancellationToken = new CancellationToken()) =>
        this.sqs.SetQueueAttributesAsync(queueUrl, attributes, cancellationToken);

    public Task<SetQueueAttributesResponse> SetQueueAttributesAsync(SetQueueAttributesRequest request,
        CancellationToken cancellationToken = new CancellationToken()) =>
        this.sqs.SetQueueAttributesAsync(request, cancellationToken);

    public Task<TagQueueResponse> TagQueueAsync(TagQueueRequest request, CancellationToken cancellationToken = new CancellationToken()) => this.sqs.TagQueueAsync(request, cancellationToken);

    public Task<UntagQueueResponse> UntagQueueAsync(UntagQueueRequest request, CancellationToken cancellationToken = new CancellationToken()) => this.sqs.UntagQueueAsync(request, cancellationToken);

    public Endpoint DetermineServiceOperationEndpoint(AmazonWebServiceRequest request) => this.sqs.DetermineServiceOperationEndpoint(request);

    public ISQSPaginatorFactory Paginators => this.sqs.Paginators;
}

namespace TP24.Alton;

using System;
using System.Net;
using TP24.Alton.QueueManagement;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

internal static class QueueOperations
{
    public static async Task GetOverallState(AltonOptions options, SqsClientResolver sqsClientResolver, HttpContext context)
    {
        var queueStates = options.QueuesToManage.Select(altonQueueComponent => GetState(altonQueueComponent.Key, options, sqsClientResolver));

        var responses = await Task.WhenAll(queueStates);

        await context.Response.WriteJsonResponse(responses, context.RequestAborted);
    }

    public static async Task GetRetrieveMessages(string queueName, AltonOptions options, SqsClientResolver sqsClientResolver, HttpContext context, ReceiveMessagesRequest request)
    {
        var queueUrls = options.QueuesToManage[queueName];
        var sqsClient = sqsClientResolver(queueUrls);

        var receiveMessageResponse = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = queueUrls.DeadLetterQueueUrl,
            MaxNumberOfMessages = request.MaxNumberOfMessages,
            VisibilityTimeout = request.VisibilityTimeout,
            MessageAttributeNames = new List<string> { "All" }
        });

        await context.Response.WriteJsonResponse(receiveMessageResponse.Messages.Select(message => new QueueMessage
        {
            Body = message.Body,
            ReceiptHandle = message.ReceiptHandle,
            Attributes = message.MessageAttributes.Select(attribute => new QueueMessageAttribute
            {
                Value = attribute.Value.StringValue,
                DataType = attribute.Value.DataType,
                Key = attribute.Key
            })
        }), context.RequestAborted);
    }

    public async static Task PurgeQueue(string queueName, AltonOptions options, SqsClientResolver sqsClientResolver, HttpContext context)
    {
        if (!options.QueuesToManage.TryGetValue(queueName, out var queueUrls) || queueUrls.DeadLetterQueueUrl == null)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        var sqsClient = sqsClientResolver(queueUrls);
        var deleteMessageResponse = await sqsClient.PurgeQueueAsync(queueUrls.DeadLetterQueueUrl);

        if (deleteMessageResponse.HttpStatusCode != HttpStatusCode.OK)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadGateway;
            return;
        }
    }

    public async static Task DeleteMessage(string queueName, string receipt, AltonOptions options, SqsClientResolver sqsClientResolver, HttpContext context)
    {
        if (!options.QueuesToManage.TryGetValue(queueName, out var queueUrls) || queueUrls.DeadLetterQueueUrl == null)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }
        var sqsClient = sqsClientResolver(queueUrls);

        try
        {
            var deleteMessageResponse = await sqsClient.DeleteMessageAsync(new DeleteMessageRequest
            {
                QueueUrl = queueUrls.DeadLetterQueueUrl,
                ReceiptHandle = receipt
            });
        }
        catch
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        }
    }

    public static async Task RedriveAll(string queueName, AltonOptions options, SqsClientResolver sqsClientResolver, HttpContext context)
    {
        if (!options.QueuesToManage.TryGetValue(queueName, out var queueUrls) || queueUrls.DeadLetterQueueUrl == null)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        var sqsClient = sqsClientResolver(queueUrls);

        var queueArn = (await sqsClient.GetQueueAttributesAsync(
            queueUrls.QueueUrl, new List<string> { "QueueArn" })).QueueARN;

        var dlqArn = (await sqsClient.GetQueueAttributesAsync(
            queueUrls.DeadLetterQueueUrl, new List<string> { "QueueArn" })).QueueARN;

        var deleteMessageResponse = await sqsClient.StartMessageMoveTaskAsync(new StartMessageMoveTaskRequest()
        {
            DestinationArn = queueArn,
            SourceArn = dlqArn
        });

        if (deleteMessageResponse.HttpStatusCode != HttpStatusCode.OK)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadGateway;
        }
    }

    private static async Task<QueueStateResponse> GetState(string queueName, AltonOptions options, SqsClientResolver sqsClientResolver)
    {
        var queueUrls = options.QueuesToManage[queueName];

        var sqsClient = sqsClientResolver(queueUrls);
        var getQueueAttributes = await sqsClient.GetQueueAttributesAsync(queueUrls.QueueUrl, new List<string> { "ApproximateNumberOfMessages" });
        var getDlqAttributes = await sqsClient.GetQueueAttributesAsync(queueUrls.DeadLetterQueueUrl, new List<string> { "ApproximateNumberOfMessages" });

        return new QueueStateResponse
        {
            Name = queueName,
            MessagesInQueue = getQueueAttributes.ApproximateNumberOfMessages,
            MessagesInDeadLetterQueue = getDlqAttributes.ApproximateNumberOfMessages
        };
    }
}

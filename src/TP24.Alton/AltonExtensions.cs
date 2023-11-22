namespace TP24.Alton;

using System.Net;
using Amazon.SQS;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

public delegate IAmazonSQS SqsClientResolver(AltonQueueComponent queue);

public static class AltonExtensions
{
    private static AltonOptions Options { get; set; } = new();

    public static void AddAlton(this IServiceCollection services, SqsClientResolver? sqsClientResolver = null)
    {
        if (sqsClientResolver ==  null)
        {
            var resolver = new RegionalSqsClientCache();
            services.AddSingleton(resolver);
            sqsClientResolver = resolver.GetClientForQueue;
        }

        services.AddSingleton(sqsClientResolver);
    }

    public static void MapAlton(this IEndpointRouteBuilder endpoints, AltonOptions options, string? policyName = null, Action<IEndpointConventionBuilder>? endpointCustomiser = null)
    {
        Options = options;
        var policies = string.IsNullOrEmpty(policyName) ? Array.Empty<string>() : new[] {policyName};
        var endpointsList = new List<IEndpointConventionBuilder>
        {
            endpoints.MapPost(UrlCombine(options.BaseRoute, "/queues/{queueName}/replay"), HandleReplay).RequireAuthorization(policies),
            endpoints.MapPost(UrlCombine(options.BaseRoute, "/queues/{queueName}/redrive-all"), HandleRedriveAll).RequireAuthorization(policies),
            endpoints.MapGet(UrlCombine(options.BaseRoute, "/queue-states"), HandleState).RequireAuthorization(policies),
            endpoints.MapPost(UrlCombine(options.BaseRoute, "/queues/{queueName}/retrieve-messages"), HandleRetrieveMessages).RequireAuthorization(policies),
            endpoints.MapDelete(UrlCombine(options.BaseRoute, "/queues/{queueName}"), HandlePurge).RequireAuthorization(policies),
            endpoints.MapDelete(UrlCombine(options.BaseRoute, "/queues/{queueName}/messages"), HandleDeleteMessage).RequireAuthorization(policies)
        };

        foreach (var endpoint in endpointsList)
        {
            endpointCustomiser?.Invoke(endpoint);
        }
    }

    private static async Task HandleDeleteMessage(HttpContext context, string queueName)
    {
        var sqsClientResolver = context.RequestServices.GetRequiredService<SqsClientResolver>();
        var request = await context.Request.ReadFromJsonAsync<DeleteMessagesRequest>();
        if (request == null)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            return;
        }

        await QueueOperations.DeleteMessage(queueName, request.ReceiptHandle, Options, sqsClientResolver, context);
    }

    private static async Task HandlePurge(HttpContext context, string queueName)
    {
        var sqsClientResolver = context.RequestServices.GetRequiredService<SqsClientResolver>();
        await QueueOperations.PurgeQueue(queueName, Options, sqsClientResolver, context);
    }

    private static async Task HandleRetrieveMessages(HttpContext context, string queueName)
    {
        var sqsClientResolver = context.RequestServices.GetRequiredService<SqsClientResolver>();
        var request = await context.Request.ReadFromJsonAsync<ReceiveMessagesRequest>();
        if (request == null)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            return;
        }

        await QueueOperations.GetRetrieveMessages(queueName, Options, sqsClientResolver, context, request);
    }

    private static async Task HandleState(HttpContext context)
    {
        var sqsClientResolver = context.RequestServices.GetRequiredService<SqsClientResolver>();
        await QueueOperations.GetOverallState(Options, sqsClientResolver, context);
    }

    private static async Task HandleRedriveAll(HttpContext context, string queueName)
    {
        context.Response.StatusCode = 204;
        await Task.CompletedTask;
    }

    private static async Task HandleReplay(HttpContext context, string queueName)
    {
        context.Response.StatusCode = 204;
        await Task.CompletedTask;
    }

    private static string UrlCombine(string url1, string url2)
    {
        if (url1.Length == 0)
        {
            return url2;
        }

        if (url2.Length == 0)
        {
            return url1;
        }

        url1 = url1.TrimEnd('/', '\\');

        url2 = url2.TrimStart('/', '\\');

        return $"{url1}/{url2}";
    }
}

internal class ReceiveMessagesRequest
{
    public int VisibilityTimeout { get; set; } = 60;

    public int MaxNumberOfMessages { get; set; } = 10;
}

internal class DeleteMessagesRequest
{
    public string ReceiptHandle { get; set; } = null!;
}

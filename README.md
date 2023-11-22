# Alton

- [1. Introduction](#1-introduction)
- [2. How to use](#2-how-to-use)

## 1. Introduction

Alton is a piece of middleware which will provide APIs to help manage your queues via your ops portal.

## 2. How to use 

Alton is an ASP.NET Core piece of middleware that will automatically register routes on your behalf.
Alton is still in a preview stage and some issues may arise.

Firstly, to register the middleware, you will need to add it to the service collection and register the middleware:

### Registering Alton

```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddAlton();
    //my other services here
}
```

The `IServiceCollection.AddAlton()` extension will automatically register an `SqsClientResolver` which will create an SQS client using the region parsed from the QueueUrl.

You can provide your own resolver with any delegate matching the signature:

```c#
IAmazonSQS Resolve(AltonQueueComponent queue);
```

```c#
public void Configure(IApplicationBuilder appBuilder)
{
    app.MapAlton(
        new AltonOptions
        {
            BaseRoute = "<api base route>",
            QueuesToManage = new Dictionary<string, AltonQueueComponent>
            {
                ["<Queue 1>"] = new()
                {
                    QueueUrl = settings.AltonQueueUrl,
                    DeadLetterQueueUrl = settings.AltonDlqUrl
                },
                ["<Queue 2>"] = new()
                {
                    QueueUrl = settings.AltonQueueUrl,
                    DeadLetterQueueUrl = settings.AltonDlqUrl
                }
                // etc...
            }
        },
    "AuthPolicyNameForTheseEndpoints", // This policy gets set in `.RequireAuthorization()`
    endpoint => endpoint.IncludeInOpenApi()); // This lets you customise the endpoints further

}
```

Base route defaults to `/queue-management` if not set.

### Alton's Endpoints

#### Get State

Gets the overall state of the managed queues

```http request
GET /queue-states
```

```json
[
    { 
    "Name": "internalQueue",
    "MessagesInQueue": 123,
    "MessagesInDeadLetterQueue": 456
    },
    { 
    "Name": "externalQueue",
    "MessagesInQueue": 789,
    "MessagesInDeadLetterQueue": 0
    }
]
```

#### Redrive All

Redrives all messages in a dead letter queue

```http request
POST /queues/<main queue name>/redrive-all
```

Returns:

No content

#### Retrieve Messages

Retrieves messages from the dead letter queue. The messages will become invisible whilst the visibility timeout is still valid. Once the visibility timeout has lapsed, you will not be able to make individual actions on the messages.

```http request
POST /queues/<queue name>/retrieve-messages
Content-Type: application/json
{
    "VisibilityTimeout": 60
    "MaxNumberOfMessages": 10
}
``` 

Returns:

```json
[
    {
      "Body": "the message body",
      "ReceiptHandle": "message-receipt-handle",
      "Attributes": [
                     {
                      "Key": "message-attribute-key",
                      "DataType": "String",
                      "Value": "my message attribute"
                     }]   
    },
    {
      "Body": "Another message body",
      "ReceiptHandle": "another-message-receipt-handle",
      "Attributes": [
                     {
                      "Key": "message-attribute-key",
                      "DataType": "String",
                      "Value": "my message attribute"
                     }]   
    }
]
```

#### Delete Message

Delete individual messages after retrieving them. Message receipt handle can be obtained from retrieve-messages POST request

```http request
DELETE /queues/<queue name>/messages
Content-Type: application/json
{
    "ReceiptHandle": "<message receipt handle>"
}
```

Returns:

No content

#### Purge Queue

Deletes all messages in a dead letter queue

```http request
DELETE /queues/<queue name>
```

Returns: 

No content

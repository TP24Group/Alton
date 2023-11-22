namespace TP24.Alton;

using System.Text.Json;
using Microsoft.AspNetCore.Http;

internal static class HttpResponseExtensions
{
    public static async Task WriteJsonResponse<T>(this HttpResponse response, T model, CancellationToken cancellationToken)
    {
        response.ContentType = "application/json; charset=utf-8";
        await response.WriteAsync(JsonSerializer.Serialize(model), cancellationToken);
    }
}

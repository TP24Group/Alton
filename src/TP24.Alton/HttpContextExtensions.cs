namespace TP24.Alton;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

internal static class HttpContextExtensions
{
    public static async Task<bool> HasRequiredPolicy(this HttpContext context, params string[] policyNames)
    {
        var authenticated = context.User?.Identity != null && context.User.Identity.IsAuthenticated;
        if (!authenticated)
        {
            context.Response.StatusCode = 401;
            return false;
        }

        var authorizationService = context.RequestServices.GetRequiredService<IAuthorizationService>();
        foreach (var policy in policyNames)
        {
            var result = await authorizationService.AuthorizeAsync(context.User, policy);
            if (!result.Succeeded)
            {
                context.Response.StatusCode = 401;
                return false;
            }
        }

        return true;
    }
}

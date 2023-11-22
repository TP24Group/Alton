namespace TP24.Alton.Tests.Sdk;

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Amazon.SQS;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;

public static class HttpServerFixture
{
    private static async Task<TestServer> Create(IAmazonSQS sqsClient, Action<AltonOptions>? configureOptions = null)
    {
        var applicationBuilder = WebApplication.CreateBuilder();
        applicationBuilder.WebHost.UseTestServer();
        applicationBuilder.Environment.EnvironmentName = "Test";
        applicationBuilder.WebHost.ConfigureLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        applicationBuilder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                          .AddJwtBearer(options =>
                          {
                              options.TokenValidationParameters = new TokenValidationParameters
                              {
                                  SignatureValidator = (token, _) => new JwtSecurityToken(token),
                                  ValidateAudience = false,
                                  ValidateIssuer = false
                              };
                          }); // Todo - remove dependency on jwt package and just setup a cookie auth scheme

        applicationBuilder.Services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                                    .RequireAuthenticatedUser()
                                    .Build();
        });

        applicationBuilder.Services.AddAlton(_ => sqsClient);
        var app = applicationBuilder.Build();

        app.UseAuthentication()
            .UseAuthorization();

        var altonOptions = new AltonOptions();
        configureOptions?.Invoke(altonOptions);
        app.MapAlton(altonOptions);

        await app.StartAsync();

        var server = (TestServer)app.Services.GetRequiredService<IServer>();
        return server;
    }

    private static HttpClient GetTestHttpClient(TestServer server, bool authenticated = true,
        string userRole = "developer")
    {
        var client = server.CreateClient();

        if (authenticated)
        {
            var accessToken = CreateTestAccessToken(userRole);
            client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
        }

        return client;
    }

    public static async Task<HttpClient> GetTestHttpClient(IAmazonSQS sqsClient, bool authenticated = true,
        string userRole = "developer", Action<AltonOptions>? configureOptions = null)
    {
        var server = await Create(sqsClient, configureOptions);
        return GetTestHttpClient(server, authenticated, userRole);
    }

    private static string CreateTestAccessToken(string userRole)
    {
        var securityTokenDescriptor = new SecurityTokenDescriptor
        {
            NotBefore = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(1),
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(RSA.Create()), SecurityAlgorithms.RsaSha512),
            Subject = new ClaimsIdentity(new List<Claim>
            {
                new("name", "Test User"),
                new("email", "user@example.com"),
                new("role", userRole),
                new("sub", "Test")
            })
        };

        var securityTokenHandler = new JwtSecurityTokenHandler();
        var token = securityTokenHandler.CreateToken(securityTokenDescriptor);
        var encodedAccessToken = securityTokenHandler.WriteToken(token);

        return encodedAccessToken;
    }
}

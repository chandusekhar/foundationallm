using System.Security.Claims;
using Asp.Versioning;
using FoundationaLLM.Common.OpenAPI;
using FoundationaLLM.Common.Constants;
using FoundationaLLM.Core.Interfaces;
using FoundationaLLM.Core.Models.ConfigurationOptions;
using FoundationaLLM.Core.Services;
using Microsoft.Extensions.Options;
using Polly;
using Swashbuckle.AspNetCore.SwaggerGen;
using FoundationaLLM.Common.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using FoundationaLLM.Common.Helpers.Authentication;
using FoundationaLLM.Common.Interfaces;
using Microsoft.Identity.Client;

namespace FoundationaLLM.Core.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddOptions<CosmosDbSettings>()
                .Bind(builder.Configuration.GetSection("FoundationaLLM:CosmosDB"));

            builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>();
            builder.Services.AddSingleton<ICoreService, CoreService>();
            builder.Services.AddSingleton<IGatekeeperAPIService, GatekeeperAPIService>();

            builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

            builder.Services
                .AddHttpClient(HttpClients.GatekeeperAPIClient,
                    httpClient =>
                    {
                        httpClient.BaseAddress = new Uri(builder.Configuration["FoundationaLLM:GatekeeperAPI:APIUrl"]);
                        httpClient.DefaultRequestHeaders.Add("X-API-KEY", builder.Configuration["FoundationaLLM:GatekeeperAPI:APIKey"]);
                    })
                .AddTransientHttpErrorPolicy(policyBuilder =>
                    policyBuilder.WaitAndRetryAsync(
                        3, retryNumber => TimeSpan.FromMilliseconds(600)));

            // Register the authentication services
            RegisterAuthConfiguration(builder);

            builder.Services.AddApplicationInsightsTelemetry();
            builder.Services.AddControllers();
            builder.Services.AddProblemDetails();
            builder.Services
                .AddApiVersioning(options =>
                {
                    // Reporting api versions will return the headers
                    // "api-supported-versions" and "api-deprecated-versions"
                    options.ReportApiVersions = true;
                    options.AssumeDefaultVersionWhenUnspecified = true;
                    options.DefaultApiVersion = new ApiVersion(1, 0);
                })
                .AddMvc()
                .AddApiExplorer(options =>
                {
                    // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
                    // note: the specified format code will format the version as "'v'major[.minor][-status]"
                    options.GroupNameFormat = "'v'VVV";
                });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            
            builder.Services.AddSwaggerGen(
                options =>
                {
                    // Add a custom operation filter which sets default values
                    options.OperationFilter<SwaggerDefaultValues>();

                    var fileName = typeof(Program).Assembly.GetName().Name + ".xml";
                    var filePath = Path.Combine(AppContext.BaseDirectory, fileName);

                    // Integrate xml comments
                    options.IncludeXmlComments(filePath);
                });

            var app = builder.Build();

            app.UseExceptionHandler(exceptionHandlerApp
                    => exceptionHandlerApp.Run(async context
                        => await Results.Problem().ExecuteAsync(context)));

            // Configure the HTTP request pipeline.
            app.UseSwagger();
            app.UseSwaggerUI(
                options =>
                {
                    var descriptions = app.DescribeApiVersions();

                    // build a swagger endpoint for each discovered API version
                    foreach (var description in descriptions)
                    {
                        var url = $"/swagger/{description.GroupName}/swagger.json";
                        var name = description.GroupName.ToUpperInvariant();
                        options.SwaggerEndpoint(url, name);
                    }
                });

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }

        public static void RegisterAuthConfiguration(WebApplicationBuilder builder)
        {
            var keyVaultUri = builder.Configuration["FoundationaLLM:Configuration:KeyVaultUri"];
            builder.Services.AddSingleton<Common.Interfaces.IConfiguration>(new KeyVaultConfiguration(keyVaultUri));
            var serviceProvider = builder.Services.BuildServiceProvider();
            var kvConfig = serviceProvider.GetRequiredService<Common.Interfaces.IConfiguration>();
            var azureAdSecret = kvConfig.GetValue(builder.Configuration["FoundationaLLM:Entra:ClientSecretKeyName"]);
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(jwtOptions =>
                    {

                    },
                    identityOptions =>
                    {
                        identityOptions.ClientSecret = azureAdSecret;
                        identityOptions.Instance = builder.Configuration["FoundationaLLM:Entra:Instance"];
                        identityOptions.TenantId = builder.Configuration["FoundationaLLM:Entra:TenantId"];
                        identityOptions.ClientId = builder.Configuration["FoundationaLLM:Entra:ClientId"];
                        identityOptions.CallbackPath = builder.Configuration["FoundationaLLM:Entra:CallbackPath"];
                    });
                //.EnableTokenAcquisitionToCallDownstreamApi()
                //.AddInMemoryTokenCaches();

            //builder.Services.AddScoped<IAuthenticatedHttpClientFactory, EntraAuthenticatedHttpClientFactory>();
            builder.Services.AddScoped<IUserClaimsProvider, EntraUserClaimsProvider>();

            // Configure the scope used by the API controllers:
            var requiredScope = builder.Configuration["FoundationaLLM:Entra:Scopes"];
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("RequiredScope", policyBuilder =>
                {
                    policyBuilder.RequireAuthenticatedUser();
                    policyBuilder.RequireClaim("http://schemas.microsoft.com/identity/claims/scope", requiredScope.Split(' '));
                });
            });
        }
    }
}